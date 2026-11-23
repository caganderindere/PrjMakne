using PrjProje.Domain.Common;

namespace PrjProje.Domain.Entities;

/// <summary>
/// An address of the old WordPress site and where it now lives. <see cref="FromPath"/> is stored normalized
/// (see LegacyPath), so every spelling of the old URL finds the same row.
/// </summary>
public class LegacyRedirect : BaseEntity
{
    public string FromPath { get; set; } = string.Empty;
    public string ToPath { get; set; } = string.Empty;

    /// <summary>Target for English visitors; empty sends them to <see cref="ToPath"/>.</summary>
    public string? ToPathEn { get; set; }

    public int HitCount { get; set; }
}
