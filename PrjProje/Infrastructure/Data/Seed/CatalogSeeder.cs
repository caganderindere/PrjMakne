using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Domain.Entities;
using PrjProje.Web.Routing;

namespace PrjProje.Infrastructure.Data.Seed;

/// <summary>
/// Loads the migrated catalogue (content/seed) into an empty database. Each record is matched by its Turkish slug,
/// so a second run adds only what is missing and never overwrites what was edited in the panel.
/// </summary>
public static class CatalogSeeder
{
    private const string Tr = Languages.Turkish;
    private const string En = Languages.English;

    // Shown in the home page "featured" strip until the panel says otherwise.
    private static readonly HashSet<string> Featured = new(StringComparer.Ordinal)
    {
        "manuel-corona-cihaz-seti", "filtreli-otomatik-kabin", "siklon-otomatik-kabin",
        "tunel-tipi-otomatik-firin", "kardan-konveyor-sistemleri", "tunel-tipi-spreyli-temizlik",
    };

    public static async Task SeedAsync(ApplicationDbContext db, IFileStorageService storage, string contentRoot, ILogger logger)
    {
        if (!File.Exists(Path.Combine(contentRoot, "seed", "catalog.json")))
        {
            logger.LogInformation("No seed content at {Root}; catalogue seeding skipped.", contentRoot);
            return;
        }

        var content = SeedContent.Load(contentRoot);
        await SeedServicesAsync(db, content.Catalog.Services);
        var categories = await SeedCategoriesAsync(db, content);
        await SeedProductsAsync(db, storage, content, categories, logger);
        await FillCategoryImagesAsync(db, categories.Values);
        await SeedRedirectsAsync(db, content);
    }

