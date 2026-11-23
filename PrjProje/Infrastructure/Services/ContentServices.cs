using System.Reflection;
using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models;
using PrjProje.Domain.Entities;
using PrjProje.Domain.Enums;
using PrjProje.Infrastructure.Data;
using PrjProje.Infrastructure.Services.Admin;
using Microsoft.EntityFrameworkCore;

namespace PrjProje.Infrastructure.Services;

public sealed class GalleryService : IGalleryService
{
    private readonly ApplicationDbContext _db;
    private readonly IContentCache _cache;

    public GalleryService(ApplicationDbContext db, IContentCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<GalleryDto> GetGalleryAsync(string languageCode, CancellationToken cancellationToken = default) =>
        _cache.GetOrCreateAsync($"gallery:{languageCode}", async () =>
        {
            var categories = await _db.GalleryCategories
                .AsNoTracking()
                .Where(c => c.Items.Any(i => i.IsActive))
                .OrderBy(c => c.SortOrder)
                .Select(c => new
                {
                    c.Id,
                    Name = c.Translations.Where(t => t.LanguageCode == languageCode).Select(t => t.Name).FirstOrDefault()
                           ?? c.Translations.Select(t => t.Name).FirstOrDefault() ?? string.Empty,
                    Slug = c.Translations.Where(t => t.LanguageCode == languageCode).Select(t => t.Slug).FirstOrDefault()
                           ?? c.Translations.Select(t => t.Slug).FirstOrDefault() ?? string.Empty,
                    Count = c.Items.Count(i => i.IsActive)
                })
                .ToListAsync(cancellationToken);

            var items = await _db.GalleryItems
                .AsNoTracking()
                .Where(i => i.IsActive)
                .OrderBy(i => i.Category.SortOrder)
                .ThenBy(i => i.SortOrder)
                .ThenByDescending(i => i.Id)
                .Select(i => new GalleryItemDto(
                    i.Id,
                    i.CategoryId,
                    i.Translations.Where(t => t.LanguageCode == languageCode).Select(t => t.Title).FirstOrDefault()
                        ?? i.Category.Translations.Where(t => t.LanguageCode == languageCode).Select(t => t.Name).FirstOrDefault()
                        ?? string.Empty,
                    i.ImagePath))
                .ToListAsync(cancellationToken);

            return new GalleryDto(
                categories.Select(c => new GalleryCategoryDto(c.Id, c.Name, c.Slug, c.Count)).ToList(),
                items);
        });
}

public sealed class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _db;
    private readonly IContentCache _cache;

    public DocumentService(ApplicationDbContext db, IContentCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<IReadOnlyList<DocumentDto>> GetPublicDocumentsAsync(string languageCode, CancellationToken cancellationToken = default) =>
        _cache.GetOrCreateAsync($"documents:{languageCode}", async () =>
            (IReadOnlyList<DocumentDto>)await _db.Documents
                .AsNoTracking()
                .Where(d => d.IsActive && (d.LanguageCode == null || d.LanguageCode == languageCode))
                .OrderBy(d => d.DocumentType)
                .ThenBy(d => d.SortOrder)
                .ThenByDescending(d => d.DocumentDate)
                .Select(d => new DocumentDto(d.Id, d.Title, d.Description, d.FilePath, d.FileSize, d.DocumentType, d.DocumentDate, d.CoverImagePath))
                .ToListAsync(cancellationToken));
}

public sealed class CapabilityService : ICapabilityService
{
    private readonly ApplicationDbContext _db;
    private readonly IContentCache _cache;

