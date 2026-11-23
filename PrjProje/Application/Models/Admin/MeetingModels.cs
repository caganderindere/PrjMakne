using System.ComponentModel.DataAnnotations;

namespace PrjProje.Application.Models.Admin;

public record MeetingMinuteListItem(int Id, string Number, string Title, DateTime MeetingDate, string? Location, string PreparedBy);

/// <summary>A panel user offered as a participant.</summary>
public record UserOption(string Id, string FullName, string? Email);

/// <summary>Everything the printed minutes show.</summary>
public record MeetingMinuteDto(
    int Id,
    string Number,
    string Title,
    DateTime MeetingDate,
    string? Location,
    IReadOnlyList<string> ParticipantUsers,
    string? Participants,
    string? Agenda,
    string Decisions,
    string? Notes,
    string PreparedBy,
    DateTime CreatedDate,
    DateTime? UpdatedDate)
{
    /// <summary>The free-text fields hold one entry per line; blank lines are dropped.</summary>
    public static IReadOnlyList<string> Lines(string? text) =>
        (text ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public class MeetingMinuteEditModel
{
    public int Id { get; set; }

    /// <summary>Shown on the form once the minutes are saved; never posted back.</summary>
    public string? Number { get; set; }

    [Display(Name = "Toplantı Konusu")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [StringLength(200, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Tarih ve Saat")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    public DateTime? MeetingDate { get; set; }

    [Display(Name = "Yer")]
    [StringLength(200, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Location { get; set; }

    /// <summary>Panel users who attended: they (and whoever wrote the minutes) are the only ones who can open them.</summary>
    [Display(Name = "Katılımcılar")]
    public List<string> ParticipantUserIds { get; set; } = new();

    [Display(Name = "Diğer Katılımcılar (panel hesabı olmayanlar, her satıra bir kişi)")]
    [StringLength(2000, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Participants { get; set; }

    [Display(Name = "Gündem (her satıra bir madde)")]
    [StringLength(4000, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Agenda { get; set; }

    [Display(Name = "Alınan Kararlar (her satıra bir karar)")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [StringLength(8000, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string Decisions { get; set; } = string.Empty;

    [Display(Name = "Notlar")]
    [StringLength(8000, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Notes { get; set; }
}
