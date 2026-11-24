using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Domain.Entities;

namespace PrjProje.Infrastructure.Data.Seed;

public sealed record SeedMedia(
    IReadOnlyDictionary<string, string> CategoryCovers,
    IReadOnlyDictionary<string, string> ServiceImages,
    IReadOnlyList<string> Gallery);

/// <summary>
/// Loads the company's own photos (content/images/gallery, listed in content/seed/media.json, prepared by
/// tools/import_photos.py). The gallery, the category covers and the service pictures are filled once, while
/// the gallery is still empty; a service only gets a picture when it has none, so panel edits are kept.
/// </summary>
public static class MediaSeeder
{
    private const string Tr = Languages.Turkish;
    private const string En = Languages.English;

    // Gallery folders that are not product categories.
    private static readonly Dictionary<string, (string Tr, string En)> ExtraGalleryNames = new()
    {
        ["tesis-kurulumlari"] = ("Tesis Kurulumları", "Plant Installations"),
    };

    public static async Task SeedAsync(ApplicationDbContext db, IFileStorageService storage, string contentRoot, ILogger logger)
    {
        var manifest = Path.Combine(contentRoot, "seed", "media.json");
        if (!File.Exists(manifest))
        {
            return;
        }

        var media = JsonSerializer.Deserialize<SeedMedia>(File.ReadAllText(manifest))
                    ?? throw new InvalidOperationException("media.json is empty.");
        var stored = new Dictionary<string, string>(StringComparer.Ordinal);

        async Task<string?> Store(string relative, ImageFolder folder)
        {
            if (stored.TryGetValue(relative, out var existing))
            {
                return existing;
            }

            var path = Path.Combine(contentRoot, "images", relative);
            if (!File.Exists(path))
            {
                logger.LogWarning("Seed photo {Path} is missing.", path);
                return null;
            }

            await using var stream = File.OpenRead(path);
            var result = await storage.SaveImageAsync(new FormFile(stream, 0, stream.Length, "file", Path.GetFileName(path)), folder);
            if (!result.Succeeded)
            {
                logger.LogWarning("Seed photo {Path} was rejected: {Errors}", path, string.Join("; ", result.Errors));
                return null;
            }

            return stored[relative] = result.Value!.RelativePath;
        }

        if (!await db.GalleryItems.AnyAsync())
        {
            logger.LogInformation("Importing the photo gallery; this runs once.");
            await SeedGalleryAsync(db, media, contentRoot, Store);

            foreach (var (slug, photo) in media.CategoryCovers)
            {
                var category = await db.ProductCategories
                    .FirstOrDefaultAsync(c => c.Translations.Any(t => t.LanguageCode == Tr && t.Slug == slug));
                if (category is not null && await Store(photo, ImageFolder.Gallery) is { } path)
                {
                    category.Image = path;
                }
            }

            await db.SaveChangesAsync();
        }

        foreach (var (slug, photo) in media.ServiceImages)
        {
            var service = await db.Services
                .FirstOrDefaultAsync(s => s.Image == null && s.Translations.Any(t => t.LanguageCode == Tr && t.Slug == slug));
            if (service is not null)
            {
                service.Image = await Store(photo, ImageFolder.Services);
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedGalleryAsync(ApplicationDbContext db, SeedMedia media, string contentRoot, Func<string, ImageFolder, Task<string?>> store)
    {
        // Installations first, then the product categories in catalogue order.
        var categoryOrder = await db.ProductCategories
            .Select(c => new
            {
                c.SortOrder,
                Tr = c.Translations.Where(t => t.LanguageCode == Tr).Select(t => new { t.Name, t.Slug }).FirstOrDefault(),
                En = c.Translations.Where(t => t.LanguageCode == En).Select(t => t.Name).FirstOrDefault(),
            })
            .ToListAsync();

        var sortOrder = await db.GalleryCategories.Select(c => (int?)c.SortOrder).MaxAsync() ?? 0;
        var folders = media.Gallery
            .OrderBy(f => ExtraGalleryNames.ContainsKey(f) ? -1 : categoryOrder.FirstOrDefault(c => c.Tr?.Slug == f)?.SortOrder ?? int.MaxValue);

        foreach (var folder in folders)
        {
            var names = ExtraGalleryNames.TryGetValue(folder, out var extra)
                ? extra
                : categoryOrder.FirstOrDefault(c => c.Tr?.Slug == folder) is { Tr: not null } match
                    ? (match.Tr.Name, match.En ?? match.Tr.Name)
                    : (folder, folder);

            var trSlug = SlugHelper.Generate(names.Item1);
            var category = await db.GalleryCategories.Include(c => c.Translations)
                .FirstOrDefaultAsync(c => c.Translations.Any(t => t.LanguageCode == Tr && t.Slug == trSlug));
            if (category is null)
            {
                category = new GalleryCategory
                {
                    SortOrder = ++sortOrder,
                    Translations =
                    {
                        new GalleryCategoryTranslation { LanguageCode = Tr, Name = names.Item1, Slug = trSlug },
                        new GalleryCategoryTranslation { LanguageCode = En, Name = names.Item2, Slug = SlugHelper.Generate(names.Item2) },
                    },
                };
                db.GalleryCategories.Add(category);
            }

            var files = Directory.GetFiles(Path.Combine(contentRoot, "images", "gallery", folder)).Order(StringComparer.Ordinal);
            var order = 0;
            foreach (var file in files)
            {
                if (await store($"gallery/{folder}/{Path.GetFileName(file)}", ImageFolder.Gallery) is not { } path)
                {
                    continue;
                }

                category.Items.Add(new GalleryItem
                {
                    ImagePath = path,
                    SortOrder = ++order,
                    Translations =
                    {
                        new GalleryItemTranslation { LanguageCode = Tr, Title = names.Item1 },
                        new GalleryItemTranslation { LanguageCode = En, Title = names.Item2 },
                    },
                });
            }

            await db.SaveChangesAsync();
        }
    }
}
