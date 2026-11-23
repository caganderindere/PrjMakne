using PrjProje.Domain.Common;

namespace PrjProje.Domain.Entities;

/// <summary>
/// Meeting minutes typed into the panel and printed on the company letterhead. Numbered per year: TOP-2026-001.
/// Only the person who wrote them and the panel users ticked as participants can open them.
/// </summary>
public class MeetingMinute : BaseEntity
{
    public const string NumberPrefix = "TOP";

    /// <summary>The year the number was issued in; the sequence starts again at 1 every year.</summary>
    public int Year { get; set; }
    public int Sequence { get; set; }
    public string Number { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public DateTime MeetingDate { get; set; }
    public string? Location { get; set; }

    /// <summary>Free text, one entry per line: participants without a panel account, agenda items, decisions.</summary>
    public string? Participants { get; set; }
    public string? Agenda { get; set; }
    public string Decisions { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public string PreparedBy { get; set; } = string.Empty;
    public string? PreparedById { get; set; }

    public ICollection<MeetingMinuteParticipant> ParticipantUsers { get; set; } = new List<MeetingMinuteParticipant>();

    public static string FormatNumber(int year, int sequence) => $"{NumberPrefix}-{year}-{sequence:000}";
}

/// <summary>A panel user who attended. The name is a copy taken at saving, so the printed minutes keep it even if the
/// account is renamed or deleted (there is deliberately no foreign key to the user).</summary>
public class MeetingMinuteParticipant
{
    public int Id { get; set; }
    public int MeetingMinuteId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}
