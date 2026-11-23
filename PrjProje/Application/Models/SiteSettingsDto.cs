using PrjProje.Application.Common;

namespace PrjProje.Application.Models;

/// <summary>
/// Strongly typed view over the key/value SiteSettings table.
/// Property names are used as setting keys.
/// </summary>
public class SiteSettingsDto
{
    public string CompanyName { get; set; } = "PRJ Proje Makina A.Ş.";
    public string Phone { get; set; } = "+90 212 544 01 04";
    public string? Fax { get; set; }
    public string? Gsm { get; set; }
    public string Email { get; set; } = "info@prestijteknikmakine.com.tr";
    public string? SalesEmail { get; set; }
    public string AddressTr { get; set; } = "Mehmet Akif Ersoy Mah. Candener Sok. No:15-A Taşoluk, 34281 Arnavutköy / İstanbul, Türkiye";
    public string AddressEn { get; set; } = "Mehmet Akif Ersoy Mah. Candener Sok. No:15-A Taşoluk, 34281 Arnavutköy / Istanbul, Türkiye";
    public string WorkingHoursTr { get; set; } = "Pazartesi - Cuma: 08:30 - 18:00";
    public string WorkingHoursEn { get; set; } = "Monday - Friday: 08:30 - 18:00";
    public string? MapEmbedUrl { get; set; }

    /// <summary>Number for the floating WhatsApp button (the sales contact's); empty hides the button.</summary>
    public string? WhatsApp { get; set; }

    // Message the chat opens with, in the visitor's language; the visitor can still edit it before sending.
    public string? WhatsAppMessageTr { get; set; } = "Merhaba, web sitenizden ulaşıyorum. Ürünleriniz hakkında bilgi almak istiyorum.";
    public string? WhatsAppMessageEn { get; set; } = "Hello, I am contacting you from your website. I would like to get information about your products.";

    public string? GetWhatsAppMessage(string languageCode) => languageCode == Languages.English ? WhatsAppMessageEn : WhatsAppMessageTr;
    public string? LinkedInUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? FacebookUrl { get; set; }
    public string? YouTubeUrl { get; set; }
    public string? XUrl { get; set; }
    public string? HeroImage { get; set; }

    // Site-relative MP4 paths; empty keeps the photo. Phones get their own portrait video.
    public string? HeroVideo { get; set; }
    public string? HeroVideoMobile { get; set; } = "/videos/heromobil.mp4";

    public string ExportCountries { get; set; } = string.Empty;
    public string? GoogleSiteVerification { get; set; }

    /// <summary>GA4 measurement ID (G-XXXXXXXXXX); empty disables analytics. It loads only after the visitor accepts cookies.</summary>
    public string? GoogleAnalyticsId { get; set; }

    // Home page / About page figures (numbers only; units and labels come from the texts).
    public string StatFacility { get; set; } = "25000";
    public string StatEmployees { get; set; } = "150";
    public string StatCountries { get; set; } = "20";
    public string StatDomestic { get; set; } = "100";

    /// <summary>
    /// Order of the movable home page sections as comma separated keys; a leading "!" hides a section.
    /// Keys missing from the value (e.g. sections added later) are appended as visible.
    /// </summary>
    // Why and process repeat the about section, so a new site starts with them hidden.
    public string HomeSections { get; set; } = "about,categories,services,featured,references,defense,export,blog,!industries,!why,!process";

    public static readonly IReadOnlyList<string> HomeSectionKeys = new[]
    {
        "about", "categories", "services", "process", "featured", "why",
        "references", "defense", "industries", "export", "blog"
    };

    public IReadOnlyList<(string Key, bool Visible)> GetHomeSections()
    {
        var result = new List<(string Key, bool Visible)>();
        foreach (var raw in HomeSections.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var visible = !raw.StartsWith('!');
            var key = raw.TrimStart('!');
            if (HomeSectionKeys.Contains(key) && result.All(s => s.Key != key))
            {
                result.Add((key, visible));
            }
        }

        result.AddRange(HomeSectionKeys.Where(key => result.All(s => s.Key != key)).Select(key => (key, true)));
        return result;
    }

    /// <summary>Parses a figure setting such as "25000" or "25.000"; falls back when it is not a number.</summary>
    public static int ParseStat(string? value, int fallback)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var number) ? number : fallback;
    }

    public string GetAddress(string languageCode) => languageCode == Languages.Turkish ? AddressTr : AddressEn;

    public string GetWorkingHours(string languageCode) => languageCode == Languages.Turkish ? WorkingHoursTr : WorkingHoursEn;

    public IReadOnlyList<string> GetExportCountryCodes() =>
        ExportCountries
            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(code => code.ToUpperInvariant())
            .Where(code => code.Length == 2)
            .Distinct()
            .ToList();

    public IEnumerable<(string Icon, string Url, string Label)> GetSocialLinks()
    {
        if (!string.IsNullOrWhiteSpace(LinkedInUrl)) yield return ("fa-brands fa-linkedin-in", LinkedInUrl, "LinkedIn");
        if (!string.IsNullOrWhiteSpace(InstagramUrl)) yield return ("fa-brands fa-instagram", InstagramUrl, "Instagram");
        if (!string.IsNullOrWhiteSpace(FacebookUrl)) yield return ("fa-brands fa-facebook-f", FacebookUrl, "Facebook");
        if (!string.IsNullOrWhiteSpace(YouTubeUrl)) yield return ("fa-brands fa-youtube", YouTubeUrl, "YouTube");
        if (!string.IsNullOrWhiteSpace(XUrl)) yield return ("fa-brands fa-x-twitter", XUrl, "X");
    }
}
