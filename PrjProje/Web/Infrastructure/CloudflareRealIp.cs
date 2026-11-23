using System.Net;
using PrjProje.Application.Interfaces;

namespace PrjProje.Web.Infrastructure;

/// <summary>Restores the visitor's address behind Cloudflare, so rate limits and form records see the real client.</summary>
public static class CloudflareRealIp
{
    public const string ViaProxyItem = "Cloudflare.ViaProxy";
    private const string ClientIpHeader = "CF-Connecting-IP";

    // shortcut: fixed copy of cloudflare.com/ips, update it if Cloudflare publishes new ranges.
    private static readonly IPNetwork[] Ranges = new[]
    {
        "173.245.48.0/20", "103.21.244.0/22", "103.22.200.0/22", "103.31.4.0/22", "141.101.64.0/18",
        "108.162.192.0/18", "190.93.240.0/20", "188.114.96.0/20", "197.234.240.0/22", "198.41.128.0/17",
        "162.158.0.0/15", "104.16.0.0/13", "104.24.0.0/14", "172.64.0.0/13", "131.0.72.0/22",
        "2400:cb00::/32", "2606:4700::/32", "2803:f800::/32", "2405:b500::/32", "2405:8100::/32",
        "2a06:98c0::/29", "2c0f:f248::/32"
    }.Select(range => IPNetwork.Parse(range)).ToArray();

    public static bool IsCloudflare(IPAddress? address)
    {
        if (address is null)
        {
            return false;
        }

        var normalized = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
        return Ranges.Any(range => range.Contains(normalized));
    }

    /// <summary>The client address, only when the connection really comes from Cloudflare; anyone else could forge the header.</summary>
    public static IPAddress? ResolveClientIp(IPAddress? peer, string? header) =>
        IsCloudflare(peer) && IPAddress.TryParse(header?.Trim(), out var client) ? client : null;

    /// <summary>Register before the rate limiter and after static files, which do not need the database lookup.</summary>
    public static IApplicationBuilder UseCloudflareRealIp(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            try
            {
                var settings = await context.RequestServices.GetRequiredService<ICloudflareService>().GetAsync(context.RequestAborted);
                if (settings.UseProxy && ResolveClientIp(context.Connection.RemoteIpAddress, context.Request.Headers[ClientIpHeader]) is { } client)
                {
                    context.Items[ViaProxyItem] = true;
                    context.Connection.RemoteIpAddress = client;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // A settings lookup failure must not take the whole site down.
                context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(CloudflareRealIp))
                    .LogWarning(exception, "Cloudflare ayarları okunamadı.");
            }

            await next();
        });
}
