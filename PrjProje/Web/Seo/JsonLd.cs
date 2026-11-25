using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using PrjProje.Application.Models;

namespace PrjProje.Web.Seo;

/// <summary>
/// Schema.org objects as JSON-LD strings. The encoder keeps Turkish letters readable but still escapes &lt; and &gt;,
/// so a value can never close the surrounding script tag.
/// </summary>
public static class JsonLd
{
    // The address setting is one free-text line; search engines want its parts separately.
    private const string StreetAddress = "Mehmet Akif Ersoy Mah. Candaner Sok. No:15-A Taşoluk";
    private const string Locality = "Arnavutköy";
    private const string Region = "İstanbul";
    private const string PostalCode = "34281";
    private const string FormerName = "Prestij Teknik Makine";
    // Google reads logos as raster images; the PNG is rendered from prj-logo.svg.
    private const string LogoPath = "/images/brand/prj-logo.png";

    // Matches the default working hours setting ("Pazartesi - Cuma: 08:30 - 18:00"), which is free text.
    private static readonly string[] OpenDays = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
    private const string Opens = "08:30";
    private const string Closes = "18:00";

    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public static string Organization(SiteSettingsDto settings, string baseUrl) => Serialize(new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = new[] { "Organization", "LocalBusiness" },
        ["@id"] = baseUrl + "/#organization",
        ["name"] = settings.CompanyName,
        ["alternateName"] = FormerName,
        ["url"] = baseUrl,
        ["logo"] = baseUrl + LogoPath,
        ["image"] = baseUrl + LogoPath,
        ["email"] = settings.Email,
        ["telephone"] = new string(settings.Phone.Where(c => char.IsDigit(c) || c == '+').ToArray()),
        ["address"] = new Dictionary<string, object>
        {
            ["@type"] = "PostalAddress",
            ["streetAddress"] = StreetAddress,
            ["addressLocality"] = Locality,
            ["addressRegion"] = Region,
            ["postalCode"] = PostalCode,
            ["addressCountry"] = "TR"
        },
        ["hasMap"] = "https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString($"{StreetAddress}, {PostalCode} {Locality} {Region}"),
        ["openingHoursSpecification"] = new Dictionary<string, object>
        {
            ["@type"] = "OpeningHoursSpecification",
            ["dayOfWeek"] = OpenDays,
            ["opens"] = Opens,
            ["closes"] = Closes
        },
        ["sameAs"] = settings.GetSocialLinks().Select(s => s.Url).ToArray()
    });

    public static string WebSite(string name, string baseUrl, IEnumerable<string> languages) => Serialize(new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "WebSite",
        ["@id"] = baseUrl + "/#website",
        ["name"] = name,
        ["url"] = baseUrl,
        ["inLanguage"] = languages.ToArray(),
        ["publisher"] = new Dictionary<string, object> { ["@id"] = baseUrl + "/#organization" }
    });

    public static string Service(string name, string? description, string? imageUrl, string url, string baseUrl) => Serialize(new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "Service",
        ["name"] = name,
        ["serviceType"] = name,
        ["description"] = description,
        ["image"] = imageUrl,
        ["url"] = url,
        ["provider"] = new Dictionary<string, object> { ["@id"] = baseUrl + "/#organization" }
    });

    /// <summary>No offers: prices are quoted per order, and an offer without a price is an error in Google's report.</summary>
    public static string Product(ProductDetailDto product, string url, string? imageUrl, string companyName) => Serialize(new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "Product",
        ["name"] = product.Name,
        ["description"] = product.ShortDescription,
        ["image"] = imageUrl,
        ["category"] = product.CategoryName,
        ["model"] = product.Variants.Count > 0 ? product.Variants : null,
        ["brand"] = new Dictionary<string, object> { ["@type"] = "Brand", ["name"] = "PRJ" },
        ["manufacturer"] = new Dictionary<string, object> { ["@type"] = "Organization", ["name"] = companyName },
        ["url"] = url
    });

    public static string Breadcrumb(IEnumerable<(string Name, string Url)> items) => Serialize(new Dictionary<string, object>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "BreadcrumbList",
        ["itemListElement"] = items.Select((item, index) => new Dictionary<string, object>
        {
            ["@type"] = "ListItem",
            ["position"] = index + 1,
            ["name"] = item.Name,
            ["item"] = item.Url
        }).ToArray()
    });

    public static string Video(string name, string youtubeId, string description) => Serialize(new Dictionary<string, object>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "VideoObject",
        ["name"] = name,
        ["description"] = description,
        ["thumbnailUrl"] = new[] { $"https://i.ytimg.com/vi/{youtubeId}/hqdefault.jpg" },
        ["embedUrl"] = $"https://www.youtube.com/embed/{youtubeId}",
        ["contentUrl"] = $"https://www.youtube.com/watch?v={youtubeId}"
    });

    public static string BlogPosting(string headline, string? description, string? imageUrl, DateTime published, DateTime modified,
        string language, string authorName, string publisherName, string baseUrl, string url) => Serialize(new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "BlogPosting",
        ["headline"] = headline,
        ["description"] = description,
        ["image"] = imageUrl,
        ["datePublished"] = UtcTimestamp(published),
        ["dateModified"] = UtcTimestamp(modified),
        ["inLanguage"] = language,
        // A named writer set in the panel is a Person (an expertise signal); the company name stays an Organization.
        ["author"] = new Dictionary<string, object>
        {
            ["@type"] = publisherName.StartsWith(authorName, StringComparison.OrdinalIgnoreCase) ? "Organization" : "Person",
            ["name"] = authorName
        },
        ["publisher"] = new Dictionary<string, object>
        {
            ["@type"] = "Organization",
            ["name"] = publisherName,
            ["logo"] = new Dictionary<string, object> { ["@type"] = "ImageObject", ["url"] = baseUrl + LogoPath }
        },
        ["mainEntityOfPage"] = url
    });

    /// <summary>Dates are stored in UTC but read back without a kind; the "Z" tells search engines the time zone.</summary>
    public static string UtcTimestamp(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture);

    // WhenWritingNull skips null properties of objects, not null dictionary entries; those are dropped here.
    private static string Serialize(object value) =>
        JsonSerializer.Serialize(value is Dictionary<string, object?> map ? map.Where(e => e.Value is not null).ToDictionary() : value, Options);
}
