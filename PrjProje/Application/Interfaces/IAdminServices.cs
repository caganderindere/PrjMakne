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

public interface ICapabilityAdminService
{
    Task<IReadOnlyList<TranslatedListItem>> GetListAsync(CancellationToken cancellationToken = default);
    Task<CapabilityEditModel> CreateNewAsync(CancellationToken cancellationToken = default);
    Task<CapabilityEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task PopulateOptionsAsync(CapabilityEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> SaveAsync(CapabilityEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IContactAdminService
{
    Task<PagedResult<ContactMessageDto>> GetListAsync(bool? unreadOnly, int page, CancellationToken cancellationToken = default);
    Task<ContactMessageDto?> GetAndMarkReadAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IJobApplicationAdminService
{
    Task<PagedResult<JobApplicationDto>> GetListAsync(bool? unreadOnly, int page, CancellationToken cancellationToken = default);
    Task<JobApplicationDto?> GetAndMarkReadAsync(int id, CancellationToken cancellationToken = default);
    Task<QuoteAttachment?> GetResumeAsync(int id, CancellationToken cancellationToken = default);
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

/// <summary>
/// Meeting minutes. Every method takes the signed-in user's id: minutes exist for a user only when they wrote them or were
/// ticked as a participant; anyone else gets the same answer as for minutes that do not exist.
/// </summary>
public interface IMeetingMinuteService
{
    Task<PagedResult<MeetingMinuteListItem>> GetListAsync(string userId, string? search, int page, CancellationToken cancellationToken = default);
    Task<MeetingMinuteDto?> GetAsync(int id, string userId, CancellationToken cancellationToken = default);
    Task<MeetingMinuteEditModel?> GetForEditAsync(int id, string userId, CancellationToken cancellationToken = default);

    /// <summary>Active panel users to tick as participants, plus anyone already ticked on <paramref name="selectedIds"/>.</summary>
    Task<IReadOnlyList<UserOption>> GetUserOptionsAsync(IEnumerable<string> selectedIds, CancellationToken cancellationToken = default);

    /// <summary>Names offered for participants without a panel account: the ones listed in recent minutes.</summary>
    Task<IReadOnlyList<string>> GetParticipantSuggestionsAsync(CancellationToken cancellationToken = default);

    /// <summary>New minutes get the next number of the current year; the writer is the user saving them first.</summary>
    Task<ServiceResult<int>> SaveAsync(MeetingMinuteEditModel model, string userId, string userName, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, string userId, CancellationToken cancellationToken = default);
}

public interface IAnnouncementAdminService
{
    Task<IReadOnlyList<AnnouncementAdminItem>> GetListAsync(CancellationToken cancellationToken = default);
    Task<AnnouncementEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> SaveAsync(AnnouncementEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IBusinessCardAdminService
{
    Task<IReadOnlyList<BusinessCardAdminItem>> GetListAsync(CancellationToken cancellationToken = default);
    Task<BusinessCardEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> SaveAsync(BusinessCardEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IFairAdminService
{
    Task<IReadOnlyList<FairAdminItem>> GetListAsync(CancellationToken cancellationToken = default);
    Task<FairEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> SaveAsync(FairEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<FairLeadsModel?> GetLeadsAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteLeadAsync(int leadId, CancellationToken cancellationToken = default);
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

    Task<MarketingSummaryDto> GetMarketingAsync(CancellationToken cancellationToken = default);
}

public interface IDeveloperService
{
    /// <summary>The quick checks shown on the dashboard.</summary>
    Task<IReadOnlyList<HealthCheck>> GetChecksAsync(CancellationToken cancellationToken = default);

    Task<DeveloperReport> GetReportAsync(CancellationToken cancellationToken = default);
}

public interface IPerformanceService
{
    /// <summary>Totals of the last 1, 7 or 30 days; any other value falls back to 1.</summary>
    Task<PerformanceReport> GetReportAsync(int days, CancellationToken cancellationToken = default);
}

public interface IDeveloperTicketService
{
    Task<PagedResult<TicketListItem>> GetListAsync(TicketViewer viewer, TicketStatus? status, int page, CancellationToken cancellationToken = default);
    Task<TicketDetail?> GetAsync(TicketViewer viewer, int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateAsync(TicketViewer viewer, string userName, TicketCreateModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(TicketUpdateModel model, CancellationToken cancellationToken = default);
    Task<QuoteAttachment?> GetAttachmentAsync(TicketViewer viewer, int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Shown in the menu on every admin page, so it returns 0 instead of failing when the database is down.</summary>
    Task<int> CountNewAsync(CancellationToken cancellationToken = default);
}
