using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models;
using PrjProje.Domain.Entities;
using PrjProje.Domain.Enums;
using PrjProje.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace PrjProje.Infrastructure.Services;

public sealed class BlogService : IBlogService
{
    private readonly ApplicationDbContext _db;
    private readonly IContentCache _cache;

    public BlogService(ApplicationDbContext db, IContentCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<IReadOnlyList<BlogPostListItemDto>> GetLatestAsync(string languageCode, int count, CancellationToken cancellationToken = default) =>
        _cache.GetOrCreateAsync($"latest-posts:{languageCode}:{count}", async () =>
            (IReadOnlyList<BlogPostListItemDto>)await ProjectListItems(Published(languageCode).OrderByDescending(t => t.PublishedDate), languageCode)
                .Take(count)
                .ToListAsync(cancellationToken),
            TimeSpan.FromMinutes(5));

    public async Task<PagedResult<BlogPostListItemDto>> GetPostsAsync(string languageCode, BlogFilter filter, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 30);
        var query = Published(languageCode);

        if (!string.IsNullOrWhiteSpace(filter.CategorySlug))
        {
            query = query.Where(t => t.BlogPost.Category != null &&
                                     t.BlogPost.Category.Translations.Any(ct => ct.LanguageCode == languageCode && ct.Slug == filter.CategorySlug));
        }

        if (!string.IsNullOrWhiteSpace(filter.TagSlug))
        {
            query = query.Where(t => t.Tags.Any(tag => tag.Slug == filter.TagSlug));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(t => t.Title.Contains(term) || (t.Summary != null && t.Summary.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await ProjectListItems(query.OrderByDescending(t => t.PublishedDate), languageCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<BlogPostListItemDto>(items, total, page, pageSize);
    }

    public async Task<BlogPostDetailDto?> GetDetailAsync(string languageCode, string slug, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var post = await Published(languageCode)
            .Where(t => t.Slug == slug)
            .Select(t => new
            {
                Translation = t,
                t.BlogPost.CoverImage,
                t.BlogPost.AuthorName,
                t.BlogPost.CategoryId,
                t.BlogPost.UpdatedDate,
                Category = t.BlogPost.Category == null
                    ? null
                    : t.BlogPost.Category.Translations
                        .Where(ct => ct.LanguageCode == languageCode)
                        .Select(ct => new { ct.Name, ct.Slug })
                        .FirstOrDefault(),
                Tags = t.Tags.OrderBy(tag => tag.Name).Select(tag => new TagDto(tag.Name, tag.Slug)).ToList(),
                Alternates = t.BlogPost.Translations
                    .Where(x => x.Status == PublishStatus.Published && x.PublishedDate <= now)
                    .Select(x => new { x.LanguageCode, x.Slug })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (post is null)
        {
            return null;
        }

        var translation = post.Translation;
        var relatedQuery = Published(languageCode)
            .Where(t => t.BlogPostId != translation.BlogPostId && (post.CategoryId == null || t.BlogPost.CategoryId == post.CategoryId))
            .OrderByDescending(t => t.PublishedDate);
        var related = await ProjectListItems(relatedQuery, languageCode).Take(3).ToListAsync(cancellationToken);

        return new BlogPostDetailDto
        {
            Id = translation.BlogPostId,
            Title = translation.Title,
            Slug = translation.Slug,
            Summary = translation.Summary,
            Content = translation.Content,
            MetaTitle = translation.MetaTitle,
            MetaDescription = translation.MetaDescription,
            CoverImage = post.CoverImage,
            AuthorName = post.AuthorName,
            CategoryName = post.Category?.Name,
            CategorySlug = post.Category?.Slug,
            PublishedDate = translation.PublishedDate ?? now,
            LastModified = post.UpdatedDate ?? translation.PublishedDate ?? now,
            Tags = post.Tags,
            AlternateSlugs = post.Alternates.ToDictionary(a => a.LanguageCode, a => a.Slug),
            RelatedPosts = related
        };
    }

    public Task<IReadOnlyList<BlogCategoryDto>> GetCategoriesAsync(string languageCode, CancellationToken cancellationToken = default) =>
        _cache.GetOrCreateAsync($"blog-categories:{languageCode}", async () =>
        {
            var now = DateTime.UtcNow;
            return (IReadOnlyList<BlogCategoryDto>)await _db.BlogCategoryTranslations
                .AsNoTracking()
                .Where(t => t.LanguageCode == languageCode)
                .OrderBy(t => t.BlogCategory.SortOrder)
                .Select(t => new BlogCategoryDto(
                    t.BlogCategoryId,
                    t.Name,
                    t.Slug,
                    t.BlogCategory.Posts.Count(p => p.Translations.Any(pt =>
                        pt.LanguageCode == languageCode && pt.Status == PublishStatus.Published && pt.PublishedDate <= now))))
                .ToListAsync(cancellationToken);
        });

    public Task<TagDto?> GetTagAsync(string languageCode, string slug, CancellationToken cancellationToken = default) =>
        _db.Tags
            .AsNoTracking()
            .Where(t => t.LanguageCode == languageCode && t.Slug == slug)
            .Select(t => new TagDto(t.Name, t.Slug))
            .FirstOrDefaultAsync(cancellationToken);

    private IQueryable<BlogPostTranslation> Published(string languageCode)
    {
        var now = DateTime.UtcNow;
        return _db.BlogPostTranslations
            .AsNoTracking()
            .Where(t => t.LanguageCode == languageCode && t.Status == PublishStatus.Published && t.PublishedDate <= now);
    }

    private static IQueryable<BlogPostListItemDto> ProjectListItems(IQueryable<BlogPostTranslation> query, string languageCode) =>
        query.Select(t => new BlogPostListItemDto(
            t.BlogPostId,
            t.Title,
            t.Slug,
            t.Summary,
            t.BlogPost.CoverImage,
            t.BlogPost.Category == null ? null : t.BlogPost.Category.Translations.Where(ct => ct.LanguageCode == languageCode).Select(ct => ct.Name).FirstOrDefault(),
            t.BlogPost.Category == null ? null : t.BlogPost.Category.Translations.Where(ct => ct.LanguageCode == languageCode).Select(ct => ct.Slug).FirstOrDefault(),
            t.BlogPost.AuthorName,
            t.PublishedDate ?? t.BlogPost.CreatedDate));
}
