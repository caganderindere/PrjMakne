using PrjProje.Application.Common;
using PrjProje.Web.Routing;

namespace PrjProje.Web.Infrastructure;

/// <summary>
/// One address per page: public paths with a trailing slash or capital letters, and pages that were renamed,
/// get a single 301 to the canonical lowercase path (the query string is kept). Only the language-prefixed
/// public pages are touched; uploads, the panel and other files keep their exact paths.
/// </summary>
public sealed class CanonicalPathMiddleware
{
    private readonly RequestDelegate _next;

    public CanonicalPathMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var path = request.Path.Value;
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method) ||
            string.IsNullOrEmpty(path) || !Languages.All.Any(lang => request.Path.StartsWithSegments("/" + lang, StringComparison.OrdinalIgnoreCase)))
        {
            return _next(context);
        }

        var canonical = path.Length > 1 ? path.TrimEnd('/').ToLowerInvariant() : path;
        if (SiteRoutes.MovedPaths.TryGetValue(canonical, out var moved))
        {
            canonical = moved;
        }

        if (string.Equals(canonical, path, StringComparison.Ordinal))
        {
            return _next(context);
        }

        context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
        context.Response.Headers.Location = canonical + request.QueryString.Value;
        return Task.CompletedTask;
    }
}
