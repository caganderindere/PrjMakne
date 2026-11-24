using System.Text.Encodings.Web;
using System.Text.Json;
using PrjMigrate;
using PrjProje.Infrastructure.Data.Seed;

// Usage: dotnet run --project tools/PrjMigrate [-- <old site url> <repo content folder>]
var site = new Uri(args.Length > 0 ? args[0] : "https://prestijteknikmakine.com.tr/");
var contentRoot = Path.GetFullPath(args.Length > 1 ? args[1] : "content");
var seedFolder = Path.Combine(contentRoot, "seed");
var rawFolder = Path.Combine(contentRoot, "images", "raw");
var productImages = Path.Combine(contentRoot, "images", "products");

var client = new WpClient(site);
var categories = await client.GetCategoriesAsync();
var products = await client.GetProductsAsync();
var pages = await client.GetPagesAsync();
var sitemap = await client.GetSitemapUrlsAsync();

var seedCategories = categories.Select((c, i) => Mapper.MapCategory(c, i + 1)).ToList();
var seedProducts = products.Select(Mapper.MapProduct).ToList();
var seedPages = pages.Select(Mapper.MapPage).ToList();

// Products and categories are not in the old sitemap, so their permalinks are added explicitly.
var legacyUrls = sitemap
    .Concat(pages.Select(p => p.Link))
    .Concat(categories.Select(c => c.Permalink))
    .Concat(products.Select(p => p.Permalink));
var redirects = Mapper.MapRedirects(legacyUrls, out var unmapped);

// Originals go to the git-ignored raw folder; the seed refers to the copies under images/products.
var imageCount = 0;
var localizedProducts = new List<SeedProduct>();
foreach (var product in seedProducts)
{
    var localImages = new List<string>();
    for (var i = 0; i < product.Images.Length; i++)
    {
        var url = product.Images[i];
        var fileName = Path.GetFileName(new Uri(url).LocalPath);
        var raw = Path.Combine(rawFolder, product.LegacySlug, fileName);
        try
        {
            await client.DownloadAsync(url, raw);
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"  ! {url}: {ex.Message}");
            continue;
        }

        var relative = $"products/{product.LegacySlug}/{i + 1:00}{Path.GetExtension(fileName).ToLowerInvariant()}";
        var target = Path.Combine(contentRoot, "images", relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(raw, target, overwrite: true);
        localImages.Add(relative);
        imageCount++;
    }

    localizedProducts.Add(product with { Images = localImages.ToArray() });
}

Directory.CreateDirectory(seedFolder);
var json = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
await Write("categories.json", seedCategories);
await Write("products.json", localizedProducts);
await Write("pages.json", seedPages);
await Write("redirects.json", redirects);

Console.WriteLine($"Categories: {seedCategories.Count}");
Console.WriteLine($"Products:   {localizedProducts.Count}");
Console.WriteLine($"Images:     {imageCount} (in {productImages})");
Console.WriteLine($"Pages:      {seedPages.Count}");
Console.WriteLine($"Redirects:  {redirects.Count}");
Console.WriteLine($"Unmapped:   {unmapped.Count}");
foreach (var path in unmapped)
{
    Console.WriteLine($"  - {path}");
}

return unmapped.Count == 0 ? 0 : 1;

async Task Write<T>(string name, T value) =>
    await File.WriteAllTextAsync(Path.Combine(seedFolder, name), JsonSerializer.Serialize(value, json));
