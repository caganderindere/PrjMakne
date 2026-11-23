using PrjProje.Application.Common;

namespace PrjProje.Web.Routing;

public sealed record LocalizedRoute(string Key, string Controller, string Action, IReadOnlyDictionary<string, string> Patterns);

/// <summary>
/// Central registry of public, language-specific URLs. Every public page has its own
/// translated path per language (e.g. /tr/kurumsal and /en/about-us), which is used for
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
    public const string Manuals = "Manuals";
    public const string Services = "Services";
    public const string ServiceDetail = "ServiceDetail";
    public const string Blog = "Blog";
    public const string BlogDetail = "BlogDetail";
    public const string Videos = "Videos";
    public const string Certificates = "Certificates";
    public const string Contact = "Contact";
    public const string Quote = "Quote";
    public const string Privacy = "Privacy";
    public const string Cookies = "Cookies";

    public static readonly IReadOnlyList<LocalizedRoute> All = new[]
    {
        Route(Home, "Home", "Index", "", ""),
        Route(About, "About", "Index", "kurumsal", "about-us"),
        Route(Certificates, "Certificates", "Index", "belgeler", "certificates"),
        Route(ProductCategory, "Products", "Index", "urunler/kategori/{slug}", "products/category/{slug}"),
        Route(ProductDetail, "Products", "Detail", "urunler/{slug}", "products/{slug}"),
        Route(Products, "Products", "Index", "urunler", "products"),
        Route(ServiceDetail, "Services", "Detail", "hizmetler/{slug}", "services/{slug}"),
        Route(Services, "Services", "Index", "hizmetler", "services"),
        Route(Videos, "Videos", "Index", "videolar", "videos"),
        Route(Manuals, "Manuals", "Index", "kullanim-kilavuzlari", "user-manuals"),
        Route(BlogDetail, "Blog", "Detail", "blog/{slug}", "blog/{slug}"),
        Route(Blog, "Blog", "Index", "blog", "blog"),
        Route(Quote, "Quote", "Index", "teklif-al", "request-a-quote"),
        Route(Contact, "Contact", "Index", "iletisim", "contact"),
        Route(Privacy, "Legal", "Privacy", "kvkk", "privacy-policy"),
        Route(Cookies, "Legal", "Cookies", "cerez-politikasi", "cookie-policy"),
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