    public CapabilityService(ApplicationDbContext db, IContentCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyList<CapabilityDto>> GetCapabilitiesAsync(string languageCode, bool homeOnly = false, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(languageCode, cancellationToken);
        return homeOnly ? all.Where(c => c.ShowOnHome).Select(c => c.Dto).ToList() : all.Select(c => c.Dto).ToList();
    }

    public async Task<CapabilityDto?> GetDetailAsync(string languageCode, string slug, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(languageCode, cancellationToken);
        return all.Select(c => c.Dto).FirstOrDefault(c => c.Slug == slug);
    }

    private Task<IReadOnlyList<(CapabilityDto Dto, bool ShowOnHome)>> GetAllAsync(string languageCode, CancellationToken cancellationToken) =>
        _cache.GetOrCreateAsync($"capabilities:{languageCode}", async () =>
        {
            var rows = await _db.Capabilities
                .AsNoTracking()
                .Where(c => c.IsActive && c.Translations.Any(t => t.LanguageCode == languageCode))
                .OrderBy(c => c.SortOrder)
                .Select(c => new
                {
                    c.Id,
                    c.IconClass,
                    c.Image,
                    c.ShowOnHome,
                    c.GalleryCategoryId,
                    ProductCategoryIds = c.ProductCategories.Select(p => p.Id).ToList(),
                    Translations = c.Translations.ToList()
                })
                .ToListAsync(cancellationToken);

            return (IReadOnlyList<(CapabilityDto, bool)>)rows.Select(c =>
            {
                var t = c.Translations.First(x => x.LanguageCode == languageCode);
                var dto = new CapabilityDto
                {
                    Id = c.Id,
                    Title = t.Title,
                    Slug = t.Slug,
                    Summary = t.Summary,
                    Content = t.Content,
                    IconClass = c.IconClass,
                    Image = c.Image,
                    GalleryCategoryId = c.GalleryCategoryId,
                    ProductCategoryIds = c.ProductCategoryIds,
                    Specifications = AdminHelpers.ParseSpecifications(t.Specifications).Select(s => new SpecificationDto(s.Name, s.Value)).ToList(),
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription,
                    AlternateSlugs = c.Translations.ToDictionary(x => x.LanguageCode, x => x.Slug)
                };
                return (dto, c.ShowOnHome);
            }).ToList();
        });
}

public sealed class SiteSettingsService : ISiteSettingsService
{
    private static readonly PropertyInfo[] SettingProperties = typeof(SiteSettingsDto)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.PropertyType == typeof(string) && p.CanWrite)
        .ToArray();

    private readonly ApplicationDbContext _db;
    private readonly IContentCache _cache;

    public SiteSettingsService(ApplicationDbContext db, IContentCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<SiteSettingsDto> GetAsync(CancellationToken cancellationToken = default) =>
        _cache.GetOrCreateAsync("site-settings", async () =>
        {
            var values = await _db.SiteSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);
            var settings = new SiteSettingsDto();
            foreach (var property in SettingProperties)
            {
                if (values.TryGetValue(property.Name, out var value) && value is not null)
                {
                    property.SetValue(settings, value);
                }
            }

            return settings;
        }, TimeSpan.FromHours(1));

    public async Task SaveAsync(SiteSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var existing = await _db.SiteSettings.ToDictionaryAsync(s => s.Key, cancellationToken);
        foreach (var property in SettingProperties)
        {
            var value = (property.GetValue(settings) as string)?.Trim();
            if (existing.TryGetValue(property.Name, out var setting))
            {
                setting.Value = value;
            }
            else
            {
                _db.SiteSettings.Add(new SiteSetting { Key = property.Name, Value = value });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
    }
}

public sealed class SitemapService : ISitemapService
{
    private readonly ApplicationDbContext _db;
    private readonly IContentCache _cache;

    public SitemapService(ApplicationDbContext db, IContentCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<IReadOnlyList<SitemapEntry>> GetDynamicEntriesAsync(CancellationToken cancellationToken = default) =>
        _cache.GetOrCreateAsync("sitemap", async () =>
        {
            var entries = new List<SitemapEntry>();
            var now = DateTime.UtcNow;

            var categories = await _db.ProductCategories.AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => new { Date = c.UpdatedDate ?? c.CreatedDate, Slugs = c.Translations.Select(t => new { t.LanguageCode, t.Slug }).ToList() })
                .ToListAsync(cancellationToken);
            entries.AddRange(categories.Select(c => new SitemapEntry("ProductCategory", c.Slugs.ToDictionary(s => s.LanguageCode, s => s.Slug), c.Date)));

            var products = await _db.Products.AsNoTracking()
                .Where(p => p.IsActive && p.Category.IsActive)
                .Select(p => new { Date = p.UpdatedDate ?? p.CreatedDate, Slugs = p.Translations.Select(t => new { t.LanguageCode, t.Slug }).ToList() })
                .ToListAsync(cancellationToken);
            entries.AddRange(products.Select(p => new SitemapEntry("ProductDetail", p.Slugs.ToDictionary(s => s.LanguageCode, s => s.Slug), p.Date)));

            var posts = await _db.BlogPosts.AsNoTracking()
                .Select(p => new
                {
                    Date = p.UpdatedDate ?? p.CreatedDate,
                    Slugs = p.Translations
                        .Where(t => t.Status == PublishStatus.Published && t.PublishedDate <= now)
                        .Select(t => new { t.LanguageCode, t.Slug })
                        .ToList()
                })
                .ToListAsync(cancellationToken);
            entries.AddRange(posts.Where(p => p.Slugs.Count > 0)
                .Select(p => new SitemapEntry("BlogDetail", p.Slugs.ToDictionary(s => s.LanguageCode, s => s.Slug), p.Date)));

            var capabilities = await _db.Capabilities.AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => new { Date = c.UpdatedDate ?? c.CreatedDate, Slugs = c.Translations.Select(t => new { t.LanguageCode, t.Slug }).ToList() })
                .ToListAsync(cancellationToken);
            entries.AddRange(capabilities.Select(c => new SitemapEntry("ManufacturingDetail", c.Slugs.ToDictionary(s => s.LanguageCode, s => s.Slug), c.Date)));

            return (IReadOnlyList<SitemapEntry>)entries;
        }, TimeSpan.FromHours(1));
}

public sealed class ContactService : IContactService
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly IActivityLogger _activityLogger;
    private readonly INotificationService _notifications;

