using PrjProje.Application.Common;
using PrjProje.Application.Models.Admin;
using PrjProje.Domain.Enums;

namespace PrjProje.Areas.Admin.Infrastructure;

/// <summary>Turkish display labels and badge styles used across admin views.</summary>
public static class AdminLabels
{
    public static string Status(QuoteStatus status) => status switch
    {
        QuoteStatus.New => "Yeni",
        QuoteStatus.InReview => "İnceleniyor",
        QuoteStatus.QuoteSent => "Teklif Gönderildi",
        QuoteStatus.Completed => "Tamamlandı",
        _ => status.ToString()
    };

    public static string StatusBadge(QuoteStatus status) => status switch
    {
        QuoteStatus.New => "bg-danger",
        QuoteStatus.InReview => "bg-warning text-dark",
        QuoteStatus.QuoteSent => "bg-info text-dark",
        QuoteStatus.Completed => "bg-success",
        _ => "bg-secondary"
    };

    public static string Kind(TicketType type) => type == TicketType.Bug ? "Arıza" : "İstek";

    public static string KindIcon(TicketType type) => type == TicketType.Bug ? "fa-bug" : "fa-lightbulb";

    public static string Priority(TicketPriority priority) => priority switch
    {
        TicketPriority.Low => "Düşük",
        TicketPriority.Urgent => "Acil",
        _ => "Normal"
    };

    public static string PriorityBadge(TicketPriority priority) => priority switch
    {
        TicketPriority.Low => "bg-secondary",
        TicketPriority.Urgent => "bg-danger",
        _ => "bg-info text-dark"
    };

    public static string Status(TicketStatus status) => status switch
    {
        TicketStatus.New => "Yeni",
        TicketStatus.InReview => "İnceleniyor",
        TicketStatus.Resolved => "Çözüldü",
        TicketStatus.Rejected => "Reddedildi",
        _ => status.ToString()
    };

    public static string StatusBadge(TicketStatus status) => status switch
    {
        TicketStatus.New => "bg-danger",
        TicketStatus.InReview => "bg-warning text-dark",
        TicketStatus.Resolved => "bg-success",
        _ => "bg-secondary"
    };

    public static string Health(HealthState state) => state switch
    {
        HealthState.Ok => "Her şey yolunda",
        HealthState.Warning => "Dikkat gereken konular var",
        _ => "Sorun var"
    };

    public static string HealthClass(HealthState state) => state switch
    {
        HealthState.Ok => "is-ok",
        HealthState.Warning => "is-warning",
        _ => "is-error"
    };

    public static string Publish(PublishStatus status) => status == PublishStatus.Published ? "Yayında" : "Taslak";

    public static string Document(DocumentType type) => type switch
    {
        DocumentType.Certificate => "Sertifika",
        DocumentType.QualityDocument => "Kalite Belgesi",
        DocumentType.Standard => "Standart",
        DocumentType.Authorization => "Yetki Belgesi",
        DocumentType.Catalog => "Ürün Kataloğu",
        _ => type.ToString()
    };

    public static string Language(string? code) => code is null ? "Tüm diller" : Languages.DisplayName(code);

    public static string Action(string action) => action switch
    {
        "Created" => "oluşturdu",
        "Updated" => "güncelledi",
        "Deleted" => "sildi",
        "Received" => "yeni kayıt",
        "Uploaded" => "yükledi",
        "Published" => "yayınladı",
        "Unpublished" => "taslağa aldı",
        "Activated" => "aktifleştirdi",
        "Deactivated" => "pasifleştirdi",
        "StatusChanged" => "durum değiştirdi",
        "Reordered" => "sıraladı",
        "Login" => "giriş yaptı",
        "PasswordChanged" => "şifre değiştirdi",
        "PasswordReset" => "geçici şifre verdi",
        "ResumeDownloaded" => "CV indirdi",
        "Viewed" => "görüntüledi",
        _ => action
    };

    public static string Entity(string entity) => entity switch
    {
        "Product" => "Ürün",
        "ProductCategory" => "Ürün kategorisi",
        "BlogPost" => "Blog yazısı",
        "BlogCategory" => "Blog kategorisi",
        "Gallery" => "Galeri",
        "GalleryCategory" => "Galeri kategorisi",
        "Document" => "Belge",
        "Service" => "Hizmet",
        "ContactMessage" => "İletişim mesajı",
        "QuoteRequest" => "Teklif talebi",
        "User" => "Kullanıcı",
        "SiteSettings" => "Site ayarları",
        "Reference" => "Referans",
        "LegacyRedirect" => "Yönlendirme",
        "Announcement" => "Duyuru",
        "ContentText" => "Sayfa içerikleri",
        _ => entity
    };

}
