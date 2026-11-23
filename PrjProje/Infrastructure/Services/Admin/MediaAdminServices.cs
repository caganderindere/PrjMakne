using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models.Admin;
using PrjProje.Domain.Entities;
using PrjProje.Domain.Enums;
using PrjProje.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace PrjProje.Infrastructure.Services.Admin;

public sealed class GalleryAdminService : IGalleryAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IActivityLogger _activityLogger;
    private readonly IContentCache _cache;
    private readonly IGalleryCategoryAdminService _categoryService;

    public GalleryAdminService(
        ApplicationDbContext db,
        IFileStorageService storage,
        IActivityLogger activityLogger,
        IContentCache cache,
        IGalleryCategoryAdminService categoryService)
    {
        _db = db;
        _storage = storage;
        _activityLogger = activityLogger;
        _cache = cache;
        _categoryService = categoryService;
    }

    public async Task<IReadOnlyList<GalleryAdminItem>> GetListAsync(int? categoryId, CancellationToken cancellationToken = default)
    {
        var query = _db.GalleryItems.AsNoTracking();
        if (categoryId is > 0)
        {
            query = query.Where(i => i.CategoryId == categoryId);
        }

        return await query
            .OrderBy(i => i.Category.SortOrder)
            .ThenBy(i => i.SortOrder)
            .ThenByDescending(i => i.Id)
            .Select(i => new GalleryAdminItem(
                i.Id,
                i.ImagePath,
                i.Translations.Where(t => t.LanguageCode == Languages.Default).Select(t => t.Title).FirstOrDefault() ?? string.Empty,
                i.Category.Translations.Where(t => t.LanguageCode == Languages.Default).Select(t => t.Name).FirstOrDefault() ?? "-",
                i.SortOrder,
                i.IsActive))
            .ToListAsync(cancellationToken);
    }

    public GalleryUploadModel CreateUploadModel() => new()
    {
        Titles = Languages.All.Select(code => new GalleryTitleInput { LanguageCode = code }).ToList()
    };

    public async Task<ServiceResult<int>> UploadAsync(GalleryUploadModel model, CancellationToken cancellationToken = default)
    {
        var files = model.Files.Where(f => f.Length > 0).ToList();
        if (files.Count == 0)
        {
            return ServiceResult<int>.Failure("En az bir fotoğraf seçmelisiniz.");
        }

        if (!await _db.GalleryCategories.AnyAsync(c => c.Id == model.CategoryId, cancellationToken))
        {
            return ServiceResult<int>.Failure("Seçilen kategori bulunamadı.");
        }

        var nextOrder = (await _db.GalleryItems.Where(i => i.CategoryId == model.CategoryId).MaxAsync(i => (int?)i.SortOrder, cancellationToken) ?? -1) + 1;
        var errors = new List<string>();
        var added = 0;

        foreach (var file in files)
        {
            var upload = await _storage.SaveImageAsync(file, ImageFolder.Gallery, cancellationToken);
            if (!upload.Succeeded)
            {
                errors.AddRange(upload.Errors);
                continue;
            }

            var item = new GalleryItem
            {
                CategoryId = model.CategoryId,
                ImagePath = upload.Value!.RelativePath,
                SortOrder = nextOrder++
            };

            foreach (var title in model.Titles.Where(t => Languages.IsSupported(t.LanguageCode) && !string.IsNullOrWhiteSpace(t.Title)))
            {
                item.Translations.Add(new GalleryItemTranslation { LanguageCode = title.LanguageCode, Title = title.Title!.Trim() });
            }

            _db.GalleryItems.Add(item);
            added++;
        }

        if (added > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _cache.Invalidate();
            await _activityLogger.LogAsync("Uploaded", "Gallery", null, $"{added} fotoğraf yüklendi", cancellationToken);
        }

        return errors.Count == 0 ? ServiceResult<int>.Success(added) : ServiceResult<int>.Failure(errors.ToArray());
    }

    public async Task<GalleryItemEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _db.GalleryItems.AsNoTracking().Include(i => i.Translations).FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        var model = new GalleryItemEditModel
        {
            Id = item.Id,
            CategoryId = item.CategoryId,
            SortOrder = item.SortOrder,
            IsActive = item.IsActive,
            ImagePath = item.ImagePath,
            Titles = Languages.All.Select(code => new GalleryTitleInput
            {
                LanguageCode = code,
                Title = item.Translations.FirstOrDefault(t => t.LanguageCode == code)?.Title
            }).ToList()
        };

        await PopulateOptionsAsync(model, cancellationToken);
        return model;
    }

    public async Task PopulateOptionsAsync(GalleryItemEditModel model, CancellationToken cancellationToken = default)
    {
        model.CategoryOptions = await _categoryService.GetOptionsAsync(cancellationToken);
        foreach (var code in Languages.All.Where(code => model.Titles.All(t => t.LanguageCode != code)))
        {
            model.Titles.Add(new GalleryTitleInput { LanguageCode = code });
        }
    }

    public async Task<ServiceResult> UpdateAsync(GalleryItemEditModel model, CancellationToken cancellationToken = default)
    {
        var item = await _db.GalleryItems.Include(i => i.Translations).FirstOrDefaultAsync(i => i.Id == model.Id, cancellationToken);
        if (item is null)
        {
            return ServiceResult.Failure("Fotoğraf bulunamadı.");
        }

        if (!await _db.GalleryCategories.AnyAsync(c => c.Id == model.CategoryId, cancellationToken))
        {
            return ServiceResult.Failure("Seçilen kategori bulunamadı.");
        }

        var image = await AdminHelpers.ReplaceImageAsync(_storage, item.ImagePath, model.ImageFile, false, ImageFolder.Gallery, cancellationToken);
        if (!image.Succeeded)
        {
            return ServiceResult.Failure(image.Errors.ToArray());
        }

        item.ImagePath = image.Value!;
        item.CategoryId = model.CategoryId;
        item.SortOrder = model.SortOrder;
        item.IsActive = model.IsActive;

        foreach (var code in Languages.All)
        {
            var input = model.Titles.FirstOrDefault(t => t.LanguageCode == code);
            var translation = item.Translations.FirstOrDefault(t => t.LanguageCode == code);
            if (string.IsNullOrWhiteSpace(input?.Title))
            {
                if (translation is not null)
                {
                    _db.GalleryItemTranslations.Remove(translation);
                }

                continue;
            }

            if (translation is null)
            {
                item.Translations.Add(new GalleryItemTranslation { LanguageCode = code, Title = input.Title.Trim() });
            }
            else
            {
                translation.Title = input.Title.Trim();
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
        await _activityLogger.LogAsync("Updated", "Gallery", item.Id, null, cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateSortOrderAsync(IReadOnlyList<GallerySortInput> items, CancellationToken cancellationToken = default)
    {
        var ids = items.Select(i => i.Id).ToList();
        var entities = await _db.GalleryItems.Where(i => ids.Contains(i.Id)).ToListAsync(cancellationToken);
        foreach (var entity in entities)
        {
            entity.SortOrder = items.First(i => i.Id == entity.Id).SortOrder;
        }

        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
        await _activityLogger.LogAsync("Reordered", "Gallery", null, $"{entities.Count} fotoğraf sıralandı", cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _db.GalleryItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (item is null)
        {
            return ServiceResult.Failure("Fotoğraf bulunamadı.");
        }

        _db.GalleryItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        _storage.DeletePublicFile(item.ImagePath);
        _cache.Invalidate();
        await _activityLogger.LogAsync("Deleted", "Gallery", id, null, cancellationToken);
        return ServiceResult.Success();
    }
}

public sealed class DocumentAdminService : IDocumentAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IActivityLogger _activityLogger;
    private readonly IContentCache _cache;

    public DocumentAdminService(ApplicationDbContext db, IFileStorageService storage, IActivityLogger activityLogger, IContentCache cache)
    {
        _db = db;
        _storage = storage;
        _activityLogger = activityLogger;
        _cache = cache;
    }

    public async Task<IReadOnlyList<DocumentAdminItem>> GetListAsync(CancellationToken cancellationToken = default) =>
        await _db.Documents.AsNoTracking()
            .OrderBy(d => d.DocumentType)
            .ThenBy(d => d.SortOrder)
            .ThenByDescending(d => d.DocumentDate)
            .Select(d => new DocumentAdminItem(d.Id, d.Title, d.DocumentType, d.LanguageCode, d.DocumentDate, d.FileSize, d.FilePath, d.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<DocumentEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var document = await _db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        return document is null ? null : new DocumentEditModel
        {
            Id = document.Id,
            Title = document.Title,
            Description = document.Description,
            DocumentType = document.DocumentType,
            LanguageCode = document.LanguageCode,
            DocumentDate = document.DocumentDate,
            SortOrder = document.SortOrder,
            IsActive = document.IsActive,
            FilePath = document.FilePath,
            CoverImagePath = document.CoverImagePath
        };
    }

    public async Task<ServiceResult<int>> SaveAsync(DocumentEditModel model, CancellationToken cancellationToken = default)
    {
        Document? document;
        var isNew = model.Id == 0;
        if (isNew)
        {
            if (model.File is not { Length: > 0 })
            {
                return ServiceResult<int>.Failure("Lütfen bir PDF dosyası seçin.");
            }

            document = new Document();
            _db.Documents.Add(document);
        }
        else
        {
            document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == model.Id, cancellationToken);
            if (document is null)
            {
                return ServiceResult<int>.Failure("Belge bulunamadı.");
            }
        }

        if (model.DocumentType == DocumentType.Catalog && model.CoverImage is not { Length: > 0 } && document.CoverImagePath is null)
        {
            return ServiceResult<int>.Failure("Ürün kataloğu için bir kapak görseli seçin.");
        }

        if (model.CoverImage is { Length: > 0 })
        {
            var cover = await _storage.SaveImageAsync(model.CoverImage, ImageFolder.Catalogs, cancellationToken);
            if (!cover.Succeeded)
            {
                return ServiceResult<int>.Failure(cover.Errors.ToArray());
            }

            _storage.DeletePublicFile(document.CoverImagePath);
            document.CoverImagePath = cover.Value!.RelativePath;
        }

        if (model.File is { Length: > 0 })
        {
            var upload = await _storage.SaveDocumentAsync(model.File, cancellationToken);
            if (!upload.Succeeded)
            {
                return ServiceResult<int>.Failure(upload.Errors.ToArray());
            }

            _storage.DeletePublicFile(document.FilePath);
            document.FilePath = upload.Value!.RelativePath;
            document.FileSize = upload.Value.Size;
        }

        document.Title = model.Title.Trim();
        document.Description = AdminHelpers.Clean(model.Description);
        document.DocumentType = model.DocumentType;
        document.LanguageCode = Languages.IsSupported(model.LanguageCode) ? model.LanguageCode : null;
        document.DocumentDate = model.DocumentDate.Date;
        document.SortOrder = model.SortOrder;
        document.IsActive = model.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
        await _activityLogger.LogAsync(isNew ? "Created" : "Updated", "Document", document.Id, document.Title, cancellationToken);
        return ServiceResult<int>.Success(document.Id);
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (document is null)
        {
            return ServiceResult.Failure("Belge bulunamadı.");
        }

        _db.Documents.Remove(document);
        await _db.SaveChangesAsync(cancellationToken);
        _storage.DeletePublicFile(document.FilePath);
        _storage.DeletePublicFile(document.CoverImagePath);
        _cache.Invalidate();
        await _activityLogger.LogAsync("Deleted", "Document", id, document.Title, cancellationToken);
        return ServiceResult.Success();
    }
}

public sealed class ServiceAdminService : IServiceAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IHtmlSanitizerService _sanitizer;
    private readonly IActivityLogger _activityLogger;
    private readonly IContentCache _cache;
    private readonly IGalleryCategoryAdminService _galleryCategoryService;
    private readonly IProductCategoryAdminService _productCategoryService;

    public ServiceAdminService(ApplicationDbContext db, IFileStorageService storage, IHtmlSanitizerService sanitizer, IActivityLogger activityLogger, IContentCache cache,
        IGalleryCategoryAdminService galleryCategoryService, IProductCategoryAdminService productCategoryService)
    {
        _galleryCategoryService = galleryCategoryService;
        _productCategoryService = productCategoryService;
        _db = db;
        _storage = storage;
        _sanitizer = sanitizer;
        _activityLogger = activityLogger;
        _cache = cache;
    }

    public async Task<IReadOnlyList<TranslatedListItem>> GetListAsync(CancellationToken cancellationToken = default) =>
        await _db.Services.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => new TranslatedListItem(
                c.Id,
                c.Translations.Where(t => t.LanguageCode == Languages.Default).Select(t => t.Title).FirstOrDefault() ?? "-",
                c.Image,
                c.SortOrder,
                c.IsActive,
                c.Translations.Select(t => t.LanguageCode).ToList(),
                c.UpdatedDate ?? c.CreatedDate))
            .ToListAsync(cancellationToken);

    public async Task<ServiceEditModel> CreateNewAsync(CancellationToken cancellationToken = default)
    {
        var model = new ServiceEditModel
        {
            Translations = Languages.All.Select(code => new ServiceTranslationInput { LanguageCode = code }).ToList()
        };
        await PopulateOptionsAsync(model, cancellationToken);
        return model;
    }

    public async Task PopulateOptionsAsync(ServiceEditModel model, CancellationToken cancellationToken = default)
    {
        model.GalleryOptions = await _galleryCategoryService.GetOptionsAsync(cancellationToken);
        model.CategoryOptions = await _productCategoryService.GetOptionsAsync(cancellationToken);
    }

    public async Task<ServiceEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var service = await _db.Services.AsNoTracking().Include(c => c.Translations).Include(c => c.ProductCategories)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (service is null)
        {
            return null;
        }

        var model = new ServiceEditModel
        {
            Id = service.Id,
            IconClass = service.IconClass,
            Image = service.Image,
            SortOrder = service.SortOrder,
            IsActive = service.IsActive,
            ShowOnHome = service.ShowOnHome,
            GalleryCategoryId = service.GalleryCategoryId,
            ProductCategoryIds = service.ProductCategories.Select(c => c.Id).ToList(),
            Translations = Languages.All.Select(code =>
            {
                var t = service.Translations.FirstOrDefault(x => x.LanguageCode == code);
                return new ServiceTranslationInput
                {
                    LanguageCode = code,
                    Title = t?.Title,
                    Slug = t?.Slug,
                    Summary = t?.Summary,
                    Content = t?.Content,
                    Specifications = t?.Specifications,
                    MetaTitle = t?.MetaTitle,
                    MetaDescription = t?.MetaDescription
                };
            }).ToList()
        };
        await PopulateOptionsAsync(model, cancellationToken);
        return model;
    }

    public async Task<ServiceResult<int>> SaveAsync(ServiceEditModel model, CancellationToken cancellationToken = default)
    {
        if (model.GalleryCategoryId is > 0 && !await _db.GalleryCategories.AnyAsync(c => c.Id == model.GalleryCategoryId, cancellationToken))
        {
            return ServiceResult<int>.Failure("Seçilen galeri kategorisi bulunamadı.");
        }

        Service? service;
        var isNew = model.Id == 0;
        if (isNew)
        {
            service = new Service();
            _db.Services.Add(service);
        }
        else
        {
            service = await _db.Services.Include(c => c.Translations).Include(c => c.ProductCategories)
                .FirstOrDefaultAsync(c => c.Id == model.Id, cancellationToken);
            if (service is null)
            {
                return ServiceResult<int>.Failure("Hizmet bulunamadı.");
            }
        }

        var image = await AdminHelpers.ReplaceImageAsync(_storage, service.Image, model.ImageFile, model.RemoveImage, ImageFolder.Services, cancellationToken);
        if (!image.Succeeded)
        {
            return ServiceResult<int>.Failure(image.Errors.ToArray());
        }

        service.Image = image.Value;
        service.IconClass = string.IsNullOrWhiteSpace(model.IconClass) ? "fa-solid fa-gear" : model.IconClass.Trim();
        service.SortOrder = model.SortOrder;
        service.IsActive = model.IsActive;
        service.ShowOnHome = model.ShowOnHome;
        service.GalleryCategoryId = model.GalleryCategoryId is > 0 ? model.GalleryCategoryId : null;
        service.ProductCategories.Clear();
        foreach (var category in await _db.ProductCategories.Where(c => model.ProductCategoryIds.Contains(c.Id)).ToListAsync(cancellationToken))
        {
            service.ProductCategories.Add(category);
        }

        foreach (var code in Languages.All)
        {
            var input = model.Translations.FirstOrDefault(t => t.LanguageCode == code);
            var translation = service.Translations.FirstOrDefault(t => t.LanguageCode == code);
            if (input is null || string.IsNullOrWhiteSpace(input.Title))
            {
                if (translation is not null)
                {
                    _db.ServiceTranslations.Remove(translation);
                }

                continue;
            }

            if (translation is null)
            {
                translation = new ServiceTranslation { LanguageCode = code };
                service.Translations.Add(translation);
            }

            var serviceId = service.Id;
            translation.Title = input.Title.Trim();
            translation.Slug = await AdminHelpers.UniqueSlugAsync(
                _db.ServiceTranslations.Where(t => t.LanguageCode == code && t.ServiceId != serviceId).Select(t => t.Slug),
                input.Slug, translation.Title, cancellationToken);
            translation.Summary = AdminHelpers.Clean(input.Summary);
            translation.Content = AdminHelpers.Clean(_sanitizer.Sanitize(input.Content));
            translation.Specifications = AdminHelpers.Clean(input.Specifications);
            translation.MetaTitle = AdminHelpers.Clean(input.MetaTitle);
            translation.MetaDescription = AdminHelpers.Clean(input.MetaDescription);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
        await _activityLogger.LogAsync(isNew ? "Created" : "Updated", "Service", service.Id,
            service.Translations.FirstOrDefault(t => t.LanguageCode == Languages.Default)?.Title, cancellationToken);
        return ServiceResult<int>.Success(service.Id);
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var service = await _db.Services.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (service is null)
        {
            return ServiceResult.Failure("Hizmet bulunamadı.");
        }

        _db.Services.Remove(service);
        await _db.SaveChangesAsync(cancellationToken);
        _storage.DeletePublicFile(service.Image);
        _cache.Invalidate();
        await _activityLogger.LogAsync("Deleted", "Service", id, null, cancellationToken);
        return ServiceResult.Success();
    }
}
