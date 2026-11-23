using PrjProje.Domain.Common;

namespace PrjProje.Domain.Entities;

/// <summary>A team member's digital business card; its public page is what the printed QR code opens.</summary>
public class BusinessCard : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? TitleTr { get; set; }
    public string? TitleEn { get; set; }
    public string? DepartmentTr { get; set; }
    public string? DepartmentEn { get; set; }
    public string? NoteTr { get; set; }
    public string? NoteEn { get; set; }
    public string? Mobile { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? LinkedIn { get; set; }
    public string? Instagram { get; set; }
    public string? Website { get; set; }

    /// <summary>One "Label | URL" pair per line (catalog PDF, YouTube, calendar link...).</summary>
    public string? ExtraLinks { get; set; }

    public string? Photo { get; set; }
    public int SortOrder { get; set; }
    public int Views { get; set; }
    public bool IsActive { get; set; } = true;
}
