using Microsoft.AspNetCore.Http;
using PrjProje.Application.Interfaces;
using PrjProje.Web.Infrastructure;

namespace PrjProje.Tests;

public class LegacyRedirectMiddlewareTests
{
    private sealed class FakeLookup : ILegacyRedirectLookup
    {
        private readonly Dictionary<string, LegacyRedirectTarget> _table = new()
        {
            ["/kurumsal"] = new(1, "/tr/kurumsal", "/en/about-us"),
            ["/iletisim"] = new(2, "/tr/iletisim", null),
            ["/blog"] = new(3, "/tr/blog", "/en/blog"),
        };

        public int Lookups { get; private set; }
        public List<int> Hits { get; } = new();

        public Task<LegacyRedirectTarget?> FindAsync(string path, CancellationToken cancellationToken = default)
        {
            Lookups++;
            return Task.FromResult(_table.GetValueOrDefault(path));
        }

        public void RecordHit(int id) => Hits.Add(id);
    }

    private static async Task<(HttpContext Context, bool NextCalled)> Run(FakeLookup lookup, string path, string query = "")
    {
        var nextCalled = false;
        var middleware = new LegacyRedirectMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(query);
        await middleware.InvokeAsync(context, lookup);
        return (context, nextCalled);
    }

    [Fact]
    public async Task Registered_path_redirects_permanently_to_turkish_target()
    {
        var lookup = new FakeLookup();
        var (context, next) = await Run(lookup, "/kurumsal/");
        Assert.False(next);
        Assert.Equal(301, context.Response.StatusCode);
        Assert.Equal("/tr/kurumsal", context.Response.Headers.Location);
        Assert.Equal([1], lookup.Hits);
    }

    [Theory]
    [InlineData("/kurumsal/", "?lang=en")]
    [InlineData("/en/kurumsal/", "")]
    public async Task English_request_goes_to_english_target(string path, string query)
    {
        var (context, _) = await Run(new FakeLookup(), path, query);
        Assert.Equal("/en/about-us", context.Response.Headers.Location);
    }

    [Fact]
    public async Task English_request_without_english_target_falls_back_to_turkish()
    {
        var (context, _) = await Run(new FakeLookup(), "/iletisim", "?lang=en");
        Assert.Equal("/tr/iletisim", context.Response.Headers.Location);
    }

    [Fact]
    public async Task Unknown_path_passes_through()
    {
        var (context, next) = await Run(new FakeLookup(), "/bilinmeyen-sayfa/");
        Assert.True(next);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/tr/kurumsal")]
    [InlineData("/tr")]
    [InlineData("/admin/products")]
    public async Task New_site_and_admin_paths_never_query_the_table(string path)
    {
        var lookup = new FakeLookup();
        var (_, next) = await Run(lookup, path);
        Assert.True(next);
        Assert.Equal(0, lookup.Lookups);
    }

    [Fact]
    public async Task A_new_english_page_is_not_redirected_onto_itself()
    {
        // "/en/blog" normalizes to "/blog", whose English target is "/en/blog" again.
        var (context, next) = await Run(new FakeLookup(), "/en/blog");
        Assert.True(next);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/", "", "/tr")]
    [InlineData("/", "?lang=en", "/en")]
    public async Task Root_goes_to_the_language_home(string path, string query, string expected)
    {
        var (context, _) = await Run(new FakeLookup(), path, query);
        Assert.Equal(301, context.Response.StatusCode);
        Assert.Equal(expected, context.Response.Headers.Location);
    }
}
