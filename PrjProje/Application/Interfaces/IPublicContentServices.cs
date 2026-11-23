using PrjProje.Application.Common;
using PrjProje.Application.Models;

namespace PrjProje.Application.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<ProductCategoryDto>> GetCategoriesAsync(string languageCode, CancellationToken cancellationToken = default);
    Task<ProductCategoryDto?> GetCategoryBySlugAsync(string languageCode, string slug, CancellationToken cancellationToken = default);
    Task<PagedResult<ProductListItemDto>> GetProductsAsync(string languageCode, ProductFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductListItemDto>> GetFeaturedAsync(string languageCode, int count, CancellationToken cancellationToken = default);
    Task<ProductDetailDto?> GetDetailAsync(string languageCode, string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductOptionDto>> GetProductOptionsAsync(string languageCode, CancellationToken cancellationToken = default);
    Task<string?> GetProductNameAsync(int productId, string languageCode, CancellationToken cancellationToken = default);
}

public interface IBlogService
{
    Task<IReadOnlyList<BlogPostListItemDto>> GetLatestAsync(string languageCode, int count, CancellationToken cancellationToken = default);
    Task<PagedResult<BlogPostListItemDto>> GetPostsAsync(string languageCode, BlogFilter filter, CancellationToken cancellationToken = default);
    Task<BlogPostDetailDto?> GetDetailAsync(string languageCode, string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BlogCategoryDto>> GetCategoriesAsync(string languageCode, CancellationToken cancellationToken = default);
    Task<TagDto?> GetTagAsync(string languageCode, string slug, CancellationToken cancellationToken = default);
}

public interface IGalleryService
{
    Task<GalleryDto> GetGalleryAsync(string languageCode, CancellationToken cancellationToken = default);
}

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentDto>> GetPublicDocumentsAsync(string languageCode, CancellationToken cancellationToken = default);
}

public interface IServicePageService
{
    Task<IReadOnlyList<ServiceDto>> GetServicesAsync(string languageCode, bool homeOnly = false, CancellationToken cancellationToken = default);
    Task<ServiceDto?> GetDetailAsync(string languageCode, string slug, CancellationToken cancellationToken = default);
}

public interface ISiteSettingsService
{
    Task<SiteSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(SiteSettingsDto settings, CancellationToken cancellationToken = default);
}

public interface ISiteContentService
{
    Task<IReadOnlyList<ReferenceDto>> GetReferencesAsync(CancellationToken cancellationToken = default);

    /// <summary>Announcements that are active right now (start/end dates applied at call time).</summary>
    Task<IReadOnlyList<AnnouncementDto>> GetCurrentAnnouncementsAsync(string languageCode, CancellationToken cancellationToken = default);
}

public interface ISitemapService
{
    Task<IReadOnlyList<SitemapEntry>> GetDynamicEntriesAsync(CancellationToken cancellationToken = default);
}

public interface IContactService
{
    Task<ServiceResult> SubmitAsync(ContactFormModel form, RequestContext context, CancellationToken cancellationToken = default);

    /// <summary>Saves a job application and its CV; errors are resource keys for the CV field.</summary>

    /// <summary>Counts a click on the WhatsApp button for today.</summary>
    Task RecordWhatsAppClickAsync(CancellationToken cancellationToken = default);
}

public interface IQuoteService
{
    Task<ServiceResult> SubmitAsync(QuoteFormModel form, RequestContext context, CancellationToken cancellationToken = default);
}
