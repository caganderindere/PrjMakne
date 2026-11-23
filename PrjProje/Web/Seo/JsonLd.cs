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
    private const string StreetAddress = "Mehmet Akif Ersoy Mah. Candener Sok. No:15-A Taşoluk";
    private const string Locality = "Arnavutköy";
    private const string Region = "İstanbul";
    private const string PostalCode = "34281";
    private const string FormerName = "Prestij Teknik Makine";
    private const string LogoPath = "/images/brand/prj-logo.svg";

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
        ["sameAs"] = settings.GetSocialLinks().Select(s => s.Url).ToArray()
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
        ["datePublished"] = published.ToString("O"),
        ["dateModified"] = modified.ToString("O"),
        ["inLanguage"] = language,
        ["author"] = new Dictionary<string, object> { ["@type"] = "Organization", ["name"] = authorName },
        ["publisher"] = new Dictionary<string, object>
        {
            ["@type"] = "Organization",
            ["name"] = publisherName,
            ["logo"] = new Dictionary<string, object> { ["@type"] = "ImageObject", ["url"] = baseUrl + LogoPath }
        },
        ["mainEntityOfPage"] = url
    });

    // WhenWritingNull skips null properties of objects, not null dictionary entries; those are dropped here.
    private static string Serialize(object value) =>
        JsonSerializer.Serialize(value is Dictionary<string, object?> map ? map.Where(e => e.Value is not null).ToDictionary() : value, Options);
}