    public ContactService(ApplicationDbContext db, IFileStorageService fileStorage, IActivityLogger activityLogger, INotificationService notifications)
    {
        _db = db;
        _fileStorage = fileStorage;
        _activityLogger = activityLogger;
        _notifications = notifications;
    }

    public async Task RecordWhatsAppClickAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (await _db.WhatsAppClickDays.Where(d => d.Day == today).ExecuteUpdateAsync(s => s.SetProperty(d => d.Count, d => d.Count + 1), cancellationToken) > 0)
        {
            return;
        }

        try
        {
            _db.WhatsAppClickDays.Add(new WhatsAppClickDay { Day = today, Count = 1 });
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another click created today's row first; count on it instead.
            _db.ChangeTracker.Clear();
            await _db.WhatsAppClickDays.Where(d => d.Day == today).ExecuteUpdateAsync(s => s.SetProperty(d => d.Count, d => d.Count + 1), cancellationToken);
        }
    }

    public async Task<ServiceResult> SubmitApplicationAsync(JobApplicationFormModel form, RequestContext context, CancellationToken cancellationToken = default)
    {
        if (!CareerAreas.Turkish.ContainsKey(form.Position))
        {
            return ServiceResult.Failure("Validation.Required");
        }

        var resume = await _fileStorage.SaveResumeAsync(form.Resume!, cancellationToken);
        if (!resume.Succeeded)
        {
            return ServiceResult.Failure(resume.Errors.ToArray());
        }

        var application = new JobApplication
        {
            FullName = form.FullName.Trim(),
            Email = form.Email.Trim(),
            Phone = form.Phone.Trim(),
            City = string.IsNullOrWhiteSpace(form.City) ? null : form.City.Trim(),
            Position = form.Position,
            Message = string.IsNullOrWhiteSpace(form.Message) ? null : form.Message.Trim(),
            ResumePath = resume.Value!.RelativePath,
            ResumeOriginalName = resume.Value.OriginalName,
            ResumeSize = resume.Value.Size,
            PrivacyConsent = form.PrivacyConsent,
            LanguageCode = Languages.IsSupported(context.LanguageCode) ? context.LanguageCode : Languages.Default,
            IpAddress = context.IpAddress
        };

        _db.JobApplications.Add(application);
        await _db.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync("Received", "JobApplication", application.Id, $"{application.FullName} - {CareerAreas.Name(application.Position)}", cancellationToken);
        // The CV itself stays out of the mail: it is personal data and is downloaded from the panel.
        await _notifications.NotifyAsync(NotificationKind.HumanResources,
            $"Yeni iş başvurusu: {application.FullName} ({CareerAreas.Name(application.Position)})",
            $"Ad Soyad: {application.FullName}\nAlan: {CareerAreas.Name(application.Position)}\nE-posta: {application.Email}\nTelefon: {application.Phone}\nŞehir: {application.City ?? "-"}\n\n{application.Message}\n\nCV, yönetim panelinde İnsan Kaynakları bölümünden indirilebilir.",
            application.Email, cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SubmitAsync(ContactFormModel form, RequestContext context, CancellationToken cancellationToken = default)
    {
        var message = new ContactMessage
        {
            FullName = form.FullName.Trim(),
            Company = form.Company?.Trim(),
            Email = form.Email.Trim(),
            Phone = form.Phone?.Trim(),
            Subject = form.Subject.Trim(),
            Message = form.Message.Trim(),
            PrivacyConsent = form.PrivacyConsent,
            LanguageCode = Languages.IsSupported(context.LanguageCode) ? context.LanguageCode : Languages.Default,
            IpAddress = context.IpAddress
        };

        _db.ContactMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync("Received", "ContactMessage", message.Id, $"{message.FullName} - {message.Subject}", cancellationToken);
        await _notifications.NotifyAsync(NotificationKind.Sales,
            $"Yeni iletişim mesajı: {message.Subject}",
            $"Ad Soyad: {message.FullName}\nFirma: {message.Company ?? "-"}\nE-posta: {message.Email}\nTelefon: {message.Phone ?? "-"}\nKonu: {message.Subject}\n\n{message.Message}",
            message.Email, cancellationToken);
        return ServiceResult.Success();
    }
}

public sealed class QuoteService : IQuoteService
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly IProductService _productService;
    private readonly IActivityLogger _activityLogger;
    private readonly INotificationService _notifications;

    public QuoteService(ApplicationDbContext db, IFileStorageService fileStorage, IProductService productService, IActivityLogger activityLogger, INotificationService notifications)
    {
        _db = db;
        _fileStorage = fileStorage;
        _productService = productService;
        _activityLogger = activityLogger;
        _notifications = notifications;
    }

    public async Task<ServiceResult> SubmitAsync(QuoteFormModel form, RequestContext context, CancellationToken cancellationToken = default)
    {
        StoredFile? attachment = null;
        if (form.Attachment is { Length: > 0 })
        {
            var upload = await _fileStorage.SavePrivateAttachmentAsync(form.Attachment, cancellationToken);
            if (!upload.Succeeded)
            {
                return ServiceResult.Failure(upload.Errors.ToArray());
            }

            attachment = upload.Value;
        }

        int? productId = null;
        var productName = form.ProductName?.Trim();
        if (form.ProductId is > 0)
        {
            var name = await _productService.GetProductNameAsync(form.ProductId.Value, context.LanguageCode, cancellationToken);
            if (name is not null)
            {
                productId = form.ProductId;
                productName = string.IsNullOrWhiteSpace(productName) ? name : productName;
            }
        }

        var quote = new QuoteRequest
        {
            FullName = form.FullName.Trim(),
            CompanyName = form.CompanyName.Trim(),
            Email = form.Email.Trim(),
            Phone = form.Phone.Trim(),
            ProductId = productId,
            ProductName = productName,
            Quantity = form.Quantity?.Trim(),
            Description = form.Description.Trim(),
            AttachmentPath = attachment?.RelativePath,
            AttachmentOriginalName = attachment?.OriginalName,
            AttachmentSize = attachment?.Size,
            PrivacyConsent = form.PrivacyConsent,
            LanguageCode = Languages.IsSupported(context.LanguageCode) ? context.LanguageCode : Languages.Default,
            IpAddress = context.IpAddress
        };

        _db.QuoteRequests.Add(quote);
        await _db.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync("Received", "QuoteRequest", quote.Id, $"{quote.CompanyName} - {quote.ProductName ?? "-"}", cancellationToken);
        await _notifications.NotifyAsync(NotificationKind.Sales,
            $"Yeni teklif talebi: {quote.CompanyName}",
            $"Firma: {quote.CompanyName}\nAd Soyad: {quote.FullName}\nE-posta: {quote.Email}\nTelefon: {quote.Phone}\nÜrün: {quote.ProductName ?? "-"}\nMiktar: {quote.Quantity ?? "-"}\nEk dosya: {quote.AttachmentOriginalName ?? "yok"}\n\n{quote.Description}",
            quote.Email, cancellationToken);
        return ServiceResult.Success();
    }
}
