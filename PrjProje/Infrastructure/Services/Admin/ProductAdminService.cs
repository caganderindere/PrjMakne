using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models.Admin;
using PrjProje.Domain.Entities;
using PrjProje.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace PrjProje.Infrastructure.Services.Admin;

public sealed class ProductAdminService : IProductAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IHtmlSanitizerService _sanitizer;
    private readonly IActivityLogger _activityLogger;
    private readonly IContentCache _cache;
    private readonly IProductCategoryAdminService _categoryService;

    public ProductAdminService(
        ApplicationDbContext db,
        IFileStorageService storage,
        IHtmlSanitizerService sanitizer,
        IActivityLogger activityLogger,
        IContentCache cache,
        IProductCategoryAdminService categoryService)
    {
        _db = db;
        _storage = storage;
        _sanitizer = sanitizer;
        _activityLogger = activityLogger;
        _cache = cache;
        _categoryService = categoryService;
    }

    public async Task<PagedResult<ProductAdminListItem>> GetListAsync(AdminListFilter filter, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, filter.Page);
        var query = _db.Products.AsNoTracking();

        if (filter.CategoryId is > 0)
        {
            query = query.Where(p => p.CategoryId == filter.CategoryId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(p =>
                p.Translations.Any(t => t.Name.Contains(term)) ||
                p.Variants.Any(v => v.Code.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.SortOrder)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(p => new ProductAdminListItem(
                p.Id,
                p.Translations.Where(t => t.LanguageCode == Languages.Default).Select(t => t.Name).FirstOrDefault()
                    ?? p.Translations.Select(t => t.Name).FirstOrDefault() ?? "-",
                p.Category.Translations.Where(t => t.LanguageCode == Languages.Default).Select(t => t.Name).FirstOrDefault() ?? "-",
                p.Image,
                p.SortOrder,
                p.IsActive,
                p.IsFeatured,
                p.Translations.Select(t => t.LanguageCode).ToList(),
                p.CreatedDate))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductAdminListItem>(items, total, page, filter.PageSize);
    }

    public async Task<ProductEditModel> CreateNewAsync(CancellationToken cancellationToken = default)
    {
        var model = new ProductEditModel
        {
            Translations = Languages.All.Select(code => new ProductTranslationInput { LanguageCode = code }).ToList()
        };
        await PopulateOptionsAsync(model, cancellationToken);
        return model;
    }

    public async Task<ProductEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Translations)
            .Include(p => p.Images)
            .Include(p => p.Specifications)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
        {
            return null;
        }

        var model = new ProductEditModel
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            Variants = product.Variants.OrderBy(v => v.SortOrder).Select(v => (string?)v.Code).ToList(),
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured,
            SortOrder = product.SortOrder,
            Image = product.Image,
            ExistingImages = product.Images
                .OrderBy(i => i.SortOrder)
                .Select(i => new ProductImageItem { Id = i.Id, ImagePath = i.ImagePath, SortOrder = i.SortOrder })
                .ToList(),
            Translations = Languages.All.Select(code =>
            {
                var t = product.Translations.FirstOrDefault(x => x.LanguageCode == code);
                var specs = product.Specifications
                    .Where(s => s.LanguageCode == code)
                    .OrderBy(s => s.SortOrder)
                    .Select(s => $"{s.Name}: {s.Value}");
                return new ProductTranslationInput
                {
                    LanguageCode = code,
                    Name = t?.Name,
                    Slug = t?.Slug,
                    ShortDescription = t?.ShortDescription,
                    Description = t?.Description,
                    ApplicationAreas = t?.ApplicationAreas,
                    MetaDescription = t?.MetaDescription,
                    SpecificationsText = string.Join(Environment.NewLine, specs)
                };
            }).ToList()
        };

        await PopulateOptionsAsync(model, cancellationToken);
        return model;
    }

    public async Task PopulateOptionsAsync(ProductEditModel model, CancellationToken cancellationToken = default)
    {
        model.CategoryOptions = await _categoryService.GetOptionsAsync(cancellationToken);
        model.RestrictedCategoryIds = await _db.ProductCategories
            .AsNoTracking()
            .Where(c => c.IsRestricted)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        foreach (var code in Languages.All.Where(code => model.Translations.All(t => t.LanguageCode != code)))
        {
            model.Translations.Add(new ProductTranslationInput { LanguageCode = code });
        }
    }

    public async Task<ServiceResult<int>> SaveAsync(ProductEditModel model, CancellationToken cancellationToken = default)
    {
        if (!await _db.ProductCategories.AnyAsync(c => c.Id == model.CategoryId, cancellationToken))
        {
            return ServiceResult<int>.Failure("Seçilen kategori bulunamadı.");
        }

        Product? product;
        var isNew = model.Id == 0;
        if (isNew)
        {
            product = new Product();
            _db.Products.Add(product);
        }
        else
        {
            product = await _db.Products
                .AsSplitQuery()
                .Include(p => p.Translations)
                .Include(p => p.Images)
                .Include(p => p.Specifications)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == model.Id, cancellationToken);

            if (product is null)
            {
                return ServiceResult<int>.Failure("Ürün bulunamadı.");
            }
        }

        var newGalleryPaths = new List<string>();
        foreach (var file in model.GalleryFiles?.Where(f => f.Length > 0) ?? Enumerable.Empty<Microsoft.AspNetCore.Http.IFormFile>())
        {
            var upload = await _storage.SaveImageAsync(file, ImageFolder.Products, cancellationToken);
            if (!upload.Succeeded)
            {
                newGalleryPaths.ForEach(_storage.DeletePublicFile);
                return ServiceResult<int>.Failure(upload.Errors.ToArray());
            }

            newGalleryPaths.Add(upload.Value!.RelativePath);
        }

        var image = await AdminHelpers.ReplaceImageAsync(_storage, product.Image, model.ImageFile, model.RemoveImage, ImageFolder.Products, cancellationToken);
        if (!image.Succeeded)
        {
            newGalleryPaths.ForEach(_storage.DeletePublicFile);
            return ServiceResult<int>.Failure(image.Errors.ToArray());
        }

        product.Image = image.Value;
        product.CategoryId = model.CategoryId;
        product.IsActive = model.IsActive;
        product.IsFeatured = model.IsFeatured;
        product.SortOrder = model.SortOrder;

        SyncImages(product, model.ExistingImages, newGalleryPaths);
        SyncVariants(product, model.Variants);
        await SyncTranslationsAsync(product, model.Translations, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();

        var name = product.Translations.FirstOrDefault(t => t.LanguageCode == Languages.Default)?.Name;
        await _activityLogger.LogAsync(isNew ? "Created" : "Updated", "Product", product.Id, name, cancellationToken);
        return ServiceResult<int>.Success(product.Id);
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
        {
            return ServiceResult.Failure("Ürün bulunamadı.");
        }

        var files = product.Images.Select(i => i.ImagePath).Append(product.Image).ToList();
        var name = product.Translations.FirstOrDefault(t => t.LanguageCode == Languages.Default)?.Name;

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(cancellationToken);
        files.ForEach(_storage.DeletePublicFile);
        _cache.Invalidate();

        await _activityLogger.LogAsync("Deleted", "Product", id, name, cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ToggleActiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return ServiceResult.Failure("Ürün bulunamadı.");
        }

        product.IsActive = !product.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
        await _activityLogger.LogAsync(product.IsActive ? "Activated" : "Deactivated", "Product", id, null, cancellationToken);
        return ServiceResult.Success();
    }

    /// <summary>Variants carry nothing but their code and order, so the list is simply replaced.</summary>
    private void SyncVariants(Product product, IEnumerable<string?> codes)
    {
        _db.ProductVariants.RemoveRange(product.Variants);
        product.Variants = ProductSearch.DistinctCodes(codes)
            .Select((code, index) => new ProductVariant { Code = code, SortOrder = index })
            .ToList();
    }

    private void SyncImages(Product product, IEnumerable<ProductImageItem> existingInputs, IReadOnlyList<string> newPaths)
    {
        foreach (var input in existingInputs)
        {
            var entity = product.Images.FirstOrDefault(i => i.Id == input.Id);
            if (entity is null)
            {
                continue;
            }

            if (input.Delete)
            {
                _storage.DeletePublicFile(entity.ImagePath);
                _db.ProductImages.Remove(entity);
            }
            else
            {
                entity.SortOrder = input.SortOrder;
            }
        }

        var nextOrder = product.Images.Count == 0 ? 0 : product.Images.Max(i => i.SortOrder) + 1;
        foreach (var path in newPaths)
        {
            product.Images.Add(new ProductImage { ImagePath = path, SortOrder = nextOrder++ });
        }
    }

    private async Task SyncTranslationsAsync(Product product, IReadOnlyList<ProductTranslationInput> inputs, CancellationToken cancellationToken)
    {
        foreach (var code in Languages.All)
        {
            var input = inputs.FirstOrDefault(t => t.LanguageCode == code);
            var translation = product.Translations.FirstOrDefault(t => t.LanguageCode == code);
            var existingSpecs = product.Specifications.Where(s => s.LanguageCode == code).ToList();

            if (input is null || string.IsNullOrWhiteSpace(input.Name))
            {
                if (translation is not null)
                {
                    _db.ProductTranslations.Remove(translation);
                }

                _db.ProductSpecifications.RemoveRange(existingSpecs);
                continue;
            }

            if (translation is null)
            {
                translation = new ProductTranslation { LanguageCode = code };
                product.Translations.Add(translation);
            }

            var productId = product.Id;
            translation.Name = input.Name.Trim();
            translation.Slug = await AdminHelpers.UniqueSlugAsync(
                _db.ProductTranslations.Where(t => t.LanguageCode == code && t.ProductId != productId).Select(t => t.Slug),
                input.Slug,
                translation.Name,
                cancellationToken);
            translation.ShortDescription = AdminHelpers.Clean(input.ShortDescription);
            translation.Description = AdminHelpers.Clean(_sanitizer.Sanitize(input.Description));
            translation.ApplicationAreas = AdminHelpers.Clean(input.ApplicationAreas);
            // The SEO title is always generated from the name and codes; clear any value saved before that rule.
            translation.MetaTitle = null;
            translation.MetaDescription = AdminHelpers.Clean(input.MetaDescription);

            _db.ProductSpecifications.RemoveRange(existingSpecs);
            var order = 0;
            foreach (var (name, value) in AdminHelpers.ParseSpecifications(input.SpecificationsText))
            {
                product.Specifications.Add(new ProductSpecification
                {
                    LanguageCode = code,
                    Name = name,
                    Value = value,
                    SortOrder = order++
                });
            }
        }
    }
}

