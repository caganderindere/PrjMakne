using System.Net;
using System.Text.RegularExpressions;
using PrjProje.Application.Common;

namespace PrjProje.Web.Seo;

public record BreadcrumbItem(string Title, string? Url);

/// <summary>Per-page SEO data rendered by the layout (title, meta, canonical, hreflang, Open Graph, JSON-LD).</summary>
public partial class PageSeo
{
    public const string ViewDataKey = "PageSeo";

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Image { get; set; }
    public string OpenGraphType { get; set; } = "website";
    public bool NoIndex { get; set; }
    public string CanonicalPath { get; set; } = "/";

    /// <summary>Real translations of this page (language → path), rendered as hreflang alternates.</summary>
    public Dictionary<string, string> Alternates { get; } = new();

    /// <summary>Language switcher targets; falls back to a list page when a translation does not exist.</summary>
    public Dictionary<string, string> LanguageLinks { get; } = new();

    public List<BreadcrumbItem> Breadcrumbs { get; } = new();

    /// <summary>Additional Schema.org objects, each already serialized by <see cref="JsonLd"/>.</summary>
    public List<string> StructuredData { get; } = new();

    public DateTime? PublishedTime { get; set; }
    public DateTime? ModifiedTime { get; set; }

    public string Robots => NoIndex ? "noindex,follow" : "index,follow,max-image-preview:large";

    /// <summary>hreflang links: only languages the page really exists in, plus x-default when there is a Turkish version.</summary>
    public IReadOnlyList<(string Hreflang, string Path)> HreflangLinks()
    {
        var links = Languages.All.Where(Alternates.ContainsKey).Select(code => (code, Alternates[code])).ToList();
        if (Alternates.TryGetValue(Languages.Default, out var defaultPath))
        {
            links.Add(("x-default", defaultPath));
        }

        return links;
    }

    /// <summary>A meta description from body text: no HTML, single spaces, at most 160 characters, cut between words.</summary>
    public static string FallbackDescription(string? text)
    {
        const int maxLength = 160;
        var plain = WebUtility.HtmlDecode(Whitespace().Replace(Tags().Replace(text ?? string.Empty, " "), " ")).Trim();
        if (plain.Length <= maxLength)
        {
            return plain;
        }

        var cut = plain[..maxLength];
        var lastSpace = cut.LastIndexOf(' ');
        return (lastSpace > 0 ? cut[..lastSpace] : cut[..(maxLength - 1)]).TrimEnd(' ', ',', ';', ':', '.') + "…";
    }

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex Tags();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
