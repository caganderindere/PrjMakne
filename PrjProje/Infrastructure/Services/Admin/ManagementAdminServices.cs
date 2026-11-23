using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models.Admin;
using PrjProje.Domain.Entities;
using PrjProje.Domain.Enums;
using PrjProje.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace PrjProje.Infrastructure.Services.Admin;

public sealed class ContactAdminService : IContactAdminService
{
    private const int PageSize = 20;
    private readonly ApplicationDbContext _db;
    private readonly IActivityLogger _activityLogger;

    public ContactAdminService(ApplicationDbContext db, IActivityLogger activityLogger)
    {
        _db = db;
        _activityLogger = activityLogger;
    }

    public async Task<PagedResult<ContactMessageDto>> GetListAsync(bool? unreadOnly, int page, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        var query = _db.ContactMessages.AsNoTracking();
        if (unreadOnly == true)
        {
            query = query.Where(m => !m.IsRead);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(m => m.CreatedDate)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(m => new ContactMessageDto(m.Id, m.FullName, m.Company, m.Email, m.Phone, m.Subject, m.Message, m.LanguageCode, m.IpAddress, m.IsRead, m.CreatedDate))
            .ToListAsync(cancellationToken);

        return new PagedResult<ContactMessageDto>(items, total, page, PageSize);
    }

    public async Task<ContactMessageDto?> GetAndMarkReadAsync(int id, CancellationToken cancellationToken = default)
    {
        var message = await _db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null)
        {
            return null;
        }

        if (!message.IsRead)
        {
            message.IsRead = true;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new ContactMessageDto(message.Id, message.FullName, message.Company, message.Email, message.Phone, message.Subject,
            message.Message, message.LanguageCode, message.IpAddress, message.IsRead, message.CreatedDate);
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var message = await _db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null)
        {
            return ServiceResult.Failure("Mesaj bulunamadı.");
        }

        _db.ContactMessages.Remove(message);
        await _db.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync("Deleted", "ContactMessage", id, message.FullName, cancellationToken);
        return ServiceResult.Success();
    }
}

public sealed class JobApplicationAdminService : IJobApplicationAdminService
{
    private const int PageSize = 20;
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IActivityLogger _activityLogger;

    public JobApplicationAdminService(ApplicationDbContext db, IFileStorageService storage, IActivityLogger activityLogger)
    {
        _db = db;
        _storage = storage;
        _activityLogger = activityLogger;
    }

    public async Task<PagedResult<JobApplicationDto>> GetListAsync(bool? unreadOnly, int page, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        var query = _db.JobApplications.AsNoTracking();
        if (unreadOnly == true)
        {
            query = query.Where(a => !a.IsRead);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.CreatedDate)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(a => new JobApplicationDto(a.Id, a.FullName, a.Email, a.Phone, a.City, a.Position, a.Message,
                a.ResumeOriginalName, a.ResumeSize, a.LanguageCode, a.IpAddress, a.IsRead, a.CreatedDate))
            .ToListAsync(cancellationToken);

        return new PagedResult<JobApplicationDto>(items, total, page, PageSize);
    }

    public async Task<JobApplicationDto?> GetAndMarkReadAsync(int id, CancellationToken cancellationToken = default)
    {
        var a = await _db.JobApplications.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (a is null)
        {
            return null;
        }

        if (!a.IsRead)
        {
            a.IsRead = true;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new JobApplicationDto(a.Id, a.FullName, a.Email, a.Phone, a.City, a.Position, a.Message,
            a.ResumeOriginalName, a.ResumeSize, a.LanguageCode, a.IpAddress, a.IsRead, a.CreatedDate);
    }

    public async Task<QuoteAttachment?> GetResumeAsync(int id, CancellationToken cancellationToken = default)
    {
        var a = await _db.JobApplications.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.ResumePath, x.ResumeOriginalName })
            .FirstOrDefaultAsync(cancellationToken);

        var physicalPath = a is null ? null : _storage.GetPrivateAttachmentPhysicalPath(a.ResumePath);
        return physicalPath is null ? null : new QuoteAttachment(physicalPath, a!.ResumeOriginalName);
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var a = await _db.JobApplications.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (a is null)
        {
            return ServiceResult.Failure("Başvuru bulunamadı.");
        }

