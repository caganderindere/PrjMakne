using System.Text.Json;

namespace PrjProje.Infrastructure.Data.Seed;

// Exported from the old WordPress site by tools/PrjMigrate (content/seed/*.json).
public sealed record SeedCategory(string LegacySlug, string NameTr, string? NameEn, string? DescriptionTr, string? Image, int SortOrder);

public sealed record SeedProduct(
    string LegacySlug,
    string CategoryLegacySlug,
    string NameTr,
    string? NameEn,
    string[] Variants,
    string[] Images,
    string LegacyHtmlTr,
    Dictionary<string, string> SpecsTr);

public sealed record SeedPage(string LegacySlug, string TitleTr, string LegacyHtmlTr);

public sealed record SeedRedirect(string From, string ToTr, string? ToEn);

// Written for the new site (content/seed/catalog.json): the published TR/EN copy, keyed by the legacy slug.
public sealed record CatalogText(string Name, string Slug, string ShortDescription, string Description, string MetaTitle, string MetaDescription);

public sealed record CatalogEntry(string? LegacySlug, string? Key, CatalogText Tr, CatalogText En);

public sealed record Catalog(IReadOnlyList<CatalogEntry> Services, IReadOnlyList<CatalogEntry> Categories, IReadOnlyList<CatalogEntry> Products);

/// <summary>The seed folder as one unit: the migration export plus the written catalog copy.</summary>
public sealed record SeedContent(
    string Root,
    Catalog Catalog,
    IReadOnlyList<SeedCategory> Categories,
    IReadOnlyList<SeedProduct> Products,
    IReadOnlyList<SeedRedirect> Redirects)
{
    public static SeedContent Load(string root)
    {
        var seed = Path.Combine(root, "seed");
        return new SeedContent(
            root,
            Read<Catalog>(seed, "catalog.json"),
            Read<List<SeedCategory>>(seed, "categories.json"),
            Read<List<SeedProduct>>(seed, "products.json"),
            Read<List<SeedRedirect>>(seed, "redirects.json"));
    }

    private static T Read<T>(string folder, string name) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(folder, name))) ?? throw new InvalidDataException($"{name} is empty.");
}
