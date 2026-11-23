using System.Globalization;
using System.Text;
using System.Xml;
using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Web.Routing;
using PrjProje.Web.Seo;
using PrjProje.Web.ViewModels;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace PrjProje.Web.Controllers;

// Failed POSTs are re-executed here with their original method; validating antiforgery again
// would mask the real status code (e.g. 429) with a 400.
[IgnoreAntiforgeryToken]
public class ErrorController : PublicControllerBase
{
    [Route("error/{code:int}")]
    public IActionResult Index(int code)
    {
        // Status code pages re-execute without route values, so the language is taken from the original URL.
        var originalPath = HttpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalPath
                           ?? HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Path
                           ?? string.Empty;
        var firstSegment = originalPath.Trim('/').Split('/')[0];
        if (Languages.IsSupported(firstSegment))
        {
            var culture = new CultureInfo(firstSegment);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        if (originalPath.StartsWith("/admin", StringComparison.OrdinalIgnoreCase) && code == 404)
        {
            return Content("404 - Not Found");
        }

        var seo = SetSeo(code == 404 ? L["Error.NotFoundTitle"] : L["Error.GenericTitle"], null, SiteRoutes.Home);
        seo.NoIndex = true;
        Response.StatusCode = code;
        return View(new ErrorViewModel { StatusCode = code });
    }
}

public class SeoController : Controller
{
    private readonly ISitemapService _sitemapService;

    public SeoController(ISitemapService sitemapService)
    {
        _sitemapService = sitemapService;
    }

    [Route("robots.txt")]
    [OutputCache(Duration = 3600)]
    public IActionResult Robots()
    {
        var builder = new StringBuilder()
            .AppendLine("User-agent: *")
            .AppendLine("Disallow: /admin")
            .AppendLine("Disallow: /error/")
            .AppendLine("Allow: /")
            .AppendLine()
            .Append("Sitemap: ").AppendLine(Url.Absolute("/sitemap.xml"));

        return Content(builder.ToString(), "text/plain", Encoding.UTF8);
    }

    [Route("sitemap.xml")]
    [OutputCache(Duration = 3600)]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        const string sitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";
        const string xhtmlNs = "http://www.w3.org/1999/xhtml";

        var entries = new List<(IReadOnlyDictionary<string, string> Paths, DateTime? LastModified, string Priority)>();
        foreach (var key in SiteRoutes.StaticPageKeys)
        {
            var paths = Languages.All.ToDictionary(lang => lang, lang => SiteRoutes.Path(key, lang));
            entries.Add((paths, null, key == SiteRoutes.Home ? "1.0" : "0.8"));
        }

        foreach (var entry in await _sitemapService.GetDynamicEntriesAsync(cancellationToken))
        {
            var paths = entry.Slugs.ToDictionary(s => s.Key, s => SiteRoutes.Path(entry.PageKey, s.Key, s.Value));
            entries.Add((paths, entry.LastModified, entry.PageKey == SiteRoutes.ProductDetail ? "0.7" : "0.6"));
        }

        var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true, Async = true };
        await using var stream = new MemoryStream();
        await using (var writer = XmlWriter.Create(stream, settings))
        {
            await writer.WriteStartDocumentAsync();
            writer.WriteStartElement("urlset", sitemapNs);
            writer.WriteAttributeString("xmlns", "xhtml", null, xhtmlNs);

            foreach (var (paths, lastModified, priority) in entries)
            {
                foreach (var (language, path) in paths)
                {
                    writer.WriteStartElement("url", sitemapNs);
                    writer.WriteElementString("loc", sitemapNs, Url.Absolute(path));
                    if (lastModified.HasValue)
                    {
                        writer.WriteElementString("lastmod", sitemapNs, lastModified.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    }

                    writer.WriteElementString("priority", sitemapNs, priority);
                    foreach (var (alternateLanguage, alternatePath) in paths)
                    {
                        writer.WriteStartElement("xhtml", "link", xhtmlNs);
                        writer.WriteAttributeString("rel", "alternate");
                        writer.WriteAttributeString("hreflang", alternateLanguage);
                        writer.WriteAttributeString("href", Url.Absolute(alternatePath));
                        writer.WriteEndElement();
                    }

                    writer.WriteStartElement("xhtml", "link", xhtmlNs);
                    writer.WriteAttributeString("rel", "alternate");
                    writer.WriteAttributeString("hreflang", "x-default");
                    writer.WriteAttributeString("href", Url.Absolute(paths.GetValueOrDefault(Languages.Default) ?? path));
                    writer.WriteEndElement();

                    writer.WriteEndElement();
                }
            }

            writer.WriteEndElement();
            await writer.WriteEndDocumentAsync();
        }

        return File(stream.ToArray(), "application/xml; charset=utf-8");
    }
}
