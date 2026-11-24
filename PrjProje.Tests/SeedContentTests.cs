using System.Net;
using System.Text.RegularExpressions;
using PrjProje.Infrastructure.Data.Seed;

namespace PrjProje.Tests;

public partial class SeedContentTests
{
    private static readonly Lazy<SeedContent> Content = new(() => SeedContent.Load(FindContentRoot()));

    private static SeedContent Seed => Content.Value;

    private static IEnumerable<(string Owner, CatalogText Text)> AllTexts() =>
        Seed.Catalog.Services.Concat(Seed.Catalog.Categories).Concat(Seed.Catalog.Products)
            .SelectMany(e => new[] { ($"{e.LegacySlug ?? e.Key}/tr", e.Tr), ($"{e.LegacySlug ?? e.Key}/en", e.En) });

    [Fact]
    public void Every_exported_category_and_product_has_catalog_copy()
    {
        var categories = Seed.Catalog.Categories.Select(c => c.LegacySlug).ToHashSet();
        var products = Seed.Catalog.Products.Select(p => p.LegacySlug).ToHashSet();

        Assert.Empty(Seed.Categories.Select(c => c.LegacySlug).Where(s => !categories.Contains(s)));
        Assert.Empty(Seed.Products.Select(p => p.LegacySlug).Where(s => !products.Contains(s)));
        Assert.Equal(Seed.Products.Count, products.Count);
    }

    [Fact]
    public void Every_text_is_complete_and_meta_lengths_fit_search_results()
    {
        var problems = new List<string>();
        foreach (var (owner, text) in AllTexts())
        {
            if (new[] { text.Name, text.Slug, text.ShortDescription, text.Description, text.MetaTitle, text.MetaDescription }.Any(string.IsNullOrWhiteSpace))
            {
                problems.Add($"{owner}: empty field");
            }

            if (text.MetaTitle.Length > 60)
            {
                problems.Add($"{owner}: MetaTitle {text.MetaTitle.Length}");
            }

            if (text.MetaDescription.Length is < 70 or > 160)
            {
                problems.Add($"{owner}: MetaDescription {text.MetaDescription.Length}");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void Slugs_are_unique_within_each_language_and_type()
    {
        foreach (var group in new[] { Seed.Catalog.Services, Seed.Catalog.Categories, Seed.Catalog.Products })
        {
            Assert.Equal(group.Count, group.Select(e => e.Tr.Slug).Distinct().Count());
            Assert.Equal(group.Count, group.Select(e => e.En.Slug).Distinct().Count());
            Assert.All(group.SelectMany(e => new[] { e.Tr.Slug, e.En.Slug }), s => Assert.Equal(Application.Common.SlugHelper.Generate(s), s));
        }
    }

    [Fact]
    public void No_two_descriptions_share_a_twelve_word_run()
    {
        var problems = new List<string>();
        var texts = AllTexts().ToList();
        var shingles = texts.Select(t => (t.Owner, Set: Shingles(t.Text.Description, 12))).ToList();

        for (var i = 0; i < shingles.Count; i++)
        {
            for (var j = i + 1; j < shingles.Count; j++)
            {
                if (shingles[i].Set.Overlaps(shingles[j].Set))
                {
                    problems.Add($"{shingles[i].Owner} ~ {shingles[j].Owner}");
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void Numeric_technical_values_come_from_the_source_text()
    {
        var problems = new List<string>();
        var sources = Seed.Products.ToDictionary(p => p.LegacySlug, p => PlainText(p.LegacyHtmlTr));

        foreach (var product in Seed.Catalog.Products)
        {
            var source = sources[product.LegacySlug!];
            foreach (var text in new[] { product.Tr, product.En })
            {
                foreach (Match match in Measurement().Matches(PlainText(text.Description)))
                {
                    var number = match.Groups["n"].Value.Replace('.', ',');
                    if (!source.Contains(number) && !source.Contains(number.Replace(',', '.')))
                    {
                        problems.Add($"{product.LegacySlug}: '{match.Value}' is not in the source");
                    }
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void Every_legacy_product_and_category_url_has_a_redirect()
    {
        var froms = Seed.Redirects.Select(r => r.From).ToHashSet();

        Assert.All(Seed.Products, p => Assert.Contains($"/urunler/{p.LegacySlug}", froms));
        Assert.All(Seed.Categories, c => Assert.Contains($"/urun-kategori/{c.LegacySlug}", froms));
    }

    [Fact]
    public void Every_product_image_in_the_export_exists_on_disk()
    {
        var missing = Seed.Products.SelectMany(p => p.Images)
            .Where(i => !File.Exists(Path.Combine(Seed.Root, "images", i)))
            .ToList();

        Assert.True(missing.Count == 0, string.Join(", ", missing));
    }

    private static HashSet<string> Shingles(string html, int size)
    {
        var words = Regex.Split(PlainText(html).ToLowerInvariant(), @"[^\p{L}\p{N}]+").Where(w => w.Length > 0).ToArray();
        var set = new HashSet<string>();
        for (var i = 0; i + size <= words.Length; i++)
        {
            set.Add(string.Join(' ', words, i, size));
        }

        return set;
    }

    private static string PlainText(string html) => WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " "));

    private static string FindContentRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "content");
            if (File.Exists(Path.Combine(candidate, "seed", "catalog.json")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("content/seed/catalog.json not found above the test output folder.");
    }

    // A number followed by a unit: "15 cm", "180°C", "%99,9", "8 saat", "250 derece".
    [GeneratedRegex(@"%\s?(?<n>\d+(?:[.,]\d+)?)|(?<n>\d+(?:[.,]\d+)?)\s?(?:mm|cm|m²|m|°C|°|C\b|derece|saat|hours|kW|bar|%)", RegexOptions.IgnoreCase)]
    private static partial Regex Measurement();
}
