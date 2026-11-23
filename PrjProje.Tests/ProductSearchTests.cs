using PrjProje.Infrastructure.Services;

namespace PrjProje.Tests;

public class ProductSearchTests
{
    private static readonly (string Id, ProductSearch.Document Doc)[] Catalogue =
    [
        ("corona", new("Manuel Corona Cihaz Seti", "Toz Boya Cihazları", "Elektrostatik yükleme ile homojen toz boya uygulaması. Yüksek gerilim kaskadı", "B 300, B 400, B 500, YB 400, YB 500")),
        ("tribo", new("Manuel Tribo Toz Boya Cihazı", "Toz Boya Cihazları", "Sürtünme ile yükleme", "T 100, T 200")),
        ("kabin", new("Siklon Otomatik Kabin", "Boya Uygulama Kabinleri", "Hızlı renk değişimi, siklon geri kazanım", null)),
        ("firin", new("Boya Pişirme Fırını", "Boya Pişirme Fırınları", "Doğalgaz brülörlü sıcak hava sirkülasyonu", "F2000")),
        ("hat", new("Kimyasal Yüzey Temizlik Hattı", "Kimyasal Yüzey Temizlik Hatları", "Daldırma tipi ön işlem", null)),
    ];

    private static string[] Search(string text)
    {
        var query = ProductSearch.ParseQuery(text)!;
        return Catalogue
            .Select(p => (p.Id, Score: ProductSearch.Score(p.Doc, query)))
            .Where(p => p.Score > 0)
            .OrderByDescending(p => p.Score)
            .Select(p => p.Id)
            .ToArray();
    }

    [Theory]
    [InlineData("yb500")]
    [InlineData("YB-500")]
    [InlineData("YB 500")]
    [InlineData("b400")]
    public void Variant_codes_match_in_any_format(string text) => Assert.Equal("corona", Search(text)[0]);

    [Fact]
    public void Each_variant_code_matches_exactly() => Assert.Equal(["tribo"], Search("T200"));

    [Fact]
    public void Exact_code_beats_a_name_that_merely_contains_the_digits() => Assert.Equal(["firin"], Search("F2000"));

    [Fact]
    public void Name_search_ignores_turkish_case() => Assert.Equal("kabin", Search("SİKLON")[0]);

    [Theory]
    [InlineData("toz boya cihazi")]
    [InlineData("TOZ BOYA CİHAZI")]
    [InlineData("pisirme")]
    [InlineData("PİŞİRME FIRINI")]
    public void Turkish_letters_and_case_do_not_matter(string text) => Assert.Contains(Search(text)[0], new[] { "corona", "tribo", "firin" });

    [Fact]
    public void Name_match_ranks_above_description_match()
    {
        Assert.Equal(["kabin"], Search("siklon"));

        var toz = Search("toz");
        Assert.Equal(2, toz.Length);
        Assert.DoesNotContain("hat", toz);
    }

    [Fact]
    public void Turkish_suffixes_still_match() => Assert.Equal("kabin", Search("kabinler")[0]);

    [Fact]
    public void Every_word_must_match_somewhere() => Assert.Empty(Search("corona vida"));

    [Fact]
    public void Prefix_typing_matches() => Assert.Equal("hat", Search("temiz")[0]);

    [Fact]
    public void Punctuation_only_query_is_ignored() => Assert.Null(ProductSearch.ParseQuery(" - / "));

    [Fact]
    public void Variant_codes_are_trimmed_and_deduplicated_across_formats() =>
        Assert.Equal(["B 300", "YB 500"], ProductSearch.DistinctCodes(["  B  300 ", "YB 500", "yb-500", "", null, "b300"]));

    [Fact]
    public void Same_query_always_gives_the_same_order() => Assert.Equal(Search("toz"), Search("toz"));
}
