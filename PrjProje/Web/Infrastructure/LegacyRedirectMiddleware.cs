using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;

namespace PrjProje.Web.Infrastructure;

/// <summary>
/// Sends addresses of the old WordPress site to their new page with a single 301, so search engines carry the
/// ranking over. Runs before routing; pages of the new site and the panel never touch the table.
/// </summary>
public sealed class LegacyRedirectMiddleware
{
    private static readonly string[] OwnPrefixes = ["/tr", "/admin", "/error", "/uploads", "/healthz"];
    private readonly RequestDelegate _next;

    public LegacyRedirectMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILegacyRedirectLookup lookup)
    {
        var request = context.Request;
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method) ||
            OwnPrefixes.Any(prefix => request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var (path, lang) = LegacyPath.Normalize(request.Path.Value + request.QueryString.Value);
        string? target;
        if (path == "/")
        {
            target = lang == Languages.English ? "/en" : "/" + Languages.Default;
        }
        else
        {
            var redirect = await lookup.FindAsync(path, context.RequestAborted);
            target = redirect is null ? null : lang == Languages.English ? redirect.ToPathEn ?? redirect.ToPath : redirect.ToPath;
            if (target is not null && string.Equals(target, request.Path.Value?.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
            {
                // An English page of the new site whose old Turkish address happens to be the same words (e.g. /blog).
                target = null;
            }

            if (target is not null)
            {
                lookup.RecordHit(redirect!.Id);
            }
        }

        if (target is null || string.Equals(target, request.Path.Value, StringComparison.Ordinal))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
        context.Response.Headers.Location = target;
    }
}