    private static async Task SeedServicesAsync(ApplicationDbContext db, IReadOnlyList<CatalogEntry> services)
    {
        var existing = await db.ServiceTranslations.Where(t => t.LanguageCode == Tr).Select(t => t.Slug).ToListAsync();
        var order = 0;
        foreach (var entry in services)
        {
            order++;
            if (existing.Contains(entry.Tr.Slug))
            {
                continue;
            }

            db.Services.Add(new Service
            {
                SortOrder = order,
                Translations =
                {
                    ServiceText(Tr, entry.Tr),
                    ServiceText(En, entry.En),
                },
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task<Dictionary<string, ProductCategory>> SeedCategoriesAsync(ApplicationDbContext db, SeedContent content)
    {
        var byLegacySlug = new Dictionary<string, ProductCategory>();
        foreach (var entry in content.Catalog.Categories)
        {
            var category = await db.ProductCategories
                .FirstOrDefaultAsync(c => c.Translations.Any(t => t.LanguageCode == Tr && t.Slug == entry.Tr.Slug));

            if (category is null)
            {
                category = new ProductCategory
                {
                    SortOrder = content.Categories.FirstOrDefault(c => c.LegacySlug == entry.LegacySlug)?.SortOrder ?? 0,
                    Translations =
                    {
                        CategoryText(Tr, entry.Tr),
                        CategoryText(En, entry.En),
                    },
                };
                db.ProductCategories.Add(category);
            }

            byLegacySlug[entry.LegacySlug!] = category;
        }

        await db.SaveChangesAsync();
        return byLegacySlug;
    }

    private static async Task SeedProductsAsync(
        ApplicationDbContext db, IFileStorageService storage, SeedContent content, Dictionary<string, ProductCategory> categories, ILogger logger)
    {
        var existing = await db.ProductTranslations.Where(t => t.LanguageCode == Tr).Select(t => t.Slug).ToListAsync();
        var exports = content.Products.ToDictionary(p => p.LegacySlug);
        var order = 0;

        foreach (var entry in content.Catalog.Products)
        {
            order++;
            if (existing.Contains(entry.Tr.Slug) || !exports.TryGetValue(entry.LegacySlug!, out var export)
                || !categories.TryGetValue(export.CategoryLegacySlug, out var category))
            {
                continue;
            }

            var product = new Product
            {
                CategoryId = category.Id,
                SortOrder = order,
                IsFeatured = Featured.Contains(export.LegacySlug),
                Translations =
                {
                    ProductText(Tr, entry.Tr),
                    ProductText(En, entry.En),
                },
            };

            for (var i = 0; i < export.Variants.Length; i++)
            {
                product.Variants.Add(new ProductVariant { Code = export.Variants[i], SortOrder = i });
            }

            var imageOrder = 0;
            foreach (var image in export.Images)
            {
                var path = await StoreImageAsync(storage, Path.Combine(content.Root, "images", image), logger);
                if (path is null)
                {
                    continue;
                }

                product.Image ??= path;
                product.Images.Add(new ProductImage { ImagePath = path, SortOrder = imageOrder++, AltTr = entry.Tr.Name, AltEn = entry.En.Name });
            }

            db.Products.Add(product);
            await db.SaveChangesAsync();
        }
    }

    // The old categories had no pictures of their own; a new one borrows the photo of its first product.
    private static async Task FillCategoryImagesAsync(ApplicationDbContext db, IEnumerable<ProductCategory> categories)
    {
        foreach (var category in categories.Where(c => c.Image is null))
        {
            category.Image = await db.Products.Where(p => p.CategoryId == category.Id && p.Image != null)
                .OrderBy(p => p.SortOrder).Select(p => p.Image).FirstOrDefaultAsync();
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Product and category URLs point at the slugs of the written catalogue (some differ from the old ones and
    /// only the catalogue knows the English slug); every other old URL keeps the target the export gave it.
    /// </summary>
    private static async Task SeedRedirectsAsync(ApplicationDbContext db, SeedContent content)
    {
        var existing = (await db.LegacyRedirects.Select(r => r.FromPath).ToListAsync()).ToHashSet(StringComparer.Ordinal);
        var products = content.Catalog.Products.ToDictionary(p => p.LegacySlug!);
        var categories = content.Catalog.Categories.ToDictionary(c => c.LegacySlug!);

        foreach (var redirect in content.Redirects)
        {
            if (!existing.Add(redirect.From))
            {
                continue;
            }

            var (toTr, toEn) = (redirect.ToTr, redirect.ToEn);
            var segments = redirect.From.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments is ["urunler", var slug] && products.TryGetValue(slug, out var product))
            {
                (toTr, toEn) = (SiteRoutes.Path(SiteRoutes.ProductDetail, Tr, product.Tr.Slug), SiteRoutes.Path(SiteRoutes.ProductDetail, En, product.En.Slug));
            }
            else if (segments is ["urun-kategori", .., var categorySlug] && categories.TryGetValue(categorySlug, out var category))
            {
                (toTr, toEn) = (SiteRoutes.Path(SiteRoutes.ProductCategory, Tr, category.Tr.Slug), SiteRoutes.Path(SiteRoutes.ProductCategory, En, category.En.Slug));
            }

            db.LegacyRedirects.Add(new LegacyRedirect { FromPath = redirect.From, ToPath = toTr, ToPathEn = toEn });
        }

        await db.SaveChangesAsync();
    }

    private static async Task<string?> StoreImageAsync(IFileStorageService storage, string path, ILogger logger)
    {
        if (!File.Exists(path))
        {
            logger.LogWarning("Seed image {Path} is missing.", path);
            return null;
        }

        await using var stream = File.OpenRead(path);
        var file = new FormFile(stream, 0, stream.Length, "file", Path.GetFileName(path));
        var result = await storage.SaveImageAsync(file, ImageFolder.Products);
        if (!result.Succeeded)
        {
            logger.LogWarning("Seed image {Path} was rejected: {Errors}", path, string.Join("; ", result.Errors));
            return null;
        }

        return result.Value!.RelativePath;
    }

    private static ServiceTranslation ServiceText(string language, CatalogText text) => new()
    {
        LanguageCode = language,
        Title = text.Name,
        Slug = text.Slug,
        Summary = text.ShortDescription,
        Content = text.Description,
        MetaTitle = text.MetaTitle,
        MetaDescription = text.MetaDescription,
    };

    private static ProductCategoryTranslation CategoryText(string language, CatalogText text) => new()
    {
        LanguageCode = language,
        Name = text.Name,
        Slug = text.Slug,
        Description = text.Description,
        MetaTitle = text.MetaTitle,
        MetaDescription = text.MetaDescription,
    };

    private static ProductTranslation ProductText(string language, CatalogText text) => new()
    {
        LanguageCode = language,
        Name = text.Name,
        Slug = text.Slug,
        ShortDescription = text.ShortDescription,
        Description = text.Description,
        MetaTitle = text.MetaTitle,
        MetaDescription = text.MetaDescription,
    };
}
