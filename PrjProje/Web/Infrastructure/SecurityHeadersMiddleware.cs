namespace PrjProje.Web.Infrastructure;

public static class SecurityHeadersMiddleware
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://challenges.cloudflare.com https://www.googletagmanager.com; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
        "font-src 'self' data: https://fonts.gstatic.com https://cdnjs.cloudflare.com; " +
        "img-src 'self' data: blob: https:; " +
        "frame-src https://www.google.com https://maps.google.com https://challenges.cloudflare.com https://lookerstudio.google.com https://datastudio.google.com; " +
        // pdf.js (product catalog) starts its CDN worker through a blob: URL.
        "worker-src 'self' blob:; " +
        "object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'self'";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, bool isDevelopment)
    {
        // Development tooling (browser refresh) needs a websocket connection to another localhost port.
        // Google Analytics sends its hits to these hosts (only after the visitor accepts cookies).
        const string analyticsHosts = "https://*.google-analytics.com https://*.analytics.google.com https://www.googletagmanager.com";
        var connectSource = isDevelopment ? $"connect-src 'self' {analyticsHosts} ws: wss: http://localhost:*" : $"connect-src 'self' {analyticsHosts}";
        var policy = $"{ContentSecurityPolicy}; {connectSource}";

        return app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "SAMEORIGIN";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

            // CSP only on HTML: on a PDF, object-src 'none' blocks the browser's built-in viewer and leaves a blank page.
            context.Response.OnStarting(() =>
            {
                if (context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true)
                {
                    context.Response.Headers.ContentSecurityPolicy = policy;
                }
                return Task.CompletedTask;
            });
            await next();
        });
    }
}
