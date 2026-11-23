using System.Globalization;
using PrjProje.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;

namespace PrjProje.Infrastructure.Services.Support;

/// <summary>
/// Lightweight, privacy-friendly bot protection: a hidden honeypot field plus a signed
/// timestamp that rejects forms submitted too fast or replayed much later.
/// A CAPTCHA provider can be added behind the same interface.
/// </summary>
public sealed class SpamProtectionService : ISpamProtectionService
{
    public const string HoneypotField = "hp_website";
    public const string TokenField = "hp_token";

    private static readonly TimeSpan MinimumFillTime = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan MaximumTokenAge = TimeSpan.FromHours(12);

    private readonly IDataProtector _protector;

    public SpamProtectionService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("PrjProje.FormSpamProtection.v1");
    }

    public string CreateFormToken() =>
        _protector.Protect(DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));

    public bool IsLikelySpam(IFormCollection form)
    {
        if (!string.IsNullOrWhiteSpace(form[HoneypotField]))
        {
            return true;
        }

        var token = form[TokenField].ToString();
        if (string.IsNullOrWhiteSpace(token))
        {
            return true;
        }

        try
        {
            var ticks = long.Parse(_protector.Unprotect(token), CultureInfo.InvariantCulture);
            var elapsed = DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc);
            return elapsed < MinimumFillTime || elapsed > MaximumTokenAge;
        }
        catch (Exception exception) when (exception is FormatException or System.Security.Cryptography.CryptographicException)
        {
            return true;
        }
    }
}
