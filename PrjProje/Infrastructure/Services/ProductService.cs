using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models;
using PrjProje.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace PrjProje.Infrastructure.Services;

public sealed class ProductService : IProductService
{
    private readonly ApplicationDbContext _db;
    private readonly IContentCache _cache;

    public ProductService(ApplicationDbContext db, IContentCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<IReadOnlyList<ProductCategoryDto>> GetCategoriesAsync(string languageCode, CancellationToken cancellationToken = default) =>
        _cache.GetOrCreateAsync($"product-categories:{languageCode}", async () =>
        {
            var categories = await _db.ProductCategories
                .AsNoTracking()
                .Where(c => c.IsActive && c.Translations.Any(t => t.LanguageCode == languageCode))
                .OrderBy(c => c.SortOrder)
                .Select(c => new
                {
                    c.Id,
                    c.Image,
                    c.IconClass,
                    c.IsRestricted,
                    ProductCount = c.Products.Count(p => p.IsActive && p.Translations.Any(t => t.LanguageCode == languageCode)),
                    Translations = c.Translations
                        .Select(t => new { t.LanguageCode, t.Name, t.Slug, t.Description, t.MetaTitle, t.MetaDescription })
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            return (IReadOnlyList<ProductCategoryDto>)categories
                .Select(c =>
                {
                    var current = c.Translations.First(t => t.LanguageCode == languageCode);
                    return new ProductCategoryDto(
                        c.Id,
                        current.Name,
                        current.Slug,
                        current.Description,
                        c.Image,
                        c.IconClass,
                        c.IsRestricted,
                        c.ProductCount,
                        current.MetaTitle,
                        current.MetaDescription,
                        c.Translations.ToDictionary(t => t.LanguageCode, t => t.Slug));
                })
                .ToList();
        });

    public async Task<ProductCategoryDto?> GetCategoryBySlugAsync(string languageCode, string slug, CancellationToken cancellationToken = default)
    {
        var categories = await GetCategoriesAsync(languageCode, cancellationToken);
        return categories.FirstOrDefault(c => c.Slug == slug);
    }

    public async Task<PagedResult<ProductListItemDto>> GetProductsAsync(string languageCode, ProductFilter filter, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 48);

        var search = ProductSearch.ParseQuery(filter.Search);
        if (search is not null)
        {
            var ranked = (await GetSearchIndexAsync(languageCode, cancellationToken))
                .Where(e => string.IsNullOrWhiteSpace(filter.CategorySlug) || e.Item.CategorySlug == filter.CategorySlug)
                .Select(e => (e.Item, e.SortOrder, Score: ProductSearch.Score(e.Document, search)))
                .Where(e => e.Score > 0)
                .OrderByDescending(e => e.Score)
                .ThenBy(e => e.SortOrder)
                .ThenBy(e => e.Item.Name, StringComparer.CurrentCulture)
                .ThenBy(e => e.Item.Id)
                .Select(e => e.Item)
                .ToList();

            return new PagedResult<ProductListItemDto>(ranked.Skip((page - 1) * pageSize).Take(pageSize).ToList(), ranked.Count, page, pageSize);
        }

        var query = _db.ProductTranslations
            .AsNoTracking()
            .Where(t => t.LanguageCode == languageCode && t.Product.IsActive && t.Product.Category.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.CategorySlug))
        {
            query = query.Where(t => t.Product.Category.Translations.Any(ct => ct.LanguageCode == languageCode && ct.Slug == filter.CategorySlug));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await ProjectListItems(query.OrderBy(t => t.Product.SortOrder).ThenBy(t => t.Name), languageCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductListItemDto>(items, totalCount, page, pageSize);
    }

    public Task<IReadOnlyList<ProductListItemDto>> GetFeaturedAsync(string languageCode, int count, CancellationToken cancellationToken = default) =>
        _cache.GetOrCreateAsync($"featured-products:{languageCode}:{count}", async () =>
        {
            var query = _db.ProductTranslations
                .AsNoTracking()
                .Where(t => t.LanguageCode == languageCode && t.Product.IsActive && t.Product.IsFeatured && t.Product.Category.IsActive)
                .OrderBy(t => t.Product.SortOrder);

            return (IReadOnlyList<ProductListItemDto>)await ProjectListItems(query, languageCode).Take(count).ToListAsync(cancellationToken);
        });

    public async Task<ProductDetailDto?> GetDetailAsync(string languageCode, string slug, CancellationToken cancellationToken = default)
    {
        var product = await _db.ProductTranslations
            .AsNoTracking()
            .Where(t => t.LanguageCode == languageCode && t.Slug == slug && t.Product.IsActive && t.Product.Category.IsActive)
            .Select(t => new
            {
                Translation = t,
                t.Product.Image,
                t.Product.CategoryId,
                t.Product.CreatedDate,
                t.Product.UpdatedDate,
                t.Product.Category.IsRestricted,
                t.Product.Category.IconClass,
                Category = t.Product.Category.Translations
                    .Where(ct => ct.LanguageCode == languageCode)
                    .Select(ct => new { ct.Name, ct.Slug })
                    .FirstOrDefault(),
                Images = t.Product.Images.OrderBy(i => i.SortOrder).Select(i => i.ImagePath).ToList(),
                Variants = t.Product.Variants.OrderBy(v => v.SortOrder).Select(v => v.Code).ToList(),
                Specifications = t.Product.Specifications
                    .Where(s => s.LanguageCode == languageCode)
                    .OrderBy(s => s.SortOrder)
                    .Select(s => new SpecificationDto(s.Name, s.Value))
                    .ToList(),
                Alternates = t.Product.Translations.Select(x => new { x.LanguageCode, x.Slug }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            return null;
        }

        var translation = product.Translation;
        var relatedQuery = _db.ProductTranslations
            .AsNoTracking()
            .Where(t => t.LanguageCode == languageCode && t.ProductId != translation.ProductId &&
                        t.Product.IsActive && t.Product.CategoryId == product.CategoryId)
            .OrderBy(t => t.Product.SortOrder);
        var related = await ProjectListItems(relatedQuery, languageCode).Take(4).ToListAsync(cancellationToken);

        return new ProductDetailDto
        {
            Id = translation.ProductId,
            Name = translation.Name,
            Slug = translation.Slug,
            ShortDescription = translation.ShortDescription,
            Description = translation.Description,
            Variants = product.Variants,
            ApplicationAreas = SplitLines(translation.ApplicationAreas),
            MetaDescription = translation.MetaDescription,
            Image = product.Image,
            Images = product.Images,
            Specifications = product.IsRestricted ? Array.Empty<SpecificationDto>() : product.Specifications,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            CategorySlug = product.Category?.Slug ?? string.Empty,
            CategoryIcon = product.IconClass,
            IsRestricted = product.IsRestricted,
            LastModified = product.UpdatedDate ?? product.CreatedDate,
            AlternateSlugs = product.Alternates.ToDictionary(a => a.LanguageCode, a => a.Slug),
            RelatedProducts = related
        };
    }

    public Task<IReadOnlyList<ProductOptionDto>> GetProductOptionsAsync(string languageCode, CancellationToken cancellationToken = default) =>
        _cache.GetOrCreateAsync($"product-options:{languageCode}", async () =>
            (IReadOnlyList<ProductOptionDto>)await _db.ProductTranslations
                .AsNoTracking()
                .Where(t => t.LanguageCode == languageCode && t.Product.IsActive)
                .OrderBy(t => t.Name)
                .Select(t => new ProductOptionDto(t.ProductId, t.Name))
                .ToListAsync(cancellationToken));

    public Task<string?> GetProductNameAsync(int productId, string languageCode, CancellationToken cancellationToken = default) =>
        _db.ProductTranslations
            .AsNoTracking()
            .Where(t => t.ProductId == productId && t.Product.IsActive)
            .OrderBy(t => t.LanguageCode == languageCode ? 0 : 1)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(cancellationToken);

    private sealed record SearchEntry(ProductListItemDto Item, int SortOrder, ProductSearch.Document Document);

    private Task<IReadOnlyList<SearchEntry>> GetSearchIndexAsync(string languageCode, CancellationToken cancellationToken) =>
        _cache.GetOrCreateAsync($"product-search:{languageCode}", async () =>
        {
            var rows = await _db.ProductTranslations
                .AsNoTracking()
                .Where(t => t.LanguageCode == languageCode && t.Product.IsActive && t.Product.Category.IsActive)
                .Select(t => new
                {
                    t.ProductId,
                    t.Name,
                    t.Slug,
                    t.ShortDescription,
                    t.ApplicationAreas,
                    t.Product.Image,
                    Variants = t.Product.Variants.OrderBy(v => v.SortOrder).Select(v => v.Code).ToList(),
                    t.Product.SortOrder,
                    CategoryName = t.Product.Category.Translations.Where(ct => ct.LanguageCode == languageCode).Select(ct => ct.Name).FirstOrDefault() ?? string.Empty,
                    CategorySlug = t.Product.Category.Translations.Where(ct => ct.LanguageCode == languageCode).Select(ct => ct.Slug).FirstOrDefault() ?? string.Empty,
                    t.Product.Category.IconClass
                })
                .ToListAsync(cancellationToken);

            return (IReadOnlyList<SearchEntry>)rows
                .Select(r => new SearchEntry(
                    new ProductListItemDto(r.ProductId, r.Name, r.Slug, r.ShortDescription, r.Image, r.CategoryName, r.CategorySlug, r.IconClass),
                    r.SortOrder,
                    new ProductSearch.Document(r.Name, r.CategoryName, $"{r.ShortDescription} {r.ApplicationAreas}", string.Join(", ", r.Variants))))
                .ToList();
        });

    private static IQueryable<ProductListItemDto> ProjectListItems(IQueryable<Domain.Entities.ProductTranslation> query, string languageCode) =>
        query.Select(t => new ProductListItemDto(
            t.ProductId,
            t.Name,
            t.Slug,
            t.ShortDescription,
            t.Product.Image,
            t.Product.Category.Translations.Where(ct => ct.LanguageCode == languageCode).Select(ct => ct.Name).FirstOrDefault() ?? string.Empty,
            t.Product.Category.Translations.Where(ct => ct.LanguageCode == languageCode).Select(ct => ct.Slug).FirstOrDefault() ?? string.Empty,
            t.Product.Category.IconClass));

    private static IReadOnlyList<string> SplitLines(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
