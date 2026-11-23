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
