using PrjProje.Application.Common;

namespace PrjProje.Web.Routing;

public sealed record LocalizedRoute(string Key, string Controller, string Action, IReadOnlyDictionary<string, string> Patterns);

/// <summary>
/// Central registry of public, language-specific URLs. Every public page has its own
/// translated path per language (e.g. /tr/hakkimizda and /en/about-us), which is used for
/// route registration, link generation, hreflang alternates and the sitemap.
/// </summary>
public static class SiteRoutes
{
    public const string SlugToken = "{slug}";

    public const string Home = "Home";
    public const string About = "About";
    public const string Products = "Products";
    public const string ProductCategory = "ProductCategory";
    public const string ProductDetail = "ProductDetail";
    public const string Catalog = "Catalog";
    public const string Manufacturing = "Manufacturing";
    public const string ManufacturingDetail = "ManufacturingDetail";
    public const string Defense = "Defense";
    public const string Blog = "Blog";
    public const string BlogDetail = "BlogDetail";
    public const string Gallery = "Gallery";
    public const string Quality = "Quality";
    public const string Contact = "Contact";
    public const string Quote = "Quote";
    public const string Careers = "Careers";
    public const string Privacy = "Privacy";
    public const string Cookies = "Cookies";
    public const string BusinessCard = "BusinessCard";
    public const string BusinessCardContact = "BusinessCardContact";
    public const string Fair = "Fair";
    public const string FairBrochure = "FairBrochure";

    public static readonly IReadOnlyList<LocalizedRoute> All = new[]
    {
        Route(Home, "Home", "Index", "", ""),
        Route(About, "About", "Index", "hakkimizda", "about-us"),
        Route(ProductCategory, "Products", "Index", "urunler/kategori/{slug}", "products/category/{slug}"),
        Route(Catalog, "Catalog", "Index", "urunler/katalog", "products/catalogue"),
        Route(ProductDetail, "Products", "Detail", "urunler/{slug}", "products/{slug}"),
        Route(Products, "Products", "Index", "urunler", "products"),
        Route(ManufacturingDetail, "Manufacturing", "Detail", "uretim/{slug}", "manufacturing/{slug}"),
        Route(Manufacturing, "Manufacturing", "Index", "uretim", "manufacturing"),
        Route(Defense, "Defense", "Index", "savunma-sanayii", "defense-industry"),
        Route(BlogDetail, "Blog", "Detail", "blog/{slug}", "blog/{slug}"),
        Route(Blog, "Blog", "Index", "blog", "blog"),
        Route(Gallery, "Gallery", "Index", "galeri", "gallery"),
        Route(Quality, "Quality", "Index", "kalite", "quality"),
        Route(Contact, "Contact", "Index", "iletisim", "contact"),
        Route(Quote, "Quote", "Index", "teklif-al", "request-a-quote"),
        Route(Careers, "Careers", "Index", "insan-kaynaklari", "careers"),
        Route(Privacy, "Legal", "Privacy", "kvkk", "privacy-policy"),
        Route(Cookies, "Legal", "Cookies", "cerez-politikasi", "cookie-policy"),
        Route(BusinessCardContact, "Card", "VCard", "kartvizit/{slug}/rehber.vcf", "card/{slug}/contact.vcf"),
        Route(BusinessCard, "Card", "Show", "kartvizit/{slug}", "card/{slug}"),
        Route(FairBrochure, "Fair", "Brochure", "fuar/{slug}/brosur", "fair/{slug}/brochure"),
        Route(Fair, "Fair", "Show", "fuar/{slug}", "fair/{slug}")
    };

    /// <summary>Static pages (no slug) listed in the sitemap.</summary>
    public static IEnumerable<string> StaticPageKeys => All.Where(r => !RequiresSlug(r.Key)).Select(r => r.Key);

    private static readonly Dictionary<string, LocalizedRoute> ByKey = All.ToDictionary(r => r.Key);

    public static bool RequiresSlug(string key) => ByKey[key].Patterns.Values.Any(p => p.Contains(SlugToken));

    public static string Path(string key, string languageCode, string? slug = null)
    {
        if (!ByKey.TryGetValue(key, out var route))
        {
            throw new ArgumentException($"Unknown route key '{key}'.", nameof(key));
        }

        if (!route.Patterns.TryGetValue(languageCode, out var pattern))
        {
            pattern = route.Patterns[Languages.Default];
            languageCode = Languages.Default;
        }

        if (pattern.Contains(SlugToken))
        {
            pattern = pattern.Replace(SlugToken, Uri.EscapeDataString(slug ?? string.Empty));
        }

        return pattern.Length == 0 ? $"/{languageCode}" : $"/{languageCode}/{pattern}";
    }

    private static LocalizedRoute Route(string key, string controller, string action, string turkish, string english) =>
        new(key, controller, action, new Dictionary<string, string>
        {
            [Languages.Turkish] = turkish,
            [Languages.English] = english
        });
}