public sealed class ProductCategoryAdminService : IProductCategoryAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IActivityLogger _activityLogger;
    private readonly IContentCache _cache;

    public ProductCategoryAdminService(ApplicationDbContext db, IFileStorageService storage, IActivityLogger activityLogger, IContentCache cache)
    {
        _db = db;
        _storage = storage;
        _activityLogger = activityLogger;
        _cache = cache;
    }

    public async Task<IReadOnlyList<TranslatedListItem>> GetListAsync(CancellationToken cancellationToken = default) =>
        await _db.ProductCategories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => new TranslatedListItem(
                c.Id,
                c.Translations.Where(t => t.LanguageCode == Languages.Default).Select(t => t.Name).FirstOrDefault() ?? "-",
                c.Image,
                c.SortOrder,
                c.IsActive,
                c.Translations.Select(t => t.LanguageCode).ToList(),
                c.UpdatedDate ?? c.CreatedDate))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OptionItem>> GetOptionsAsync(CancellationToken cancellationToken = default) =>
        await _db.ProductCategories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => new OptionItem(c.Id, c.Translations.Where(t => t.LanguageCode == Languages.Default).Select(t => t.Name).FirstOrDefault() ?? "-"))
            .ToListAsync(cancellationToken);

    public ProductCategoryEditModel CreateNew() => new()
    {
        Translations = Languages.All.Select(code => new CategoryTranslationInput { LanguageCode = code }).ToList()
    };

    public async Task<ProductCategoryEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _db.ProductCategories.AsNoTracking().Include(c => c.Translations).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
        {
            return null;
        }

        return new ProductCategoryEditModel
        {
            Id = category.Id,
            IconClass = category.IconClass,
            Image = category.Image,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive,
            IsRestricted = category.IsRestricted,
            Translations = Languages.All.Select(code =>
            {
                var t = category.Translations.FirstOrDefault(x => x.LanguageCode == code);
                return new CategoryTranslationInput
                {
                    LanguageCode = code,
                    Name = t?.Name,
                    Slug = t?.Slug,
                    Description = t?.Description,
                    MetaTitle = t?.MetaTitle,
                    MetaDescription = t?.MetaDescription
                };
            }).ToList()
        };
    }

    public async Task<ServiceResult<int>> SaveAsync(ProductCategoryEditModel model, CancellationToken cancellationToken = default)
    {
        ProductCategory? category;
        var isNew = model.Id == 0;
        if (isNew)
        {
            category = new ProductCategory();
            _db.ProductCategories.Add(category);
        }
        else
        {
            category = await _db.ProductCategories.Include(c => c.Translations).FirstOrDefaultAsync(c => c.Id == model.Id, cancellationToken);
            if (category is null)
            {
                return ServiceResult<int>.Failure("Kategori bulunamadı.");
            }
        }

        var image = await AdminHelpers.ReplaceImageAsync(_storage, category.Image, model.ImageFile, model.RemoveImage, ImageFolder.Categories, cancellationToken);
        if (!image.Succeeded)
        {
            return ServiceResult<int>.Failure(image.Errors.ToArray());
        }

        category.Image = image.Value;
        category.IconClass = string.IsNullOrWhiteSpace(model.IconClass) ? "fa-solid fa-gears" : model.IconClass.Trim();
        category.SortOrder = model.SortOrder;
        category.IsActive = model.IsActive;
        category.IsRestricted = model.IsRestricted;

        foreach (var code in Languages.All)
        {
            var input = model.Translations.FirstOrDefault(t => t.LanguageCode == code);
            var translation = category.Translations.FirstOrDefault(t => t.LanguageCode == code);
            if (input is null || string.IsNullOrWhiteSpace(input.Name))
            {
                if (translation is not null)
                {
                    _db.ProductCategoryTranslations.Remove(translation);
                }

                continue;
            }

            if (translation is null)
            {
                translation = new ProductCategoryTranslation { LanguageCode = code };
                category.Translations.Add(translation);
            }

            var categoryId = category.Id;
            translation.Name = input.Name.Trim();
            translation.Slug = await AdminHelpers.UniqueSlugAsync(
                _db.ProductCategoryTranslations.Where(t => t.LanguageCode == code && t.ProductCategoryId != categoryId).Select(t => t.Slug),
                input.Slug,
                translation.Name,
                cancellationToken);
            translation.Description = AdminHelpers.Clean(input.Description);
            translation.MetaTitle = AdminHelpers.Clean(input.MetaTitle);
            translation.MetaDescription = AdminHelpers.Clean(input.MetaDescription);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
        await _activityLogger.LogAsync(isNew ? "Created" : "Updated", "ProductCategory", category.Id,
            category.Translations.FirstOrDefault(t => t.LanguageCode == Languages.Default)?.Name, cancellationToken);
        return ServiceResult<int>.Success(category.Id);
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _db.ProductCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
        {
            return ServiceResult.Failure("Kategori bulunamadı.");
        }

        if (await _db.Products.AnyAsync(p => p.CategoryId == id, cancellationToken))
        {
            return ServiceResult.Failure("Bu kategoriye bağlı ürünler var. Önce ürünleri taşıyın veya silin.");
        }

        _db.ProductCategories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
        _storage.DeletePublicFile(category.Image);
        _cache.Invalidate();
        await _activityLogger.LogAsync("Deleted", "ProductCategory", id, null, cancellationToken);
        return ServiceResult.Success();
    }
}
