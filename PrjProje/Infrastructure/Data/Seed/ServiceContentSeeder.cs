using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PrjProje.Application.Common;
using PrjProje.Domain.Entities;

namespace PrjProje.Infrastructure.Data.Seed;

public sealed record SeedServiceText(string Summary, string MetaDescription, string Specifications, string Content);
public sealed record SeedServiceGallery(string Tr, string En);
public sealed record SeedService(
    SeedServiceGallery Gallery, IReadOnlyList<string> Photos, IReadOnlyList<string> ProductCategories, SeedServiceText Tr, SeedServiceText En);

/// <summary>
/// The detailed service pages (content/seed/services.json): long text with photos, specifications, a gallery
/// category of its own and the related product categories. Runs after the photo import. A field is only written
/// while it is empty or still holds the first catalogue text, so anything edited in the panel stays as it is.
/// Photos are referenced as "{{photo:gallery-folder/NNN}}" and resolve to the imported gallery picture.
/// </summary>
public static partial class ServiceContentSeeder
{
    private const string Tr = Languages.Turkish;
    private const string En = Languages.English;

    public static async Task SeedAsync(ApplicationDbContext db, string contentRoot)
    {
        var file = Path.Combine(contentRoot, "seed", "services.json");
        if (!File.Exists(file) || !await db.GalleryItems.AnyAsync())
        {
            return;
        }

        var seed = JsonSerializer.Deserialize<Dictionary<string, SeedService>>(File.ReadAllText(file))
                   ?? throw new InvalidOperationException("services.json is empty.");
        var catalog = SeedContent.Load(contentRoot).Catalog.Services.ToDictionary(s => s.Tr.Slug);
        var photos = await LoadPhotosAsync(db);

        foreach (var (slug, entry) in seed)
        {
            var service = await db.Services
                .Include(s => s.Translations)
                .Include(s => s.ProductCategories)
                .FirstOrDefaultAsync(s => s.Translations.Any(t => t.LanguageCode == Tr && t.Slug == slug));
            if (service is null)
            {
                continue;
            }

            if (service.GalleryCategoryId is null)
            {
                service.GalleryCategory = await GalleryFor(db, entry, photos);
            }

            if (service.ProductCategories.Count == 0)
            {
                var categories = await db.ProductCategories
                    .Where(c => c.Translations.Any(t => t.LanguageCode == Tr && entry.ProductCategories.Contains(t.Slug)))
                    .ToListAsync();
                foreach (var category in categories)
                {
                    service.ProductCategories.Add(category);
                }
            }

            catalog.TryGetValue(slug, out var original);
            foreach (var translation in service.Translations)
            {
                var (text, first) = translation.LanguageCode == En ? (entry.En, original?.En) : (entry.Tr, original?.Tr);
                if (Untouched(translation.Content, first?.Description))
                {
                    translation.Content = PhotoToken().Replace(text.Content, m => photos.TryGetValue(m.Groups[1].Value, out var path) ? Medium(path) : string.Empty);
                }

                if (Untouched(translation.Summary, first?.ShortDescription))
                {
                    translation.Summary = text.Summary;
                }

                if (Untouched(translation.MetaDescription, first?.MetaDescription))
                {
                    translation.MetaDescription = text.MetaDescription;
                }

                if (string.IsNullOrWhiteSpace(translation.Specifications))
                {
                    translation.Specifications = text.Specifications;
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private static bool Untouched(string? current, string? first) =>
        string.IsNullOrWhiteSpace(current) || (first is not null && current.Trim() == first.Trim());

    // The text column shows the 960px copy the image pipeline writes next to each upload.
    private static string Medium(string path) => Path.ChangeExtension(path, null) + "-960" + Path.GetExtension(path);

    // "folder/NNN" -> the stored path of the imported photo.
    private static async Task<Dictionary<string, string>> LoadPhotosAsync(ApplicationDbContext db)
    {
        var items = await db.GalleryItems
            .Select(i => new
            {
                Folder = i.Category.Translations.Where(t => t.LanguageCode == Tr).Select(t => t.Slug).FirstOrDefault(),
                i.SortOrder,
                i.ImagePath,
            })
            .ToListAsync();

        var photos = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in items.Where(i => i.Folder is not null))
        {
            photos.TryAdd($"{item.Folder}/{item.SortOrder:000}", item.ImagePath);
        }

        return photos;
    }

    private static async Task<GalleryCategory> GalleryFor(ApplicationDbContext db, SeedService entry, IReadOnlyDictionary<string, string> photos)
    {
        var trSlug = SlugHelper.Generate(entry.Gallery.Tr);
        var category = await db.GalleryCategories.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Translations.Any(t => t.LanguageCode == Tr && t.Slug == trSlug));
        if (category is null)
        {
            category = new GalleryCategory
            {
                SortOrder = (await db.GalleryCategories.Select(c => (int?)c.SortOrder).MaxAsync() ?? 0) + 1,
                Translations =
                {
                    new GalleryCategoryTranslation { LanguageCode = Tr, Name = entry.Gallery.Tr, Slug = trSlug },
                    new GalleryCategoryTranslation { LanguageCode = En, Name = entry.Gallery.En, Slug = SlugHelper.Generate(entry.Gallery.En) },
                },
            };
            db.GalleryCategories.Add(category);
        }

        if (category.Items.Count == 0)
        {
            // The same stored picture is shown under the service as well; no file is copied.
            var order = 0;
            foreach (var key in entry.Photos)
            {
                if (!photos.TryGetValue(key, out var path))
                {
                    continue;
                }

                category.Items.Add(new GalleryItem
                {
                    ImagePath = path,
                    SortOrder = ++order,
                    Translations =
                    {
                        new GalleryItemTranslation { LanguageCode = Tr, Title = entry.Gallery.Tr },
                        new GalleryItemTranslation { LanguageCode = En, Title = entry.Gallery.En },
                    },
                });
            }
        }

        return category;
    }

    [GeneratedRegex(@"\{\{photo:([a-z0-9\-]+/\d{3})\}\}")]
    private static partial Regex PhotoToken();
}
