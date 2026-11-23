using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models.Admin;
using PrjProje.Domain.Entities;
using PrjProje.Domain.Enums;
using PrjProje.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace PrjProje.Infrastructure.Services.Admin;

public sealed class BlogAdminService : IBlogAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IHtmlSanitizerService _sanitizer;
    private readonly IActivityLogger _activityLogger;
    private readonly IContentCache _cache;
    private readonly IBlogCategoryAdminService _categoryService;

    public BlogAdminService(
        ApplicationDbContext db,
        IFileStorageService storage,
        IHtmlSanitizerService sanitizer,
        IActivityLogger activityLogger,
        IContentCache cache,
        IBlogCategoryAdminService categoryService)
    {
        _db = db;
        _storage = storage;
        _sanitizer = sanitizer;
        _activityLogger = activityLogger;
        _cache = cache;
        _categoryService = categoryService;
    }

    public async Task<PagedResult<BlogAdminListItem>> GetListAsync(AdminListFilter filter, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, filter.Page);
        var query = _db.BlogPosts.AsNoTracking();

        if (filter.CategoryId is > 0)
        {
            query = query.Where(p => p.CategoryId == filter.CategoryId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(p => p.Translations.Any(t => t.Title.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(p => p.CreatedDate)
            .Skip((page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(p => new
            {
                p.Id,
                Title = p.Translations.OrderBy(t => t.LanguageCode == Languages.Default ? 0 : 1).Select(t => t.Title).FirstOrDefault() ?? "-",
                CategoryName = p.Category == null ? null : p.Category.Translations.Where(t => t.LanguageCode == Languages.Default).Select(t => t.Name).FirstOrDefault(),
                p.AuthorName,
                p.CoverImage,
                Statuses = p.Translations.Select(t => new { t.LanguageCode, t.Status }).ToList(),
                p.CreatedDate
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => new BlogAdminListItem(
            r.Id,
            r.Title,
            r.CategoryName,
            r.AuthorName,
            r.CoverImage,
            r.Statuses.ToDictionary(s => s.LanguageCode, s => s.Status),
            r.CreatedDate)).ToList();

        return new PagedResult<BlogAdminListItem>(items, total, page, filter.PageSize);
    }

    public async Task<BlogPostEditModel> CreateNewAsync(string authorName, CancellationToken cancellationToken = default)
    {
        var model = new BlogPostEditModel
        {
            AuthorName = authorName,
            Translations = Languages.All.Select(code => new BlogTranslationInput { LanguageCode = code }).ToList()
        };
        await PopulateOptionsAsync(model, cancellationToken);
        return model;
    }

    public async Task<BlogPostEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var post = await _db.BlogPosts
            .AsNoTracking()
            .Include(p => p.Translations).ThenInclude(t => t.Tags)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (post is null)
        {
            return null;
        }

        var model = new BlogPostEditModel
        {
            Id = post.Id,
            CategoryId = post.CategoryId,
            AuthorName = post.AuthorName,
            CoverImage = post.CoverImage,
            Translations = Languages.All.Select(code =>
            {
                var t = post.Translations.FirstOrDefault(x => x.LanguageCode == code);
                return new BlogTranslationInput
                {
                    LanguageCode = code,
                    Title = t?.Title,
                    Slug = t?.Slug,
                    Summary = t?.Summary,
                    Content = t?.Content,
                    MetaTitle = t?.MetaTitle,
                    MetaDescription = t?.MetaDescription,
                    Status = t?.Status ?? PublishStatus.Draft,
                    PublishedDate = AdminHelpers.UtcToLocal(t?.PublishedDate),
                    TagsText = t is null ? null : string.Join(", ", t.Tags.OrderBy(tag => tag.Name).Select(tag => tag.Name))
                };
            }).ToList()
        };

        await PopulateOptionsAsync(model, cancellationToken);
        return model;
    }

    public async Task PopulateOptionsAsync(BlogPostEditModel model, CancellationToken cancellationToken = default)
    {
        model.CategoryOptions = await _categoryService.GetOptionsAsync(cancellationToken);
        foreach (var code in Languages.All.Where(code => model.Translations.All(t => t.LanguageCode != code)))
        {
            model.Translations.Add(new BlogTranslationInput { LanguageCode = code });
        }
    }

    public async Task<ServiceResult<int>> SaveAsync(BlogPostEditModel model, CancellationToken cancellationToken = default)
    {
        if (model.CategoryId is > 0 && !await _db.BlogCategories.AnyAsync(c => c.Id == model.CategoryId, cancellationToken))
        {
            return ServiceResult<int>.Failure("Seçilen kategori bulunamadı.");
        }

        BlogPost? post;
        var isNew = model.Id == 0;
        if (isNew)
        {
            post = new BlogPost();
            _db.BlogPosts.Add(post);
        }
        else
        {
            post = await _db.BlogPosts
                .Include(p => p.Translations).ThenInclude(t => t.Tags)
                .FirstOrDefaultAsync(p => p.Id == model.Id, cancellationToken);
            if (post is null)
            {
                return ServiceResult<int>.Failure("Yazı bulunamadı.");
            }
        }

        var cover = await AdminHelpers.ReplaceImageAsync(_storage, post.CoverImage, model.CoverImageFile, model.RemoveCover, ImageFolder.Blog, cancellationToken);
        if (!cover.Succeeded)
        {
            return ServiceResult<int>.Failure(cover.Errors.ToArray());
        }

        post.CoverImage = cover.Value;
        post.CategoryId = model.CategoryId is > 0 ? model.CategoryId : null;
        post.AuthorName = model.AuthorName.Trim();

        foreach (var code in Languages.All)
        {
            var input = model.Translations.FirstOrDefault(t => t.LanguageCode == code);
            var translation = post.Translations.FirstOrDefault(t => t.LanguageCode == code);

            if (input is null || string.IsNullOrWhiteSpace(input.Title))
            {
                if (translation is not null)
                {
                    _db.BlogPostTranslations.Remove(translation);
                }

                continue;
            }

            if (translation is null)
            {
                translation = new BlogPostTranslation { LanguageCode = code };
                post.Translations.Add(translation);
            }

            var postId = post.Id;
            translation.Title = input.Title.Trim();
            translation.Slug = await AdminHelpers.UniqueSlugAsync(
                _db.BlogPostTranslations.Where(t => t.LanguageCode == code && t.BlogPostId != postId).Select(t => t.Slug),
                input.Slug,
                translation.Title,
                cancellationToken);
            translation.Summary = AdminHelpers.Clean(input.Summary);
            translation.Content = _sanitizer.Sanitize(input.Content);
            translation.MetaTitle = AdminHelpers.Clean(input.MetaTitle);
            translation.MetaDescription = AdminHelpers.Clean(input.MetaDescription);
            translation.Status = input.Status;
            translation.PublishedDate = AdminHelpers.LocalToUtc(input.PublishedDate)
                                        ?? (input.Status == PublishStatus.Published ? DateTime.UtcNow : null);

            var tags = await ResolveTagsAsync(code, input.TagsText, cancellationToken);
            translation.Tags.Clear();
            foreach (var tag in tags)
            {
                translation.Tags.Add(tag);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
        await _activityLogger.LogAsync(isNew ? "Created" : "Updated", "BlogPost", post.Id,
            post.Translations.Select(t => t.Title).FirstOrDefault(), cancellationToken);
        return ServiceResult<int>.Success(post.Id);
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var post = await _db.BlogPosts.Include(p => p.Translations).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (post is null)
        {
            return ServiceResult.Failure("Yazı bulunamadı.");
        }

        var title = post.Translations.Select(t => t.Title).FirstOrDefault();
        _db.BlogPosts.Remove(post);
        await _db.SaveChangesAsync(cancellationToken);
        _storage.DeletePublicFile(post.CoverImage);
        _cache.Invalidate();
        await _activityLogger.LogAsync("Deleted", "BlogPost", id, title, cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetStatusAsync(int id, PublishStatus status, CancellationToken cancellationToken = default)
    {
        var translations = await _db.BlogPostTranslations.Where(t => t.BlogPostId == id).ToListAsync(cancellationToken);
        if (translations.Count == 0)
        {
            return ServiceResult.Failure("Yazı bulunamadı.");
        }

        foreach (var translation in translations)
        {
            translation.Status = status;
            if (status == PublishStatus.Published && translation.PublishedDate is null)
            {
                translation.PublishedDate = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
        await _activityLogger.LogAsync(status == PublishStatus.Published ? "Published" : "Unpublished", "BlogPost", id, null, cancellationToken);
        return ServiceResult.Success();
    }

    private async Task<List<Tag>> ResolveTagsAsync(string languageCode, string? tagsText, CancellationToken cancellationToken)
    {
        var requested = (tagsText ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(name => name.Length > 80 ? name[..80] : name)
            .Select(name => (Name: name, Slug: SlugHelper.Generate(name, 100)))
            .Where(tag => tag.Slug.Length > 0)
            .DistinctBy(tag => tag.Slug)
            .ToList();

        if (requested.Count == 0)
        {
            return new List<Tag>();
        }

        var slugs = requested.Select(t => t.Slug).ToList();
        var existing = await _db.Tags
            .Where(t => t.LanguageCode == languageCode && slugs.Contains(t.Slug))
            .ToListAsync(cancellationToken);

        var pendingTags = _db.ChangeTracker.Entries<Tag>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .ToList();

        return requested.Select(tag =>
            existing.FirstOrDefault(t => t.Slug == tag.Slug)
            ?? pendingTags.FirstOrDefault(t => t.LanguageCode == languageCode && t.Slug == tag.Slug)
            ?? new Tag { LanguageCode = languageCode, Name = tag.Name, Slug = tag.Slug }).ToList();
    }
}

public sealed class BlogCategoryAdminService : IBlogCategoryAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IActivityLogger _activityLogger;
    private readonly IContentCache _cache;

    public BlogCategoryAdminService(ApplicationDbContext db, IActivityLogger activityLogger, IContentCache cache)
    {
        _db = db;
        _activityLogger = activityLogger;
        _cache = cache;
    }

    public async Task<IReadOnlyList<SimpleCategoryListItem>> GetListAsync(CancellationToken cancellationToken = default) =>
        await _db.BlogCategories.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => new SimpleCategoryListItem(
                c.Id,
                c.Translations.Where(t => t.LanguageCode == Languages.Default).Select(t => t.Name).FirstOrDefault() ?? "-",
                c.SortOrder,
                c.Posts.Count))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OptionItem>> GetOptionsAsync(CancellationToken cancellationToken = default) =>
        await _db.BlogCategories.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => new OptionItem(c.Id, c.Translations.Where(t => t.LanguageCode == Languages.Default).Select(t => t.Name).FirstOrDefault() ?? "-"))
            .ToListAsync(cancellationToken);

    public SimpleCategoryEditModel CreateNew() => new()
    {
        Translations = Languages.All.Select(code => new SimpleTranslationInput { LanguageCode = code }).ToList()
    };

    public async Task<SimpleCategoryEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _db.BlogCategories.AsNoTracking().Include(c => c.Translations).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return category is null ? null : new SimpleCategoryEditModel
        {
            Id = category.Id,
            SortOrder = category.SortOrder,
            Translations = Languages.All.Select(code =>
            {
                var t = category.Translations.FirstOrDefault(x => x.LanguageCode == code);
                return new SimpleTranslationInput { LanguageCode = code, Name = t?.Name, Slug = t?.Slug };
            }).ToList()
        };
    }

    public async Task<ServiceResult<int>> SaveAsync(SimpleCategoryEditModel model, CancellationToken cancellationToken = default)
    {
        BlogCategory? category;
        if (model.Id == 0)
        {
            category = new BlogCategory();
            _db.BlogCategories.Add(category);
        }
        else
        {
            category = await _db.BlogCategories.Include(c => c.Translations).FirstOrDefaultAsync(c => c.Id == model.Id, cancellationToken);
            if (category is null)
            {
                return ServiceResult<int>.Failure("Kategori bulunamadı.");
            }
        }

        category.SortOrder = model.SortOrder;
        foreach (var code in Languages.All)
        {
            var input = model.Translations.FirstOrDefault(t => t.LanguageCode == code);
            var translation = category.Translations.FirstOrDefault(t => t.LanguageCode == code);
            if (input is null || string.IsNullOrWhiteSpace(input.Name))
            {
                if (translation is not null)
                {
                    _db.BlogCategoryTranslations.Remove(translation);
                }

                continue;
            }

            if (translation is null)
            {
                translation = new BlogCategoryTranslation { LanguageCode = code };
                category.Translations.Add(translation);
            }

            var categoryId = category.Id;
            translation.Name = input.Name.Trim();
            translation.Slug = await AdminHelpers.UniqueSlugAsync(
                _db.BlogCategoryTranslations.Where(t => t.LanguageCode == code && t.BlogCategoryId != categoryId).Select(t => t.Slug),
                input.Slug, translation.Name, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
        await _activityLogger.LogAsync(model.Id == 0 ? "Created" : "Updated", "BlogCategory", category.Id, null, cancellationToken);
        return ServiceResult<int>.Success(category.Id);
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _db.BlogCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
        {
            return ServiceResult.Failure("Kategori bulunamadı.");
        }

        _db.BlogCategories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
        await _activityLogger.LogAsync("Deleted", "BlogCategory", id, null, cancellationToken);
        return ServiceResult.Success();
    }
}

public sealed class GalleryCategoryAdminService : IGalleryCategoryAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IActivityLogger _activityLogger;
    private readonly IContentCache _cache;

    public GalleryCategoryAdminService(ApplicationDbContext db, IActivityLogger activityLogger, IContentCache cache)
    {
        _db = db;
        _activityLogger = activityLogger;
        _cache = cache;
    }

    public async Task<IReadOnlyList<SimpleCategoryListItem>> GetListAsync(CancellationToken cancellationToken = default) =>
        await _db.GalleryCategories.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => new SimpleCategoryListItem(
                c.Id,
                c.Translations.Where(t => t.LanguageCode == Languages.Default).Select(t => t.Name).FirstOrDefault() ?? "-",
                c.SortOrder,
                c.Items.Count))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OptionItem>> GetOptionsAsync(CancellationToken cancellationToken = default) =>
        await _db.GalleryCategories.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => new OptionItem(c.Id, c.Translations.Where(t => t.LanguageCode == Languages.Default).Select(t => t.Name).FirstOrDefault() ?? "-"))
            .ToListAsync(cancellationToken);

    public SimpleCategoryEditModel CreateNew() => new()
    {
        Translations = Languages.All.Select(code => new SimpleTranslationInput { LanguageCode = code }).ToList()
    };

    public async Task<SimpleCategoryEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _db.GalleryCategories.AsNoTracking().Include(c => c.Translations).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return category is null ? null : new SimpleCategoryEditModel
        {
            Id = category.Id,
            SortOrder = category.SortOrder,
            Translations = Languages.All.Select(code =>
            {
                var t = category.Translations.FirstOrDefault(x => x.LanguageCode == code);
                return new SimpleTranslationInput { LanguageCode = code, Name = t?.Name, Slug = t?.Slug };
            }).ToList()
        };
    }

    public async Task<ServiceResult<int>> SaveAsync(SimpleCategoryEditModel model, CancellationToken cancellationToken = default)
    {
        GalleryCategory? category;
        if (model.Id == 0)
        {
            category = new GalleryCategory();
            _db.GalleryCategories.Add(category);
        }
        else
        {
            category = await _db.GalleryCategories.Include(c => c.Translations).FirstOrDefaultAsync(c => c.Id == model.Id, cancellationToken);
            if (category is null)
            {
                return ServiceResult<int>.Failure("Kategori bulunamadı.");
            }
        }

        category.SortOrder = model.SortOrder;
        foreach (var code in Languages.All)
        {
            var input = model.Translations.FirstOrDefault(t => t.LanguageCode == code);
            var translation = category.Translations.FirstOrDefault(t => t.LanguageCode == code);
            if (input is null || string.IsNullOrWhiteSpace(input.Name))
            {
                if (translation is not null)
                {
                    _db.GalleryCategoryTranslations.Remove(translation);
                }

                continue;
            }

            if (translation is null)
            {
                translation = new GalleryCategoryTranslation { LanguageCode = code };
                category.Translations.Add(translation);
            }

            var categoryId = category.Id;
            translation.Name = input.Name.Trim();
            translation.Slug = await AdminHelpers.UniqueSlugAsync(
                _db.GalleryCategoryTranslations.Where(t => t.LanguageCode == code && t.GalleryCategoryId != categoryId).Select(t => t.Slug),
                input.Slug, translation.Name, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
        await _activityLogger.LogAsync(model.Id == 0 ? "Created" : "Updated", "GalleryCategory", category.Id, null, cancellationToken);
        return ServiceResult<int>.Success(category.Id);
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _db.GalleryCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
        {
            return ServiceResult.Failure("Kategori bulunamadı.");
        }

        if (await _db.GalleryItems.AnyAsync(i => i.CategoryId == id, cancellationToken))
        {
            return ServiceResult.Failure("Bu kategoride fotoğraflar var. Önce fotoğrafları taşıyın veya silin.");
        }

        _db.GalleryCategories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
        await _activityLogger.LogAsync("Deleted", "GalleryCategory", id, null, cancellationToken);
        return ServiceResult.Success();
    }
}
