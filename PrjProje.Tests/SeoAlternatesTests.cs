using PrjProje.Web.Seo;

namespace PrjProje.Tests;

public class SeoAlternatesTests
{
    [Fact]
    public void Turkish_only_page_lists_turkish_and_x_default()
    {
        var seo = new PageSeo();
        seo.Alternates["tr"] = "/tr/urunler/b-400";
        Assert.Equal([("tr", "/tr/urunler/b-400"), ("x-default", "/tr/urunler/b-400")], seo.HreflangLinks());
    }

    [Fact]
    public void English_only_page_has_no_x_default()
    {
        var seo = new PageSeo();
        seo.Alternates["en"] = "/en/products/b-400";
        Assert.Equal([("en", "/en/products/b-400")], seo.HreflangLinks());
    }

    [Fact]
    public void Both_languages_are_listed_before_x_default()
    {
        var seo = new PageSeo();
        seo.Alternates["tr"] = "/tr/kurumsal";
        seo.Alternates["en"] = "/en/about-us";
        Assert.Equal(["tr", "en", "x-default"], seo.HreflangLinks().Select(l => l.Hreflang));
    }

    [Fact]
    public void Fallback_description_cuts_at_a_word_boundary()
    {
        var text = string.Join(' ', Enumerable.Repeat("elektrostatik", 25));
        var result = PageSeo.FallbackDescription(text);
        Assert.True(result.Length <= 160);
        Assert.EndsWith("…", result);
        Assert.Matches("^(elektrostatik )*elektrostatik…$", result);
    }

    [Fact]
    public void Fallback_description_strips_html_and_collapses_whitespace() =>
        Assert.Equal("Toz boya & fırın", PageSeo.FallbackDescription("<p>Toz   boya &amp;</p>\n<p><strong>fırın</strong></p>"));

    [Fact]
    public void Short_text_is_kept_whole() => Assert.Equal("Kısa metin.", PageSeo.FallbackDescription("Kısa metin."));

    [Theory]
    [InlineData(true, "noindex,follow")]
    [InlineData(false, "index,follow,max-image-preview:large")]
    public void Search_results_are_not_indexed(bool noIndex, string expected) =>
        Assert.Equal(expected, new PageSeo { NoIndex = noIndex }.Robots);
}
