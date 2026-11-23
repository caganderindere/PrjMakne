using System.Globalization;
using PrjProje.Application.Common;

namespace PrjProje.Tests;

public class SlugHelperTests
{
    [Theory]
    [InlineData("Özel İmalat Ürünler", "ozel-imalat-urunler")]
    [InlineData("Kimyasal Yüzey Temizlik Hatları", "kimyasal-yuzey-temizlik-hatlari")]
    [InlineData("B 400 Manuel Cihaz", "b-400-manuel-cihaz")]
    [InlineData("  Şişe -- Çığ  ", "sise-cig")]
    [InlineData("İ", "i")]
    [InlineData("TOZ BOYA FIRINI", "toz-boya-firini")]
    public void Turkish_text_becomes_ascii_slug(string text, string expected) =>
        Assert.Equal(expected, SlugHelper.Generate(text));

    [Fact]
    public void Result_does_not_depend_on_current_culture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            Assert.Equal("ilk-isik", SlugHelper.Generate("İLK IŞIK"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
