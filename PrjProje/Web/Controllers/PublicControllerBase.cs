using PrjProje.Application.Common;
using PrjProje.Web.Routing;
using PrjProje.Web.Seo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace PrjProje.Web.Controllers;

public abstract class PublicControllerBase : Controller
{
    private IStringLocalizer<SharedResource>? _localizer;

    protected string Lang => Languages.Current;

    protected IStringLocalizer<SharedResource> L =>
        _localizer ??= HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();

    /// <summary>
    /// Creates the page SEO model with hreflang alternates for every language the page exists in.
    /// For slug-based pages, <paramref name="slugs"/> maps language → slug; missing languages
    /// fall back to <paramref name="fallbackKey"/> in the language switcher.
    /// </summary>
    protected PageSeo SetSeo(
        string title,
        string? description,
        string pageKey,
        IReadOnlyDictionary<string, string>? slugs = null,
        string? fallbackKey = null)
    {
        var seo = new PageSeo { Title = title, Description = description };
        var requiresSlug = SiteRoutes.RequiresSlug(pageKey);

        foreach (var language in Languages.All)
        {
            string? path = null;
            if (!requiresSlug)
            {
                path = SiteRoutes.Path(pageKey, language);
            }
            else if (slugs is not null && slugs.TryGetValue(language, out var slug))
            {
                path = SiteRoutes.Path(pageKey, language, slug);
            }

            if (path is not null)
            {
                seo.Alternates[language] = path;
            }

            seo.LanguageLinks[language] = path ?? SiteRoutes.Path(fallbackKey ?? SiteRoutes.Home, language);
        }

        seo.CanonicalPath = seo.Alternates.TryGetValue(Lang, out var canonical) ? canonical : Request.Path.Value ?? "/";
        if (pageKey != SiteRoutes.Home)
        {
            seo.Breadcrumbs.Add(new BreadcrumbItem(L["Nav.Home"], SiteRoutes.Path(SiteRoutes.Home, Lang)));
        }

        ViewData[PageSeo.ViewDataKey] = seo;
        return seo;
    }

    protected Application.Models.RequestContext CreateRequestContext() =>
        new(Lang, HttpContext.Connection.RemoteIpAddress?.ToString());
}
