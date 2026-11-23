using PrjProje.Application.Common;

namespace PrjProje.Tests;

public class LegacyPathTests
{
    [Theory]
    [InlineData("/urunler/Manuel-Corona-Cihaz-Seti/", "/urunler/manuel-corona-cihaz-seti", null)]
    [InlineData("/urunler/manuel-corona-cihaz-seti", "/urunler/manuel-corona-cihaz-seti", null)]
    [InlineData("/urun-kategori/toz-boya-cihazlar%C4%B1/", "/urun-kategori/toz-boya-cihazları", null)]
    [InlineData("/KURUMSAL/", "/kurumsal", null)]
    [InlineData("/kurumsal/?lang=en", "/kurumsal", "en")]
    [InlineData("/en/kurumsal/", "/kurumsal", "en")]
    [InlineData("/en", "/", "en")]
    [InlineData("/?lang=en", "/", "en")]
    [InlineData("/", "/", null)]
    [InlineData("/iletisim/?utm_source=x", "/iletisim", null)]
    [InlineData("//kurumsal//", "/kurumsal", null)]
    public void Normalize_folds_every_variant_to_one_key(string input, string path, string? lang) =>
        Assert.Equal((path, lang), LegacyPath.Normalize(input));

    [Fact]
    public void Malformed_escapes_do_not_throw() =>
        Assert.Equal(("/a%zz", null), LegacyPath.Normalize("/a%zz/"));
}