        _db.JobApplications.Remove(a);
        await _db.SaveChangesAsync(cancellationToken);
        // The CV is personal data: it goes with the application.
        _storage.DeletePrivateFile(a.ResumePath);
        await _activityLogger.LogAsync("Deleted", "JobApplication", id, a.FullName, cancellationToken);
        return ServiceResult.Success();
    }
}

public sealed class QuoteAdminService : IQuoteAdminService
{
    private const int PageSize = 20;
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IActivityLogger _activityLogger;

    public QuoteAdminService(ApplicationDbContext db, IFileStorageService storage, IActivityLogger activityLogger)
    {
        _db = db;
        _storage = storage;
        _activityLogger = activityLogger;
    }

    public async Task<PagedResult<QuoteRequestListItem>> GetListAsync(QuoteStatus? status, int page, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        var query = _db.QuoteRequests.AsNoTracking();
        if (status.HasValue)
        {
            query = query.Where(q => q.Status == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(q => q.CreatedDate)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(q => new QuoteRequestListItem(q.Id, q.FullName, q.CompanyName, q.Email, q.ProductName, q.Status, q.AttachmentPath != null, q.CreatedDate))
            .ToListAsync(cancellationToken);

        return new PagedResult<QuoteRequestListItem>(items, total, page, PageSize);
    }

    public Task<QuoteRequestDto?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        _db.QuoteRequests.AsNoTracking()
            .Where(q => q.Id == id)
            .Select(q => new QuoteRequestDto(q.Id, q.FullName, q.CompanyName, q.Email, q.Phone, q.ProductId, q.ProductName, q.Quantity,
                q.Description, q.AttachmentOriginalName, q.AttachmentSize, q.Status, q.AdminNote, q.LanguageCode, q.IpAddress, q.CreatedDate, q.UpdatedDate))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ServiceResult> UpdateStatusAsync(QuoteStatusUpdateModel model, CancellationToken cancellationToken = default)
    {
        var quote = await _db.QuoteRequests.FirstOrDefaultAsync(q => q.Id == model.Id, cancellationToken);
        if (quote is null)
        {
            return ServiceResult.Failure("Teklif talebi bulunamadı.");
        }

        if (!Enum.IsDefined(model.Status))
        {
            return ServiceResult.Failure("Geçersiz durum.");
        }

        var previous = quote.Status;
        quote.Status = model.Status;
        quote.AdminNote = AdminHelpers.Clean(model.AdminNote);
        await _db.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync("StatusChanged", "QuoteRequest", quote.Id, $"{previous} → {model.Status}", cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<QuoteAttachment?> GetAttachmentAsync(int id, CancellationToken cancellationToken = default)
    {
        var quote = await _db.QuoteRequests.AsNoTracking()
            .Where(q => q.Id == id && q.AttachmentPath != null)
            .Select(q => new { q.AttachmentPath, q.AttachmentOriginalName })
            .FirstOrDefaultAsync(cancellationToken);

        var physicalPath = quote?.AttachmentPath is null ? null : _storage.GetPrivateAttachmentPhysicalPath(quote.AttachmentPath);
        return physicalPath is null ? null : new QuoteAttachment(physicalPath, quote!.AttachmentOriginalName ?? Path.GetFileName(physicalPath));
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var quote = await _db.QuoteRequests.FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quote is null)
        {
            return ServiceResult.Failure("Teklif talebi bulunamadı.");
        }

        _db.QuoteRequests.Remove(quote);
        await _db.SaveChangesAsync(cancellationToken);
        _storage.DeletePrivateFile(quote.AttachmentPath);
        await _activityLogger.LogAsync("Deleted", "QuoteRequest", id, quote.CompanyName, cancellationToken);
        return ServiceResult.Success();
    }
}

public sealed class UserAdminService : IUserAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogger _activityLogger;

    public UserAdminService(UserManager<ApplicationUser> userManager, IActivityLogger activityLogger)
    {
        _userManager = userManager;
        _activityLogger = activityLogger;
    }

    public async Task<IReadOnlyList<UserListItem>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users.AsNoTracking().OrderBy(u => u.FullName).ToListAsync(cancellationToken);
        var membersByRole = new Dictionary<string, HashSet<string>>();
        foreach (var role in Roles.All)
        {
            membersByRole[role] = (await _userManager.GetUsersInRoleAsync(role)).Select(u => u.Id).ToHashSet();
        }

        string RoleOf(string id) => Roles.Primary(role => membersByRole[role].Contains(id));

        return users
            .Select(u => new UserListItem(u.Id, u.FullName, u.Email ?? "-", RoleOf(u.Id), u.IsActive, u.CreatedDate))
            .ToList();
    }

    public async Task<UserEditModel?> GetForEditAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        return new UserEditModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = Roles.Primary(roles.Contains),
            IsActive = user.IsActive
        };
    }

    public async Task<ServiceResult> CreateAsync(UserEditModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            return ServiceResult.Failure("Yeni kullanıcı için şifre zorunludur.");
        }

        if (!Roles.All.Contains(model.Role))
        {
            return ServiceResult.Failure("Geçersiz rol.");
        }

        if (await _userManager.FindByEmailAsync(model.Email) is not null)
        {
            return ServiceResult.Failure("Bu e-posta adresiyle kayıtlı bir kullanıcı zaten var.");
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            EmailConfirmed = true,
            FullName = model.FullName.Trim(),
            IsActive = model.IsActive,
            // The admin chose this password, so the user replaces it on the first sign-in.
            MustChangePassword = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            return ServiceResult.Failure(result.Errors.Select(e => e.Description).ToArray());
        }

        await _userManager.AddToRoleAsync(user, model.Role);
        await _activityLogger.LogAsync("Created", "User", user.Id, $"{user.Email} ({model.Role})");
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateAsync(UserEditModel model, string currentUserId)
    {
        var user = model.Id is null ? null : await _userManager.FindByIdAsync(model.Id);
        if (user is null)
        {
            return ServiceResult.Failure("Kullanıcı bulunamadı.");
        }

        if (!Roles.All.Contains(model.Role))
        {
            return ServiceResult.Failure("Geçersiz rol.");
        }

        var isSelf = user.Id == currentUserId;
        if (isSelf && (!model.IsActive || model.Role != Roles.Admin))
        {
            return ServiceResult.Failure("Kendi hesabınızı pasifleştiremez veya yönetici rolünüzü kaldıramazsınız.");
        }

        var existingWithEmail = await _userManager.FindByEmailAsync(model.Email);
        if (existingWithEmail is not null && existingWithEmail.Id != user.Id)
        {
            return ServiceResult.Failure("Bu e-posta adresi başka bir kullanıcıya ait.");
        }

        user.FullName = model.FullName.Trim();
        user.Email = model.Email.Trim();
        user.UserName = model.Email.Trim();
        user.IsActive = model.IsActive;

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return ServiceResult.Failure(update.Errors.Select(e => e.Description).ToArray());
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.SequenceEqual(new[] { model.Role }))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Role);
        }

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var reset = await _userManager.ResetPasswordAsync(user, token, model.Password);
            if (!reset.Succeeded)
            {
                return ServiceResult.Failure(reset.Errors.Select(e => e.Description).ToArray());
            }

            // A password reset for someone else is temporary: they pick their own on the next sign-in, and a lockout
            // from the forgotten password is lifted.
            if (!isSelf)
            {
                user.MustChangePassword = true;
                await _userManager.UpdateAsync(user);
            }

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
            await _activityLogger.LogAsync("PasswordReset", "User", user.Id, user.Email);
        }

        // Invalidate existing cookies so role/status changes take effect immediately.
        await _userManager.UpdateSecurityStampAsync(user);
        await _activityLogger.LogAsync("Updated", "User", user.Id, $"{user.Email} ({model.Role})");
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(string id, string currentUserId)
    {
        if (id == currentUserId)
        {
            return ServiceResult.Failure("Kendi hesabınızı silemezsiniz.");
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return ServiceResult.Failure("Kullanıcı bulunamadı.");
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return ServiceResult.Failure(result.Errors.Select(e => e.Description).ToArray());
        }

        await _activityLogger.LogAsync("Deleted", "User", id, user.Email);
        return ServiceResult.Success();
    }
}

