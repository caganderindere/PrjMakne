namespace PrjProje.Web.Seo;

public record BreadcrumbItem(string Title, string? Url);

/// <summary>Per-page SEO data rendered by the layout (title, meta, canonical, hreflang, Open Graph, JSON-LD).</summary>
public class PageSeo
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

    /// <summary>Additional Schema.org objects serialized as JSON-LD.</summary>
    public List<object> StructuredData { get; } = new();

    public DateTime? PublishedTime { get; set; }
    public DateTime? ModifiedTime { get; set; }
}
