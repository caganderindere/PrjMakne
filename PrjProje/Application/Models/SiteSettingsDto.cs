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
    public string AddressTr { get; set; } = "Mehmet Akif Ersoy Mah. Candaner Sok. No:15-A Taşoluk, 34281 Arnavutköy / İstanbul, Türkiye";
    public string AddressEn { get; set; } = "Mehmet Akif Ersoy Mah. Candaner Sok. No:15-A Taşoluk, 34281 Arnavutköy / Istanbul, Türkiye";
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

    // Site-relative MP4 path for the desktop hero; empty keeps the photo.
    public string? HeroVideo { get; set; }

    // Home counters; empty hides the counter. Kept as strings because every setting is stored as text.
    public string? FoundedYear { get; set; }
    public string? CompletedProjects { get; set; }

    public string? GoogleSiteVerification { get; set; }

    /// <summary>GA4 measurement ID (G-XXXXXXXXXX); empty disables analytics. It loads only after the visitor accepts cookies.</summary>
    public string? GoogleAnalyticsId { get; set; }

    public string GetAddress(string languageCode) => languageCode == Languages.Turkish ? AddressTr : AddressEn;

    public string GetWorkingHours(string languageCode) => languageCode == Languages.Turkish ? WorkingHoursTr : WorkingHoursEn;


    public IEnumerable<(string Icon, string Url, string Label)> GetSocialLinks()
    {
        if (!string.IsNullOrWhiteSpace(LinkedInUrl)) yield return ("fa-brands fa-linkedin-in", LinkedInUrl, "LinkedIn");
        if (!string.IsNullOrWhiteSpace(InstagramUrl)) yield return ("fa-brands fa-instagram", InstagramUrl, "Instagram");
        if (!string.IsNullOrWhiteSpace(FacebookUrl)) yield return ("fa-brands fa-facebook-f", FacebookUrl, "Facebook");
        if (!string.IsNullOrWhiteSpace(YouTubeUrl)) yield return ("fa-brands fa-youtube", YouTubeUrl, "YouTube");
        if (!string.IsNullOrWhiteSpace(XUrl)) yield return ("fa-brands fa-x-twitter", XUrl, "X");
    }
}
