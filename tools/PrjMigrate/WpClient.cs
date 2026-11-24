using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace PrjMigrate;

/// <summary>
/// Reads the old site through its public endpoints (WooCommerce Store API, WordPress pages, sitemaps),
/// so no WordPress credentials are needed.
/// </summary>
public sealed partial class WpClient
{
    private readonly HttpClient _http;

    public WpClient(Uri baseAddress)
    {
        _http = new HttpClient { BaseAddress = baseAddress, Timeout = TimeSpan.FromMinutes(2) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("PrjMigrate/1.0");
    }

    public Task<List<WpCategory>> GetCategoriesAsync() =>
        GetAllAsync<WpCategory>("wp-json/wc/store/v1/products/categories?per_page=100");

    public Task<List<WpProduct>> GetProductsAsync() =>
        GetAllAsync<WpProduct>("wp-json/wc/store/v1/products?per_page=100");

    public Task<List<WpPage>> GetPagesAsync() =>
        GetAllAsync<WpPage>("wp-json/wp/v2/pages?per_page=100&_fields=slug,link,title,content");

    /// <summary>Every &lt;loc&gt; reachable from the sitemap index (WordPress core or Yoast layout).</summary>
    public async Task<List<string>> GetSitemapUrlsAsync()
    {
        var urls = new List<string>();
        var pending = new Queue<string>(["wp-sitemap.xml"]);
        var seen = new HashSet<string>();

        while (pending.TryDequeue(out var sitemap))
        {
            if (!seen.Add(sitemap))
            {
                continue;
            }

            using var response = await _http.GetAsync(sitemap);
            if (!response.IsSuccessStatusCode)
            {
                continue;
            }

            foreach (Match match in Loc().Matches(await response.Content.ReadAsStringAsync()))
            {
                var loc = match.Groups[1].Value.Trim();
                if (loc.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    pending.Enqueue(loc);
                }
                else
                {
                    urls.Add(loc);
                }
            }
        }

        return urls;
    }

    public async Task DownloadAsync(string url, string path)
    {
        if (File.Exists(path))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var bytes = await _http.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(path, bytes);
    }

    private async Task<List<T>> GetAllAsync<T>(string url)
    {
        var all = new List<T>();
        for (var page = 1; ; page++)
        {
            var batch = await _http.GetFromJsonAsync<List<T>>($"{url}&page={page}") ?? [];
            all.AddRange(batch);
            if (batch.Count < 100)
            {
                return all;
            }
        }
    }

    [GeneratedRegex(@"<loc>\s*([^<]+?)\s*</loc>")]
    private static partial Regex Loc();
}
