using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PrjProje.Application.Common;
using PrjProje.Domain.Entities;
using PrjProje.Domain.Enums;

namespace PrjProje.Infrastructure.Data.Seed;

public sealed record SeedBlogText(string Title, string Slug, string Summary, string MetaDescription, string Content);
public sealed record SeedBlogPost(string CoverProduct, DateTime PublishedDate, SeedBlogText Tr, SeedBlogText En);
public sealed record SeedBlogCategoryText(string Name, string Slug);
public sealed record SeedBlogCategory(SeedBlogCategoryText Tr, SeedBlogCategoryText En);
public sealed record SeedBlog(SeedBlogCategory Category, IReadOnlyList<SeedBlogPost> Posts);

/// <summary>
/// Loads the starter articles (content/seed/blog.json). Like the catalogue, posts are matched by Turkish slug,
/// so a post edited in the panel is never overwritten.
/// </summary>
public static class BlogSeeder
{
    private const string Tr = Languages.Turkish;
    private const string En = Languages.English;
    private const string Author = "PRJ Proje Makina";

    public static SeedBlog Load(string contentRoot) =>
        JsonSerializer.Deserialize<SeedBlog>(File.ReadAllText(Path.Combine(contentRoot, "seed", "blog.json")))
        ?? throw new InvalidOperationException("blog.json is empty.");

    public static async Task SeedAsync(ApplicationDbContext db, string contentRoot)
    {
        if (!File.Exists(Path.Combine(contentRoot, "seed", "blog.json")))
        {
            return;
        }

        var seed = Load(contentRoot);
        var catalog = SeedContent.Load(contentRoot).Catalog;

        var category = await db.BlogCategories
            .FirstOrDefaultAsync(c => c.Translations.Any(t => t.LanguageCode == Tr && t.Slug == seed.Category.Tr.Slug));
        if (category is null)
        {
            category = new BlogCategory
            {
                Translations =
                {
                    new BlogCategoryTranslation { LanguageCode = Tr, Name = seed.Category.Tr.Name, Slug = seed.Category.Tr.Slug },
                    new BlogCategoryTranslation { LanguageCode = En, Name = seed.Category.En.Name, Slug = seed.Category.En.Slug },
                },
            };
            db.BlogCategories.Add(category);
        }

        var existing = await db.BlogPostTranslations.Where(t => t.LanguageCode == Tr).Select(t => t.Slug).ToListAsync();
        foreach (var post in seed.Posts.Where(p => !existing.Contains(p.Tr.Slug)))
        {
            // The cover reuses the first photo of the product the article is about.
            var productSlug = catalog.Products.FirstOrDefault(p => p.LegacySlug == post.CoverProduct)?.Tr.Slug;
            var cover = productSlug is null ? null : await db.ProductImages
                .Where(i => i.Product.Translations.Any(t => t.LanguageCode == Tr && t.Slug == productSlug))
                .OrderBy(i => i.SortOrder)
                .Select(i => i.ImagePath)
                .FirstOrDefaultAsync();

            db.BlogPosts.Add(new BlogPost
            {
                Category = category,
                CoverImage = cover,
                AuthorName = Author,
                Translations =
                {
                    PostText(Tr, post.Tr, post.PublishedDate),
                    PostText(En, post.En, post.PublishedDate),
                },
            });
        }

        await db.SaveChangesAsync();
    }

    private static BlogPostTranslation PostText(string language, SeedBlogText text, DateTime published) => new()
    {
        LanguageCode = language,
        Title = text.Title,
        Slug = text.Slug,
        Summary = text.Summary,
        Content = text.Content,
        MetaDescription = text.MetaDescription,
        Status = PublishStatus.Published,
        PublishedDate = published,
    };
}
