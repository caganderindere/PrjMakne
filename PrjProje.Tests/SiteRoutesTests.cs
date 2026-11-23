using PrjProje.Web.Routing;

namespace PrjProje.Tests;

public class SiteRoutesTests
{
    // Spec §6 site map.
    [Theory]
    [InlineData("Home", "tr", null, "/tr")]
    [InlineData("Home", "en", null, "/en")]
    [InlineData("About", "tr", null, "/tr/kurumsal")]
    [InlineData("About", "en", null, "/en/about-us")]
    [InlineData("Certificates", "tr", null, "/tr/belgeler")]
    [InlineData("Certificates", "en", null, "/en/certificates")]
    [InlineData("Products", "tr", null, "/tr/urunler")]
    [InlineData("Products", "en", null, "/en/products")]
    [InlineData("ProductCategory", "tr", "toz-boya-cihazlari", "/tr/urunler/kategori/toz-boya-cihazlari")]
    [InlineData("ProductCategory", "en", "powder-coating-guns", "/en/products/category/powder-coating-guns")]
    [InlineData("ProductDetail", "tr", "b-400", "/tr/urunler/b-400")]
    [InlineData("ProductDetail", "en", "manual-corona-gun-set", "/en/products/manual-corona-gun-set")]
    [InlineData("Services", "tr", null, "/tr/hizmetler")]
    [InlineData("Services", "en", null, "/en/services")]
    [InlineData("ServiceDetail", "tr", "x", "/tr/hizmetler/x")]
    [InlineData("ServiceDetail", "en", "x", "/en/services/x")]
    [InlineData("Videos", "tr", null, "/tr/videolar")]
    [InlineData("Videos", "en", null, "/en/videos")]
    [InlineData("Manuals", "tr", null, "/tr/kullanim-kilavuzlari")]
    [InlineData("Manuals", "en", null, "/en/user-manuals")]
    [InlineData("Blog", "tr", null, "/tr/blog")]
    [InlineData("BlogDetail", "en", "y", "/en/blog/y")]
    [InlineData("Quote", "tr", null, "/tr/teklif-al")]
    [InlineData("Quote", "en", null, "/en/request-a-quote")]
    [InlineData("Contact", "tr", null, "/tr/iletisim")]
    [InlineData("Contact", "en", null, "/en/contact")]
    [InlineData("Privacy", "tr", null, "/tr/kvkk")]
    [InlineData("Privacy", "en", null, "/en/privacy-policy")]
    [InlineData("Cookies", "tr", null, "/tr/cerez-politikasi")]
    [InlineData("Cookies", "en", null, "/en/cookie-policy")]
    public void Paths_match_spec(string key, string lang, string? slug, string expected) =>
        Assert.Equal(expected, SiteRoutes.Path(key, lang, slug));

    [Fact]
    public void Only_spec_pages_are_routed() =>
        Assert.Equal(
            new[] { "About", "BlogDetail", "Blog", "Certificates", "Contact", "Cookies", "Home", "Manuals", "ProductCategory", "ProductDetail", "Products", "Privacy", "Quote", "ServiceDetail", "Services", "Videos" }.Order(),
            SiteRoutes.All.Select(r => r.Key).Order());
}
