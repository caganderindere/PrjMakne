using System.Net;
using System.Text.RegularExpressions;
using PrjProje.Application.Common;
using PrjProje.Web.Routing;

namespace PrjMigrate;

public static partial class Mapper
{
    // Old WordPress page slugs (first path segment) and the route each one now lives at.
    private static readonly Dictionary<string, string> PageRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["anasayfa-2"] = SiteRoutes.Home,
        ["kurumsal"] = SiteRoutes.About,
        ["hizmetler"] = SiteRoutes.Services,
        ["belgeler"] = SiteRoutes.Certificates,
        ["videolar"] = SiteRoutes.Videos,
        ["iletisim"] = SiteRoutes.Contact,
        ["kullanim-kilavuzu"] = SiteRoutes.Manuals,
        ["cihazlar-2"] = SiteRoutes.Products,
        ["urunler"] = SiteRoutes.Products,
        ["urun-kategori"] = SiteRoutes.Products,
        // Blog archives of the old theme; the new site has no matching listing, so they land on the home page.
        ["category"] = SiteRoutes.Home,
        ["tag"] = SiteRoutes.Home,
    };

    public static SeedProduct MapProduct(WpProduct product)
    {
        var html = product.Description ?? string.Empty;
        var lines = TextLines(html);

        return new SeedProduct(
            product.Slug,
            product.Categories.FirstOrDefault()?.Slug ?? string.Empty,
            WebUtility.HtmlDecode(product.Name).Trim(),
            null,
            lines.Select(l => VariantLine().Match(l)).Where(m => m.Success).Select(m => m.Groups[1].Value).Distinct().ToArray(),
            OriginalImages(product.Images.Select(i => i.Src).Concat(InlineImages(html))),
            html.Trim(),
            Specs(lines));
    }

    public static SeedCategory MapCategory(WpCategory category, int sortOrder) =>
        new(category.Slug,
            WebUtility.HtmlDecode(category.Name).Trim(),
            null,
            string.IsNullOrWhiteSpace(category.Description) ? null : category.Description.Trim(),
            category.Image is null ? null : OriginalImageUrl(category.Image.Src),
            sortOrder);

    public static SeedPage MapPage(WpPage page) =>
        new(page.Slug, WebUtility.HtmlDecode(page.Title.Rendered).Trim(), page.Content.Rendered.Trim());

    /// <summary>
    /// Strips WordPress resize suffixes (-600x640, -300x300_c, -scaled) so each picture is fetched once,
    /// at the resolution it was uploaded in; the order of first appearance is kept.
    /// </summary>
    public static string[] OriginalImages(IEnumerable<string> urls) =>
        urls.Where(u => !string.IsNullOrWhiteSpace(u)).Select(OriginalImageUrl).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public static string OriginalImageUrl(string url) => ResizeSuffix().Replace(url.Trim(), "$1");

    /// <summary>Maps every old URL to its new home. Paths that fit no rule are returned in <paramref name="unmapped"/>.</summary>
    public static IReadOnlyList<SeedRedirect> MapRedirects(IEnumerable<string> legacyUrls, out IReadOnlyList<string> unmapped)
    {
        var redirects = new Dictionary<string, SeedRedirect>();
        var misses = new List<string>();

        foreach (var url in legacyUrls)
        {
            var (path, _) = LegacyPath.Normalize(Uri.TryCreate(url, UriKind.Absolute, out var absolute) ? absolute.PathAndQuery : url);

            // The root is served by the new site itself.
            if (path == "/" || redirects.ContainsKey(path))
            {
                continue;
            }

            var target = Target(path.Split('/', StringSplitOptions.RemoveEmptyEntries));
            if (target is null)
            {
                misses.Add(path);
                continue;
            }

            redirects[path] = target;
        }

        unmapped = misses;
        return redirects.Values.OrderBy(r => r.From, StringComparer.Ordinal).ToList();
    }

    private static SeedRedirect? Target(string[] segments)
    {
        var from = "/" + string.Join('/', segments);
        switch (segments)
        {
            // English targets of products and categories are filled in once their EN slugs exist.
            case ["urunler", var slug]:
                return new SeedRedirect(from, SiteRoutes.Path(SiteRoutes.ProductDetail, Languages.Turkish, SlugHelper.Generate(slug)), null);
            case ["urun-kategori", .., var slug]:
                return new SeedRedirect(from, SiteRoutes.Path(SiteRoutes.ProductCategory, Languages.Turkish, SlugHelper.Generate(slug)), null);
            case [var first, ..] when PageRoutes.TryGetValue(first, out var key):
                return new SeedRedirect(from, SiteRoutes.Path(key, Languages.Turkish), SiteRoutes.Path(key, Languages.English));
            default:
                return null;
        }
    }

    private static Dictionary<string, string> Specs(IEnumerable<string> lines)
    {
        var specs = new Dictionary<string, string>();
        foreach (var match in lines.Select(l => SpecLine().Match(l)).Where(m => m.Success))
        {
            specs.TryAdd(match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim());
        }

        return specs;
    }

    private static IEnumerable<string> InlineImages(string html) =>
        InlineImage().Matches(html).Select(m => WebUtility.HtmlDecode(m.Groups[1].Value));

    private static List<string> TextLines(string html)
    {
        var text = WebUtility.HtmlDecode(Tag().Replace(html, "\n")).Replace(' ', ' ');
        return text.Split('\n').Select(l => l.Trim().TrimStart('•', '-', '–').Trim()).Where(l => l.Length > 0).ToList();
    }

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex Tag();

    [GeneratedRegex(@"<img[^>]+src=""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex InlineImage();

    [GeneratedRegex(@"(?:-\d+x\d+(?:_c)?|-scaled)(\.[a-z0-9]+)(?=$|\?)", RegexOptions.IgnoreCase)]
    private static partial Regex ResizeSuffix();

    // "B 300 Manuel Cihaz", "YB 500 Manuel Cihaz": a model code at the start of a short line.
    [GeneratedRegex(@"^([A-ZÇĞİÖŞÜ]{1,3} \d{2,4})(?:\s+\S.{0,60})?$")]
    private static partial Regex VariantLine();

    // "Ürün Adı : Manuel corona cihaz setleri"
    [GeneratedRegex(@"^([^:]{2,40}?)\s*:\s+(\S.{0,200})$")]
    private static partial Regex SpecLine();
}
