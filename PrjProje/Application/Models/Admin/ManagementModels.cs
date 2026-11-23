using System.ComponentModel.DataAnnotations;
using PrjProje.Domain.Enums;

namespace PrjProje.Application.Models.Admin;

public record ContactMessageDto(
    int Id,
    string FullName,
    string? Company,
    string Email,
    string? Phone,
    string Subject,
    string Message,
    string LanguageCode,
    string? IpAddress,
    bool IsRead,
    DateTime CreatedDate);

public record QuoteRequestListItem(
    int Id,
    string FullName,
    string CompanyName,
    string Email,
    string? ProductName,
    QuoteStatus Status,
    bool HasAttachment,
    DateTime CreatedDate);

public record QuoteRequestDto(
    int Id,
    string FullName,
    string CompanyName,
    string Email,
    string Phone,
    int? ProductId,
    string? ProductName,
    string? Quantity,
    string Description,
    string? AttachmentOriginalName,
    long? AttachmentSize,
    QuoteStatus Status,
    string? AdminNote,
    string LanguageCode,
    string? IpAddress,
    DateTime CreatedDate,
    DateTime? UpdatedDate);

public class QuoteStatusUpdateModel
{
    public int Id { get; set; }

    [Display(Name = "Durum")]
    public QuoteStatus Status { get; set; }

    [Display(Name = "Yönetici Notu")]
    [StringLength(2000, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? AdminNote { get; set; }
}

public record QuoteAttachment(string PhysicalPath, string DownloadName);

public record ActivityLogDto(string UserName, string Action, string EntityType, string? EntityId, string? Description, DateTime CreatedDate);

public class DashboardDto
{
    public int TotalProducts { get; init; }
    public int ActiveProducts { get; init; }
    public int BlogPosts { get; init; }
    public int PublishedTranslations { get; init; }
    public int TotalMessages { get; init; }
    public int UnreadMessages { get; init; }
    public int TotalQuotes { get; init; }
    public int NewQuotes { get; init; }
    public int GalleryItems { get; init; }
    public int WhatsAppToday { get; init; }
    public int WhatsAppLast30Days { get; init; }
    public int Documents { get; init; }
    public IReadOnlyList<ActivityLogDto> RecentActivities { get; init; } = Array.Empty<ActivityLogDto>();
    public IReadOnlyList<QuoteRequestListItem> RecentQuotes { get; init; } = Array.Empty<QuoteRequestListItem>();
    public IReadOnlyList<ContactMessageDto> RecentMessages { get; init; } = Array.Empty<ContactMessageDto>();
}

/// <summary>Ordered by severity, so the worst of several checks is simply their Max().</summary>
public enum HealthState
{
    Ok,
    Warning,
    Error
}

public record HealthCheck(string Name, string Icon, HealthState State, string Detail);

public record ContentIssue(string Label, int Count, string Controller);

public record MissingFile(string Source, string Path);

public record RecentLogEntry(DateTime CreatedDate, bool IsError, string Category, string Message, string? Exception);

public record RuntimeFact(string Label, string Value);

public class DeveloperReport
{
    public IReadOnlyList<HealthCheck> Checks { get; init; } = Array.Empty<HealthCheck>();
    public bool DatabaseChecked { get; init; }
    public IReadOnlyList<ContentIssue> ContentIssues { get; init; } = Array.Empty<ContentIssue>();
    public IReadOnlyList<MissingFile> MissingFiles { get; init; } = Array.Empty<MissingFile>();
    public IReadOnlyList<RecentLogEntry> Logs { get; init; } = Array.Empty<RecentLogEntry>();
    public IReadOnlyList<RuntimeFact> Runtime { get; init; } = Array.Empty<RuntimeFact>();
}

public record UserListItem(string Id, string FullName, string Email, string Role, bool IsActive, DateTime CreatedDate);

public class UserEditModel
{
    public string? Id { get; set; }

    [Display(Name = "Ad Soyad")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [StringLength(150, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "E-posta")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Rol")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    public string Role { get; set; } = "Editor";

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Şifre")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "{0} en az {2} karakter olmalıdır.")]
    public string? Password { get; set; }

    [Display(Name = "Şifre (Tekrar)")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor.")]
    public string? ConfirmPassword { get; set; }
}

public class LoginModel
{
    [Display(Name = "E-posta")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Şifre")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Beni hatırla")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class ChangePasswordModel
{
    [Display(Name = "Mevcut Şifre")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Display(Name = "Yeni Şifre")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "{0} en az {2} karakter olmalıdır.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Display(Name = "Yeni Şifre (Tekrar)")]
    [Compare(nameof(NewPassword), ErrorMessage = "Şifreler eşleşmiyor.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
