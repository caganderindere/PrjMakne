using PrjProje.Domain.Common;
using PrjProje.Domain.Enums;

namespace PrjProje.Domain.Entities;

/// <summary>A customer / partner logo shown in the home page references strip.</summary>
public class Reference : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Site-wide announcement (e.g. a trade fair) shown as a top bar or a pop-up between two dates.
/// Dates are stored in UTC.
/// </summary>
public class Announcement : BaseEntity
{
    public AnnouncementDisplay DisplayType { get; set; }
    public string MessageTr { get; set; } = string.Empty;
    public string MessageEn { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public string? LinkTextTr { get; set; }
    public string? LinkTextEn { get; set; }
    public string? Image { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Admin override of a UI text from Resources/SharedResource.{lang}.resx.
/// When a row exists for a key and language it replaces the built-in text everywhere on the site.
/// </summary>
public class ContentText
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}
