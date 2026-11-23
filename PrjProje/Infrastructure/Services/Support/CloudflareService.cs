using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models.Admin;
using PrjProje.Domain.Entities;
using PrjProje.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PrjProje.Infrastructure.Services.Support;

/// <summary>Panel-managed Cloudflare settings and Turnstile verification. Kept out of SiteSettingsDto so the secret never reaches views.</summary>
public sealed class CloudflareService : ICloudflareService
{
    public const string ResponseField = "cf-turnstile-response";

    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
    private const string Prefix = "Cloudflare.";
    private const string UseProxyKey = Prefix + "UseProxy";
    private const string SiteKeyKey = Prefix + "TurnstileSiteKey";
    private const string SecretKey = Prefix + "TurnstileSecret";
    private const string LoginKey = Prefix + "TurnstileLogin";
    private const string ContactKey = Prefix + "TurnstileContact";
    private const string QuoteKey = Prefix + "TurnstileQuote";
    private const string FairKey = Prefix + "TurnstileFair";

    // Our own misconfiguration or a Cloudflare fault, not a failed visitor; these must not block real people.
    private static readonly string[] ServiceErrors = { "missing-input-secret", "invalid-input-secret", "internal-error" };

    private readonly ApplicationDbContext _db;
    private readonly IContentCache _cache;
    private readonly IDataProtector _protector;
    private readonly IActivityLogger _activityLogger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _http;
    private readonly ILogger<CloudflareService> _logger;

    public CloudflareService(
        ApplicationDbContext db,
        IContentCache cache,
        IDataProtectionProvider dataProtection,
        IActivityLogger activityLogger,
        IConfiguration configuration,
        HttpClient http,
        ILogger<CloudflareService> logger)
    {
        _db = db;
        _cache = cache;
        _protector = dataProtection.CreateProtector("PrjProje.Cloudflare.v1");
        _activityLogger = activityLogger;
        _configuration = configuration;
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(5);
        _logger = logger;
    }

    public Task<CloudflareSettings> GetAsync(CancellationToken cancellationToken = default) =>
        _cache.GetOrCreateAsync("cloudflare-settings", async () =>
        {
            var values = await _db.SiteSettings.AsNoTracking()
                .Where(s => s.Key.StartsWith(Prefix))
                .ToDictionaryAsync(s => s.Key, s => s.Value, CancellationToken.None);

            bool Flag(string key) => values.GetValueOrDefault(key) == "true";
            var (secret, unreadable) = Unprotect(values.GetValueOrDefault(SecretKey));
            return new CloudflareSettings(
                Flag(UseProxyKey), values.GetValueOrDefault(SiteKeyKey), secret, unreadable,
                Flag(LoginKey), Flag(ContactKey), Flag(QuoteKey), Flag(FairKey),
                _configuration.GetValue<bool>("Cloudflare:DisableTurnstile"));
        }, TimeSpan.FromHours(1));

    public async Task<ServiceResult> SaveAsync(CloudflareEditModel model, CancellationToken cancellationToken = default)
    {
        var current = await GetAsync(cancellationToken);
        var siteKey = model.TurnstileSiteKey?.Trim();
        var newSecret = model.TurnstileSecret?.Trim();
        var anyForm = model.ProtectLogin || model.ProtectContact || model.ProtectQuote || model.ProtectFair;
        if (anyForm && (string.IsNullOrEmpty(siteKey) || (string.IsNullOrEmpty(newSecret) && current.Secret is null)))
        {
            return ServiceResult.Failure("Turnstile'ı bir formda açmak için site key ve secret key gereklidir.");
        }

        var existing = await _db.SiteSettings.Where(s => s.Key.StartsWith(Prefix)).ToDictionaryAsync(s => s.Key, cancellationToken);
        void Set(string key, string? value)
        {
            if (existing.TryGetValue(key, out var setting))
            {
                setting.Value = value;
            }
            else
            {
                _db.SiteSettings.Add(new SiteSetting { Key = key, Value = value });
            }
        }

        static string Bool(bool value) => value ? "true" : "false";
        Set(UseProxyKey, Bool(model.UseProxy));
        Set(SiteKeyKey, siteKey);
        Set(LoginKey, Bool(model.ProtectLogin));
        Set(ContactKey, Bool(model.ProtectContact));
        Set(QuoteKey, Bool(model.ProtectQuote));
        Set(FairKey, Bool(model.ProtectFair));
        if (!string.IsNullOrEmpty(newSecret))
        {
            Set(SecretKey, _protector.Protect(newSecret));
        }

        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
        await _activityLogger.LogAsync("Updated", "Cloudflare", null, null, cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<bool> VerifyAsync(TurnstileForm form, HttpContext context, CancellationToken cancellationToken = default)
    {
        var settings = await GetAsync(cancellationToken);
        if (!settings.Protects(form))
        {
            return true;
        }

        var token = context.Request.Form[ResponseField].ToString();
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var result = await SiteVerifyAsync(settings.Secret!, token, context.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        if (result is null || result.Errors.Any(code => ServiceErrors.Contains(code)))
        {
            _logger.LogWarning("Turnstile doğrulaması yapılamadı, form kabul edildi: {Errors}", result is null ? "servise ulaşılamadı" : string.Join(",", result.Errors));
            return true;
        }

        return result.Success;
    }

    public async Task<string?> TestAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        var settings = await GetAsync(cancellationToken);
        if (settings.Secret is null)
        {
            return "Secret key kayıtlı değil veya çözülemedi.";
        }

        var token = context.Request.Form[ResponseField].ToString();
        if (string.IsNullOrWhiteSpace(token))
        {
            return "Doğrulama kutusu tamamlanmadı. Site key hatalı veya alan adı Turnstile'da tanımlı olmayabilir.";
        }

        var result = await SiteVerifyAsync(settings.Secret, token, context.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return result is null ? "Cloudflare'e ulaşılamadı."
            : result.Success ? null
            : "Cloudflare doğrulamayı reddetti: " + string.Join(", ", result.Errors);
    }

    private (string? Secret, bool Unreadable) Unprotect(string? stored)
    {
        if (string.IsNullOrEmpty(stored))
        {
            return (null, false);
        }

        try
        {
            return (_protector.Unprotect(stored), false);
        }
        catch (CryptographicException exception)
        {
            _logger.LogWarning(exception, "Turnstile secret key çözülemedi; panelden yeniden girilmeli.");
            return (null, true);
        }
    }

    private async Task<SiteVerifyResult?> SiteVerifyAsync(string secret, string token, string? remoteIp, CancellationToken cancellationToken)
    {
        var fields = new Dictionary<string, string> { ["secret"] = secret, ["response"] = token };
        if (remoteIp is not null)
        {
            fields["remoteip"] = remoteIp;
        }

        try
        {
            using var response = await _http.PostAsync(VerifyUrl, new FormUrlEncodedContent(fields), cancellationToken);
            await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<SiteVerifyResult>(body, cancellationToken: cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Turnstile servisine ulaşılamadı.");
            return null;
        }
    }

    private sealed record SiteVerifyResult(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("error-codes")] string[]? ErrorCodes)
    {
        public string[] Errors => ErrorCodes ?? Array.Empty<string>();
    }
}
