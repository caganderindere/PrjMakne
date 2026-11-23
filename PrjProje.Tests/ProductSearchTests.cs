using PrjProje.Infrastructure.Services;

namespace PrjProje.Tests;

public class ProductSearchTests
{
    private static readonly (string Id, ProductSearch.Document Doc)[] Catalogue =
    [
        ("corona", new("Manuel Corona Toz Boya Cihazı", "Toz Boya Cihazları", "Elektrostatik yükleme ile homojen toz boya uygulaması. Yüksek gerilim kaskadı", "B 400, B-500", "PRJ-MC-400")),
        ("tribo", new("Manuel Tribo Toz Boya Cihazı", "Toz Boya Cihazları", "Sürtünme ile yükleme", null, "PRJ-MT-100, PRJ-MT-200")),
        ("kabin", new("Siklon Otomatik Kabin", "Boya Uygulama Kabinleri", "Hızlı renk değişimi, siklon geri kazanım", null, null)),
        ("firin", new("Boya Pişirme Fırını", "Boya Pişirme Fırınları", "Doğalgaz brülörlü sıcak hava sirkülasyonu", "F2000", null)),
        ("hat", new("Kimyasal Yüzey Temizlik Hattı", "Kimyasal Yüzey Temizlik Hatları", "Daldırma tipi ön işlem", null, null)),
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
    [InlineData("B 400")]
    [InlineData("b400")]
    [InlineData("B-500")]
    [InlineData("prj mc 400")]
    public void Model_codes_match_in_any_format(string text) => Assert.Equal("corona", Search(text)[0]);

    [Fact]
    public void Each_comma_separated_service_code_matches_exactly() => Assert.Equal(["tribo"], Search("PRJMT200"));

    [Fact]
    public void Exact_code_beats_a_name_that_merely_contains_the_digits() => Assert.Equal(["firin"], Search("F2000"));

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
    public void Same_query_always_gives_the_same_order() => Assert.Equal(Search("toz"), Search("toz"));
}
