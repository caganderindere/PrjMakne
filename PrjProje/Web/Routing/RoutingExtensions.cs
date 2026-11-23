using PrjProje.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace PrjProje.Web.Routing;

public static class RoutingExtensions
{
    public const string CultureRouteKey = "culture";

    public static void MapLocalizedRoutes(this IEndpointRouteBuilder endpoints)
    {
        // "/" has no route: LegacyRedirectMiddleware sends it to /tr (or /en for ?lang=en).
        foreach (var route in SiteRoutes.All)
        {
            foreach (var (languageCode, pattern) in route.Patterns)
            {
                var fullPattern = pattern.Length == 0 ? languageCode : $"{languageCode}/{pattern}";
                endpoints.MapControllerRoute(
                    name: $"{route.Key}.{languageCode}",
                    pattern: fullPattern,
                    defaults: new { controller = route.Controller, action = route.Action, culture = languageCode });
            }
        }
    }

    /// <summary>Localized path of a page in the current request language.</summary>
    public static string Localized(this IUrlHelper url, string key, string? slug = null) =>
        SiteRoutes.Path(key, Languages.Current, slug);

    public static string Localized(this IUrlHelper url, string key, string languageCode, string? slug) =>
        SiteRoutes.Path(key, languageCode, slug);

    public static string Absolute(this IUrlHelper url, string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out _))
        {
            return path;
        }

        var request = url.ActionContext.HttpContext.Request;
        var configured = url.ActionContext.HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Site:BaseUrl"];
        var baseUrl = string.IsNullOrWhiteSpace(configured) ? $"{request.Scheme}://{request.Host}" : configured.TrimEnd('/');
        return baseUrl + (path.StartsWith('/') ? path : "/" + path);
    }
}