public sealed class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;

    public DashboardService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthAgo = today.AddDays(-29);
        var whatsApp = await _db.WhatsAppClickDays.AsNoTracking().Where(d => d.Day >= monthAgo).ToListAsync(cancellationToken);
        return new DashboardDto
        {
            WhatsAppToday = whatsApp.Where(d => d.Day == today).Sum(d => d.Count),
            WhatsAppLast30Days = whatsApp.Sum(d => d.Count),
            TotalProducts = await _db.Products.CountAsync(cancellationToken),
            ActiveProducts = await _db.Products.CountAsync(p => p.IsActive, cancellationToken),
            BlogPosts = await _db.BlogPosts.CountAsync(cancellationToken),
            PublishedTranslations = await _db.BlogPostTranslations.CountAsync(t => t.Status == PublishStatus.Published, cancellationToken),
            TotalMessages = await _db.ContactMessages.CountAsync(cancellationToken),
            UnreadMessages = await _db.ContactMessages.CountAsync(m => !m.IsRead, cancellationToken),
            TotalQuotes = await _db.QuoteRequests.CountAsync(cancellationToken),
            NewQuotes = await _db.QuoteRequests.CountAsync(q => q.Status == QuoteStatus.New, cancellationToken),
            GalleryItems = await _db.GalleryItems.CountAsync(cancellationToken),
            Documents = await _db.Documents.CountAsync(cancellationToken),
            RecentActivities = await _db.ActivityLogs.AsNoTracking()
                .OrderByDescending(a => a.CreatedDate)
                .Take(5)
                .Select(a => new ActivityLogDto(a.UserName, a.Action, a.EntityType, a.EntityId, a.Description, a.CreatedDate))
                .ToListAsync(cancellationToken),
            RecentQuotes = await _db.QuoteRequests.AsNoTracking()
                .OrderByDescending(q => q.CreatedDate)
                .Take(5)
                .Select(q => new QuoteRequestListItem(q.Id, q.FullName, q.CompanyName, q.Email, q.ProductName, q.Status, q.AttachmentPath != null, q.CreatedDate))
                .ToListAsync(cancellationToken),
            RecentMessages = await _db.ContactMessages.AsNoTracking()
                .OrderByDescending(m => m.CreatedDate)
                .Take(5)
                .Select(m => new ContactMessageDto(m.Id, m.FullName, m.Company, m.Email, m.Phone, m.Subject, m.Message, m.LanguageCode, m.IpAddress, m.IsRead, m.CreatedDate))
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<MarketingSummaryDto> GetMarketingAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthAgo = today.AddDays(-29);
        var since = DateTime.UtcNow.AddDays(-30);
        var whatsApp = await _db.WhatsAppClickDays.AsNoTracking().Where(d => d.Day >= monthAgo).ToListAsync(cancellationToken);
        return new MarketingSummaryDto
        {
            QuotesLast30Days = await _db.QuoteRequests.CountAsync(q => q.CreatedDate >= since, cancellationToken),
            NewQuotes = await _db.QuoteRequests.CountAsync(q => q.Status == QuoteStatus.New, cancellationToken),
            MessagesLast30Days = await _db.ContactMessages.CountAsync(m => m.CreatedDate >= since, cancellationToken),
            UnreadMessages = await _db.ContactMessages.CountAsync(m => !m.IsRead, cancellationToken),
            UnreadApplications = await _db.JobApplications.CountAsync(a => !a.IsRead, cancellationToken),
            WhatsAppToday = whatsApp.Where(d => d.Day == today).Sum(d => d.Count),
            WhatsAppLast30Days = whatsApp.Sum(d => d.Count),
            FairLeadsLast30Days = await _db.FairLeads.CountAsync(l => l.CreatedDate >= since, cancellationToken),
            FairLeadsWithConsent = await _db.FairLeads.CountAsync(l => l.CreatedDate >= since && l.MarketingConsent, cancellationToken),
            RecentQuotes = await _db.QuoteRequests.AsNoTracking()
                .OrderByDescending(q => q.CreatedDate)
                .Take(5)
                .Select(q => new QuoteRequestListItem(q.Id, q.FullName, q.CompanyName, q.Email, q.ProductName, q.Status, q.AttachmentPath != null, q.CreatedDate))
                .ToListAsync(cancellationToken),
            RecentFairLeads = await _db.FairLeads.AsNoTracking()
                .OrderByDescending(l => l.CreatedDate)
                .Take(5)
                .Select(l => new FairLeadListItem(l.FairId, l.Fair.Name, l.Email, l.MarketingConsent, l.CreatedDate))
                .ToListAsync(cancellationToken)
        };
    }
}
