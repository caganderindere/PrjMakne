using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models.Admin;
using PrjProje.Domain.Entities;
using PrjProje.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PrjProje.Infrastructure.Services.Support;

/// <summary>
/// Panel-managed SMTP settings and the e-mails sent when a form arrives. Like the Cloudflare secret, the SMTP password
/// is stored encrypted and never reaches a view. Sending runs in the background, so a slow or broken mail server
/// never delays or fails the visitor's form.
/// </summary>
public sealed class EmailNotificationService : INotificationService
{
    private const string Prefix = "Email.";
    private const string HostKey = Prefix + "Host";
    private const string PortKey = Prefix + "Port";
    private const string SslKey = Prefix + "Ssl";
    private const string UserKey = Prefix + "UserName";
    private const string PasswordKey = Prefix + "Password";
    private const string FromKey = Prefix + "From";
    private const string SalesKey = Prefix + "SalesTo";
    private const string HrKey = Prefix + "HrTo";

    private readonly ApplicationDbContext _db;
    private readonly IContentCache _cache;
    private readonly IDataProtector _protector;
    private readonly IActivityLogger _activityLogger;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        ApplicationDbContext db,
        IContentCache cache,
        IDataProtectionProvider dataProtection,
        IActivityLogger activityLogger,
        IConfiguration configuration,
        ILogger<EmailNotificationService> logger)
    {
        _db = db;
        _cache = cache;
        _protector = dataProtection.CreateProtector("PrjProje.Email.v1");
        _activityLogger = activityLogger;
        _configuration = configuration;
        _logger = logger;
    }

    public Task<EmailSettings> GetAsync(CancellationToken cancellationToken = default) =>
        _cache.GetOrCreateAsync("email-settings", async () =>
        {
            var values = await _db.SiteSettings.AsNoTracking()
                .Where(s => s.Key.StartsWith(Prefix))
                .ToDictionaryAsync(s => s.Key, s => s.Value, CancellationToken.None);

            var (password, unreadable) = Unprotect(values.GetValueOrDefault(PasswordKey));
            return new EmailSettings(
                values.GetValueOrDefault(HostKey),
                int.TryParse(values.GetValueOrDefault(PortKey), out var port) ? port : 587,
                values.GetValueOrDefault(SslKey) != "false",
                values.GetValueOrDefault(UserKey),
                password,
                unreadable,
                values.GetValueOrDefault(FromKey),
                values.GetValueOrDefault(SalesKey),
                values.GetValueOrDefault(HrKey));
        }, TimeSpan.FromHours(1));

    public async Task<ServiceResult> SaveAsync(EmailEditModel model, CancellationToken cancellationToken = default)
    {
        foreach (var address in new[] { model.From }.Concat(Split(model.SalesTo)).Concat(Split(model.HrTo)))
        {
            if (!string.IsNullOrWhiteSpace(address) && !MailAddress.TryCreate(address, out _))
            {
                return ServiceResult.Failure($"Geçersiz e-posta adresi: {address}");
            }
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

        Set(HostKey, model.Host?.Trim());
        Set(PortKey, model.Port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Set(SslKey, model.UseSsl ? "true" : "false");
        Set(UserKey, model.UserName?.Trim());
        Set(FromKey, model.From?.Trim());
        Set(SalesKey, model.SalesTo?.Trim());
        Set(HrKey, model.HrTo?.Trim());
        if (!string.IsNullOrEmpty(model.Password))
        {
            Set(PasswordKey, _protector.Protect(model.Password));
        }

        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
        await _activityLogger.LogAsync("Updated", "EmailSettings", null, null, cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<string?> SendTestAsync(string to, CancellationToken cancellationToken = default)
    {
        var settings = await GetAsync(cancellationToken);
        if (!settings.IsConfigured)
        {
            return "Önce sunucu, kullanıcı adı, şifre ve gönderen adresini kaydedin.";
        }

        try
        {
            await SendAsync(settings, Split(to), "PRJ Proje Makina test e-postası",
                "Bu bir test e-postasıdır. Bunu okuyorsanız site bildirimleri çalışıyor.", replyTo: null);
            return null;
        }
        catch (Exception exception) when (exception is SmtpException or InvalidOperationException or FormatException)
        {
            _logger.LogWarning(exception, "Test e-postası gönderilemedi.");
            return $"Gönderilemedi: {exception.Message}";
        }
    }

    public async Task NotifyAsync(NotificationKind kind, string subject, string body, string? replyTo, CancellationToken cancellationToken = default)
    {
        var settings = await GetAsync(cancellationToken);
        var recipients = Split(kind == NotificationKind.HumanResources ? settings.HrTo : settings.SalesTo).ToList();
        if (!settings.IsConfigured || recipients.Count == 0)
        {
            return;
        }

        var baseUrl = _configuration["Site:BaseUrl"]?.TrimEnd('/');
        var text = string.IsNullOrEmpty(baseUrl) ? body : $"{body}\n\nYönetim paneli: {baseUrl}/admin";

        // Fire and forget: the form has already been saved; a mail failure is logged, never shown to the visitor.
        _ = Task.Run(async () =>
        {
            try
            {
                await SendAsync(settings, recipients, subject, text, replyTo);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Bildirim e-postası gönderilemedi: {Subject}", subject);
            }
        }, CancellationToken.None);
    }

    private static async Task SendAsync(EmailSettings settings, IEnumerable<string> to, string subject, string body, string? replyTo)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(settings.From!, "PRJ Proje Makina"),
            // Visitor-typed text ends up in the subject: line breaks there would be header injection.
            Subject = subject.ReplaceLineEndings(" "),
            Body = body,
            IsBodyHtml = false
        };
        foreach (var address in to)
        {
            message.To.Add(address);
        }

        if (replyTo is not null && MailAddress.TryCreate(replyTo, out var reply))
        {
            message.ReplyToList.Add(reply);
        }

        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.UseSsl,
            Credentials = new NetworkCredential(settings.UserName, settings.Password),
            Timeout = 15000
        };
        await client.SendMailAsync(message);
    }

    private static IEnumerable<string> Split(string? addresses) =>
        (addresses ?? string.Empty).Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private (string? Password, bool Unreadable) Unprotect(string? stored)
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
            _logger.LogWarning(exception, "SMTP şifresi çözülemedi; panelden yeniden girilmeli.");
            return (null, true);
        }
    }
}
