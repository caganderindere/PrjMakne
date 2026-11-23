using PrjProje.Domain.Common;
using PrjProje.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace PrjProje.Domain.Entities;

public class Document : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DocumentType DocumentType { get; set; }

    /// <summary>Cover shown on the product catalog page; only used by catalogs.</summary>
    public string? CoverImagePath { get; set; }

    /// <summary>Null means the document is listed for every language.</summary>
    public string? LanguageCode { get; set; }
    public DateTime DocumentDate { get; set; } = DateTime.UtcNow.Date;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ContactMessage : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool PrivacyConsent { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public bool IsRead { get; set; }
}

/// <summary>Clicks on the floating WhatsApp button, one row per day (each visitor counted once a day).</summary>
public class WhatsAppClickDay
{
    public DateOnly Day { get; set; }
    public int Count { get; set; }
}

public class QuoteRequest : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
    public string? ProductName { get; set; }
    public string? Quantity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? AttachmentPath { get; set; }
    public string? AttachmentOriginalName { get; set; }
    public long? AttachmentSize { get; set; }
    public QuoteStatus Status { get; set; } = QuoteStatus.New;
    public string? AdminNote { get; set; }
    public bool PrivacyConsent { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
}

public class SiteSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
}

public class ActivityLog
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>Set when an admin creates the account or resets its password: the user must pick their own on the next sign-in.</summary>
    public bool MustChangePassword { get; set; }
}
