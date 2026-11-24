using PrjProje.Application.Common;
using PrjProje.Application.Models.Admin;
using PrjProje.Domain.Enums;

namespace PrjProje.Application.Interfaces;

public interface IProductAdminService
{
    Task<PagedResult<ProductAdminListItem>> GetListAsync(AdminListFilter filter, CancellationToken cancellationToken = default);
    Task<ProductEditModel> CreateNewAsync(CancellationToken cancellationToken = default);
    Task<ProductEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task PopulateOptionsAsync(ProductEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> SaveAsync(ProductEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> ToggleActiveAsync(int id, CancellationToken cancellationToken = default);
}

public interface IProductCategoryAdminService
{
    Task<IReadOnlyList<TranslatedListItem>> GetListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OptionItem>> GetOptionsAsync(CancellationToken cancellationToken = default);
    ProductCategoryEditModel CreateNew();
    Task<ProductCategoryEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> SaveAsync(ProductCategoryEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IBlogAdminService
{
    Task<PagedResult<BlogAdminListItem>> GetListAsync(AdminListFilter filter, CancellationToken cancellationToken = default);
    Task<BlogPostEditModel> CreateNewAsync(string authorName, CancellationToken cancellationToken = default);
    Task<BlogPostEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task PopulateOptionsAsync(BlogPostEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> SaveAsync(BlogPostEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> SetStatusAsync(int id, PublishStatus status, CancellationToken cancellationToken = default);
}

/// <summary>Shared contract for simple translated categories (blog, gallery).</summary>
public interface ISimpleCategoryAdminService
{
    Task<IReadOnlyList<SimpleCategoryListItem>> GetListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OptionItem>> GetOptionsAsync(CancellationToken cancellationToken = default);
    SimpleCategoryEditModel CreateNew();
    Task<SimpleCategoryEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> SaveAsync(SimpleCategoryEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IBlogCategoryAdminService : ISimpleCategoryAdminService
{
}

public interface IGalleryCategoryAdminService : ISimpleCategoryAdminService
{
}

public interface IGalleryAdminService
{
    Task<IReadOnlyList<GalleryAdminItem>> GetListAsync(int? categoryId, CancellationToken cancellationToken = default);
    GalleryUploadModel CreateUploadModel();
    Task<ServiceResult<int>> UploadAsync(GalleryUploadModel model, CancellationToken cancellationToken = default);
    Task<GalleryItemEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task PopulateOptionsAsync(GalleryItemEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(GalleryItemEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateSortOrderAsync(IReadOnlyList<GallerySortInput> items, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IDocumentAdminService
{
    Task<IReadOnlyList<DocumentAdminItem>> GetListAsync(CancellationToken cancellationToken = default);
    Task<DocumentEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> SaveAsync(DocumentEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IServiceAdminService
{
    Task<IReadOnlyList<TranslatedListItem>> GetListAsync(CancellationToken cancellationToken = default);
    Task<ServiceEditModel> CreateNewAsync(CancellationToken cancellationToken = default);
    Task<ServiceEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task PopulateOptionsAsync(ServiceEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> SaveAsync(ServiceEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IContactAdminService
{
    Task<PagedResult<ContactMessageDto>> GetListAsync(bool? unreadOnly, int page, CancellationToken cancellationToken = default);
    Task<ContactMessageDto?> GetAndMarkReadAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IQuoteAdminService
{
    Task<PagedResult<QuoteRequestListItem>> GetListAsync(QuoteStatus? status, int page, CancellationToken cancellationToken = default);
    Task<QuoteRequestDto?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateStatusAsync(QuoteStatusUpdateModel model, CancellationToken cancellationToken = default);
    Task<QuoteAttachment?> GetAttachmentAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IUserAdminService
{
    Task<IReadOnlyList<UserListItem>> GetListAsync(CancellationToken cancellationToken = default);
    Task<UserEditModel?> GetForEditAsync(string id);
    Task<ServiceResult> CreateAsync(UserEditModel model);
    Task<ServiceResult> UpdateAsync(UserEditModel model, string currentUserId);
    Task<ServiceResult> DeleteAsync(string id, string currentUserId);
}

public interface IReferenceAdminService
{
    Task<IReadOnlyList<ReferenceAdminItem>> GetListAsync(CancellationToken cancellationToken = default);
    Task<ReferenceEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> SaveAsync(ReferenceEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IVideoAdminService
{
    Task<IReadOnlyList<VideoAdminItem>> GetListAsync(CancellationToken cancellationToken = default);
    Task<VideoEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> SaveAsync(VideoEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface ILegacyRedirectAdminService
{
    Task<IReadOnlyList<LegacyRedirectAdminItem>> GetListAsync(CancellationToken cancellationToken = default);
    Task<LegacyRedirectEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> SaveAsync(LegacyRedirectEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IAnnouncementAdminService
{
    Task<IReadOnlyList<AnnouncementAdminItem>> GetListAsync(CancellationToken cancellationToken = default);
    Task<AnnouncementEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> SaveAsync(AnnouncementEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IContentTextAdminService
{
    IReadOnlyList<ContentTextGroup> Groups { get; }
    Task<ContentTextPageModel> GetPageAsync(string? groupId, string? search, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> SaveAsync(IReadOnlyList<ContentTextRow> rows, CancellationToken cancellationToken = default);
}

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default);
}
