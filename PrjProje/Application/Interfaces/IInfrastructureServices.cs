using PrjProje.Application.Common;
using Microsoft.AspNetCore.Http;

namespace PrjProje.Application.Interfaces;

public enum ImageFolder
{
    Products,
    Categories,
    Blog,
    Gallery,
    Services,
    Site,
    References,
    Announcements,
    Catalogs
}

public record StoredFile(string RelativePath, long Size, string OriginalName);

/// <summary>Admin overrides of the built-in UI texts, read on every localized string lookup.</summary>
public interface IContentTextStore
{
    string? Find(string languageCode, string key);
    void Invalidate();
}

public interface IFileStorageService
{
    /// <summary>Validates, re-encodes to WebP (stripping metadata/payloads) and stores a public image. Returns the web path.</summary>
    Task<ServiceResult<StoredFile>> SaveImageAsync(IFormFile file, ImageFolder folder, CancellationToken cancellationToken = default);

    /// <summary>Validates and stores a public PDF document.</summary>
    Task<ServiceResult<StoredFile>> SaveDocumentAsync(IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>Stores a customer technical file outside the web root; it is only reachable through the admin panel.</summary>
    Task<ServiceResult<StoredFile>> SavePrivateAttachmentAsync(IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>Stores a job applicant's CV (PDF or Word) outside the web root, next to the private attachments.</summary>

    string? GetPrivateAttachmentPhysicalPath(string relativePath);

    void DeletePublicFile(string? webPath);

    void DeletePrivateFile(string? relativePath);
}

public interface IHtmlSanitizerService
{
    string Sanitize(string? html);
}

public interface IActivityLogger
{
    Task LogAsync(string action, string entityType, object? entityId, string? description, CancellationToken cancellationToken = default);
}

public interface IContentCache
{
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? duration = null);

    /// <summary>Evicts every cached content entry (called after any CMS change).</summary>
    void Invalidate();
}

public enum NotificationKind
{
    /// <summary>Quote requests and contact messages.</summary>
    Sales,
    HumanResources
}

public record EmailSettings(
    string? Host, int Port, bool UseSsl, string? UserName, string? Password, bool PasswordUnreadable,
    string? From, string? SalesTo, string? HrTo)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(From) && Password is not null;
}

public interface INotificationService
{
    Task<EmailSettings> GetAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> SaveAsync(Models.Admin.EmailEditModel model, CancellationToken cancellationToken = default);

    /// <summary>Sends a test mail right away; returns the problem, or null when it went out.</summary>
    Task<string?> SendTestAsync(string to, CancellationToken cancellationToken = default);

    /// <summary>Queues a notification to the addresses set for this kind; does nothing when e-mail is not set up.</summary>
    Task NotifyAsync(NotificationKind kind, string subject, string body, string? replyTo, CancellationToken cancellationToken = default);
}

public interface ISpamProtectionService
{
    string CreateFormToken();
    bool IsLikelySpam(IFormCollection form);
}

public record LegacyRedirectTarget(int Id, string ToPath, string? ToPathEn);

/// <summary>Old WordPress addresses, held in memory; used on every request that is not a page of the new site.</summary>
public interface ILegacyRedirectLookup
{
    /// <param name="path">A key produced by LegacyPath.Normalize.</param>
    Task<LegacyRedirectTarget?> FindAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Counts a redirect without delaying the visitor.</summary>
    void RecordHit(int id);
}
