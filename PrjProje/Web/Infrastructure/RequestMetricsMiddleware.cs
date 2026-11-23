using System.Diagnostics;
using PrjProje.Infrastructure.Services.Support;

namespace PrjProje.Web.Infrastructure;

public static class RequestMetricsMiddleware
{
    public const string PageScanHeader = "X-Page-Scan";
    private const string UnmatchedRoute = "(eşleşmeyen adres)";

    /// <summary>
    /// Times every request that reaches this point. Register after static files so they are not counted.
    /// Routes are stored by pattern, never by raw URL, so bots probing random addresses cannot grow the table.
    /// </summary>
    public static IApplicationBuilder UseRequestMetrics(this IApplicationBuilder app)
    {
        var recorder = app.ApplicationServices.GetRequiredService<RequestMetricsRecorder>();

        return app.Use(async (context, next) =>
        {
            var watch = Stopwatch.StartNew();
            var threw = true;
            try
            {
                await next();
                threw = false;
            }
            finally
            {
                // The Developer page's own scan and visitors who left mid-request say nothing about real traffic.
                // /error/* is a re-execution of a request that was already counted with its real status.
                if (!context.Request.Headers.ContainsKey(PageScanHeader) && !context.RequestAborted.IsCancellationRequested
                    && !context.Request.Path.StartsWithSegments("/error", StringComparison.OrdinalIgnoreCase))
                {
                    var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? UnmatchedRoute;
                    recorder.Record(route, threw ? StatusCodes.Status500InternalServerError : context.Response.StatusCode, watch.ElapsedMilliseconds);
                }
            }
        });
    }
}
