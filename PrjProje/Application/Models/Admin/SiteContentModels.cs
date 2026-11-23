using System.ComponentModel.DataAnnotations;
using PrjProje.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace PrjProje.Application.Models.Admin;

public record ReferenceAdminItem(int Id, string Name, string Logo, string? WebsiteUrl, int SortOrder, bool IsActive);

public class ReferenceEditModel
{
    public int Id { get; set; }

    [Display(Name = "Firma / Kurum Adı")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [StringLength(150, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Web Sitesi")]
    [Url(ErrorMessage = "Geçerli bir adres girin (https://...).")]
    [StringLength(300, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? WebsiteUrl { get; set; }

    [Display(Name = "Sıra")]
    public int SortOrder { get; set; }

    [Display(Name = "Yayında")]
    public bool IsActive { get; set; } = true;

    public string? Logo { get; set; }

    [Display(Name = "Logo")]
    public IFormFile? LogoFile { get; set; }
}

public record AnnouncementAdminItem(int Id, AnnouncementDisplay DisplayType, string MessageTr, DateTime StartDate, DateTime? EndDate, bool IsActive);

public class AnnouncementEditModel
{
    public int Id { get; set; }

    [Display(Name = "Gösterim Şekli")]
    public AnnouncementDisplay DisplayType { get; set; }

    [Display(Name = "Duyuru Metni (TR)")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [StringLength(500, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string MessageTr { get; set; } = string.Empty;

    [Display(Name = "Duyuru Metni (EN)")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [StringLength(500, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string MessageEn { get; set; } = string.Empty;

    [Display(Name = "Bağlantı Adresi")]
    [StringLength(300, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? LinkUrl { get; set; }

    [Display(Name = "Buton Metni (TR)")]
    [StringLength(60, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? LinkTextTr { get; set; }

    [Display(Name = "Buton Metni (EN)")]
    [StringLength(60, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? LinkTextEn { get; set; }

    [Display(Name = "Başlangıç")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [DataType(DataType.DateTime)]
    public DateTime StartDate { get; set; } = DateTime.Now.Date;

    [Display(Name = "Bitiş")]
    [DataType(DataType.DateTime)]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    public string? Image { get; set; }

    [Display(Name = "Görsel (yalnızca pop-up)")]
    public IFormFile? ImageFile { get; set; }

    public bool RemoveImage { get; set; }
}

/// <summary>A group of editable site texts (one tab on the "Sayfa İçerikleri" screen).</summary>
public record ContentTextGroup(string Id, string Title, string Description, IReadOnlyList<string> KeyPrefixes);

public class ContentTextRow
{
    public string Key { get; set; } = string.Empty;
    public string? DefaultTr { get; set; }
    public string? DefaultEn { get; set; }
    public string? ValueTr { get; set; }
    public string? ValueEn { get; set; }
    public bool IsCustomized { get; set; }
}

public class ContentTextPageModel
{
    public IReadOnlyList<ContentTextGroup> Groups { get; init; } = Array.Empty<ContentTextGroup>();
    public ContentTextGroup Group { get; init; } = null!;
    public string? Search { get; init; }
    public List<ContentTextRow> Rows { get; set; } = new();
}

public record CloudflareStatus(bool ViaCloudflare, string? ClientIp, string? Ray, bool HasSecret, bool SecretUnreadable, bool TurnstileDisabled, string? SiteKey);

public class EmailEditModel
{
    [Display(Name = "SMTP Sunucusu")]
    [StringLength(200, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Host { get; set; }

    [Display(Name = "Port")]
    [Range(1, 65535, ErrorMessage = "{0} 1 ile 65535 arasında olmalıdır.")]
    public int Port { get; set; } = 587;

    [Display(Name = "Güvenli bağlantı (STARTTLS)")]
    public bool UseSsl { get; set; } = true;

    [Display(Name = "Kullanıcı Adı")]
    [StringLength(200, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? UserName { get; set; }

    [Display(Name = "Şifre")]
    [StringLength(200, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Password { get; set; }

    [Display(Name = "Gönderen Adres")]
    [StringLength(200, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? From { get; set; }

    [Display(Name = "Teklif ve mesaj bildirimleri")]
    [StringLength(500, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? SalesTo { get; set; }

    [Display(Name = "İş başvurusu bildirimleri")]
    [StringLength(500, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? HrTo { get; set; }
}

public class CloudflareEditModel
{
    [Display(Name = "Site Cloudflare arkasında çalışıyor")]
    public bool UseProxy { get; set; }

    [Display(Name = "Site Key")]
    [StringLength(100, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? TurnstileSiteKey { get; set; }

    [Display(Name = "Secret Key")]
    [StringLength(200, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? TurnstileSecret { get; set; }

    [Display(Name = "Admin girişi")]
    public bool ProtectLogin { get; set; }

    [Display(Name = "İletişim formu")]
    public bool ProtectContact { get; set; }

    [Display(Name = "Teklif formu")]
    public bool ProtectQuote { get; set; }

    [Display(Name = "Fuar formu")]
    public bool ProtectFair { get; set; }
}

public record FairAdminItem(int Id, string Name, string Slug, int LeadCount, bool EmailRequired, bool IsActive);

public record FairLeadItem(int Id, string Email, bool MarketingConsent, DateTime? MarketingConsentDate, string LanguageCode, DateTime? CreatedDate);

public record FairLeadsModel(int FairId, string FairName, string FairSlug, IReadOnlyList<FairLeadItem> Leads);

public class FairEditModel
{
    public int Id { get; set; }

    [Display(Name = "Fuar Adı")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [StringLength(150, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Bağlantı")]
    [StringLength(160, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Slug { get; set; }

    [Display(Name = "Başlık (TR)")]
    [StringLength(150, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? TitleTr { get; set; }

    [Display(Name = "Başlık (EN)")]
    [StringLength(150, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? TitleEn { get; set; }

    [Display(Name = "Kısa Açıklama (TR)")]
    [StringLength(400, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? IntroTr { get; set; }

    [Display(Name = "Kısa Açıklama (EN)")]
    [StringLength(400, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? IntroEn { get; set; }

    [Display(Name = "E-posta zorunlu")]
    public bool EmailRequired { get; set; } = true;

    [Display(Name = "Tanıtım izni kutucuğunu göster")]
    public bool ShowMarketingConsent { get; set; } = true;

    [Display(Name = "Fuara Özel Bağlantılar")]
    public string? ExtraLinks { get; set; }

    [Display(Name = "Yayında")]
    public bool IsActive { get; set; } = true;

    public string? BrochurePdf { get; set; }

    [Display(Name = "Broşür (PDF)")]
    public IFormFile? BrochureFile { get; set; }
}

public record BusinessCardAdminItem(int Id, string FullName, string Slug, string? Title, string? Department, string? Photo, int Views, bool IsActive);

public class BusinessCardEditModel
{
    public int Id { get; set; }

    [Display(Name = "Ad")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [StringLength(60, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Soyad")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [StringLength(60, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>The card's random address token; set by the server, shown read-only.</summary>
    public string? Slug { get; set; }

    [Display(Name = "QR kodu yenile")]
    public bool RegenerateLink { get; set; }

    [Display(Name = "Unvan (TR)")]
    [StringLength(100, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? TitleTr { get; set; }

    [Display(Name = "Unvan (EN)")]
    [StringLength(100, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? TitleEn { get; set; }

    [Display(Name = "Departman (TR)")]
    [StringLength(100, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? DepartmentTr { get; set; }

    [Display(Name = "Departman (EN)")]
    [StringLength(100, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? DepartmentEn { get; set; }

    [Display(Name = "Kısa Not (TR)")]
    [StringLength(400, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? NoteTr { get; set; }

    [Display(Name = "Kısa Not (EN)")]
    [StringLength(400, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? NoteEn { get; set; }

    [Display(Name = "Cep Telefonu")]
    [StringLength(30, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Mobile { get; set; }

    [Display(Name = "Sabit Telefon")]
    [StringLength(30, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Phone { get; set; }

    [Display(Name = "WhatsApp")]
    [StringLength(30, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? WhatsApp { get; set; }

    [Display(Name = "E-posta")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [StringLength(120, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Email { get; set; }

    [Display(Name = "LinkedIn")]
    [StringLength(200, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? LinkedIn { get; set; }

    [Display(Name = "Instagram")]
    [StringLength(200, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Instagram { get; set; }

    [Display(Name = "Web Sitesi")]
    [StringLength(200, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Website { get; set; }

    [Display(Name = "Ek Bağlantılar")]
    public string? ExtraLinks { get; set; }

    [Display(Name = "Sıra")]
    public int SortOrder { get; set; }

    [Display(Name = "Yayında")]
    public bool IsActive { get; set; } = true;

    public int Views { get; set; }

    public string? Photo { get; set; }

    [Display(Name = "Fotoğraf")]
    public IFormFile? PhotoFile { get; set; }

    public bool RemovePhoto { get; set; }
}
