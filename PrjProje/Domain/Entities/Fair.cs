using PrjProje.Domain.Common;

namespace PrjProje.Domain.Entities;

/// <summary>A trade fair; the QR code at the stand opens its public page, which collects e-mails and shows the brochure.</summary>
public class Fair : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? TitleTr { get; set; }
    public string? TitleEn { get; set; }
    public string? IntroTr { get; set; }
    public string? IntroEn { get; set; }
    public string BrochurePdf { get; set; } = string.Empty;
    public bool EmailRequired { get; set; } = true;
    public bool ShowMarketingConsent { get; set; } = true;

    /// <summary>One "Label | URL" pair per line, shown after the site-wide social accounts.</summary>
    public string? ExtraLinks { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<FairLead> Leads { get; set; } = new List<FairLead>();
}

public class FairLead : BaseEntity
{
    public int FairId { get; set; }
    public Fair Fair { get; set; } = null!;
    public string Email { get; set; } = string.Empty;
    public bool MarketingConsent { get; set; }
    public DateTime? MarketingConsentDate { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
}
