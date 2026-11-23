using System.Text.Json;
using PrjProje.Application.Models;
using PrjProje.Web.Seo;

namespace PrjProje.Tests;

public class JsonLdTests
{
    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    private static SiteSettingsDto Settings() => new()
    {
        InstagramUrl = "https://www.instagram.com/prj",
        FacebookUrl = "https://www.facebook.com/prj",
        YouTubeUrl = "https://www.youtube.com/@prj"
    };

    [Fact]
    public void Organization_describes_the_company_as_a_local_business()
    {
        var org = Parse(JsonLd.Organization(Settings(), "https://www.prestijteknikmakine.com.tr"));

        Assert.Equal(["Organization", "LocalBusiness"], org.GetProperty("@type").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal("PRJ Proje Makina A.Ş.", org.GetProperty("name").GetString());
        Assert.Equal("Prestij Teknik Makine", org.GetProperty("alternateName").GetString());
        Assert.Equal("+902125440104", org.GetProperty("telephone").GetString());
        var address = org.GetProperty("address");
        Assert.Equal("Arnavutköy", address.GetProperty("addressLocality").GetString());
        Assert.Equal("34281", address.GetProperty("postalCode").GetString());
        Assert.Equal("TR", address.GetProperty("addressCountry").GetString());
        Assert.Equal(3, org.GetProperty("sameAs").GetArrayLength());
    }

    [Fact]
    public void Organization_json_cannot_close_its_script_tag()
    {
        var settings = Settings();
        settings.CompanyName = "</script><script>alert(1)</script>";
        Assert.DoesNotContain("</script>", JsonLd.Organization(settings, "https://x"));
    }

    [Fact]
    public void Product_has_brand_and_models_but_no_offer()
    {
        var product = new ProductDetailDto { Name = "Manuel Corona Cihaz Seti", ShortDescription = "Kısa", CategoryName = "Toz Boya Cihazları", Variants = ["B 300", "YB 500"] };
        var json = Parse(JsonLd.Product(product, "https://x/tr/urunler/a", null, "PRJ Proje Makina A.Ş."));

        Assert.Equal("Product", json.GetProperty("@type").GetString());
        Assert.Equal("Brand", json.GetProperty("brand").GetProperty("@type").GetString());
        Assert.Equal("PRJ", json.GetProperty("brand").GetProperty("name").GetString());
        Assert.Equal(2, json.GetProperty("model").GetArrayLength());
        Assert.False(json.TryGetProperty("offers", out _));
        Assert.False(json.TryGetProperty("image", out _));
    }

    [Fact]
    public void Breadcrumb_numbers_items_from_one()
    {
        var json = Parse(JsonLd.Breadcrumb([("Ana Sayfa", "https://x/tr"), ("Ürünler", "https://x/tr/urunler")]));
        var items = json.GetProperty("itemListElement").EnumerateArray().ToList();
        Assert.Equal([1, 2], items.Select(i => i.GetProperty("position").GetInt32()));
        Assert.Equal("https://x/tr/urunler", items[1].GetProperty("item").GetString());
    }

    [Fact]
    public void Video_points_at_youtube()
    {
        var json = Parse(JsonLd.Video("Kurulum", "dQw4w9WgXcQ", "Tesis kurulumu"));
        Assert.Equal("VideoObject", json.GetProperty("@type").GetString());
        Assert.Equal("https://www.youtube.com/embed/dQw4w9WgXcQ", json.GetProperty("embedUrl").GetString());
        Assert.Equal("https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg", json.GetProperty("thumbnailUrl")[0].GetString());
    }
}
