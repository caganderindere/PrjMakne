using System.Text.Json;
using PrjMigrate;

namespace PrjProje.Tests.Migrate;

public class MapperTests
{
    private static T Fixture<T>(string name) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Migrate", "fixtures", name)))!;

    [Fact]
    public void Product_variants_come_from_the_model_list_in_the_description()
    {
        var product = Mapper.MapProduct(Fixture<WpProduct>("product-manuel-corona.json"));

        Assert.Equal(["B 300", "B 400", "B 500", "YB 400", "YB 500"], product.Variants);
        Assert.Equal("toz-boya-cihazlari", product.CategoryLegacySlug);
        Assert.Equal("Manuel Corona Cihaz Seti", product.NameTr);
        Assert.Contains("Ürün Adı", product.SpecsTr.Keys);
    }

    [Fact]
    public void Product_images_drop_resized_copies_and_keep_originals()
    {
        var source = Fixture<WpProduct>("product-manuel-corona.json");
        var product = Mapper.MapProduct(source);

        Assert.Equal(source.Images.Count - 2, product.Images.Length);
        Assert.DoesNotContain(product.Images, i => i.Contains("-600x640") || i.Contains("-300x300"));
        Assert.Equal(source.Images[0].Src, product.Images[0]);
    }

    [Theory]
    [InlineData("https://x.tr/wp-content/uploads/2025/07/a-600x640.jpg", "https://x.tr/wp-content/uploads/2025/07/a.jpg")]
    [InlineData("https://x.tr/wp-content/uploads/2025/07/a-300x300_c.png", "https://x.tr/wp-content/uploads/2025/07/a.png")]
    [InlineData("https://x.tr/wp-content/uploads/2025/07/3-10.jpg", "https://x.tr/wp-content/uploads/2025/07/3-10.jpg")]
    [InlineData("https://x.tr/wp-content/uploads/2025/07/big-scaled.jpeg", "https://x.tr/wp-content/uploads/2025/07/big.jpeg")]
    public void Original_image_url_strips_wordpress_resize_suffixes(string url, string expected) =>
        Assert.Equal(expected, Mapper.OriginalImageUrl(url));

    [Fact]
    public void Every_legacy_url_maps_to_a_new_route()
    {
        var redirects = Mapper.MapRedirects(Fixture<string[]>("legacy-urls.json"), out var unmapped);

        Assert.True(unmapped.Count == 0, "Unmapped: " + string.Join(", ", unmapped));
        Assert.Contains(redirects, r => r is { From: "/urunler/manuel-corona-cihaz-seti", ToTr: "/tr/urunler/manuel-corona-cihaz-seti" });
        Assert.Contains(redirects, r => r is { From: "/urun-kategori/toz-boya-cihazlari", ToTr: "/tr/urunler/kategori/toz-boya-cihazlari" });
        Assert.Contains(redirects, r => r is { From: "/kurumsal", ToTr: "/tr/kurumsal", ToEn: "/en/about-us" });
        Assert.Contains(redirects, r => r is { From: "/kullanim-kilavuzu", ToTr: "/tr/kullanim-kilavuzlari" });
        Assert.DoesNotContain(redirects, r => r.From == "/");
        Assert.Equal(redirects.Count, redirects.Select(r => r.From).Distinct().Count());
    }

    [Fact]
    public void Unknown_legacy_paths_are_reported()
    {
        Mapper.MapRedirects(["https://x.tr/bilinmeyen-sayfa/"], out var unmapped);

        Assert.Equal(["/bilinmeyen-sayfa"], unmapped);
    }
}
