using System.Globalization;
using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models;
using PrjProje.Domain.Entities;
using PrjProje.Domain.Enums;
using PrjProje.Infrastructure.Data;
using PrjProje.Infrastructure.Services.Support;
using Microsoft.EntityFrameworkCore;

namespace PrjProje.Infrastructure.Services;

public sealed class BusinessCardService : IBusinessCardService
{
    private readonly ApplicationDbContext _db;

    public BusinessCardService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<BusinessCardDto?> GetAsync(string slug, string languageCode, CancellationToken cancellationToken = default)
    {
        // Name-based addresses (the old format) never open a card.
        if (!BusinessCardFiles.IsToken(slug))
        {
            return null;
        }

        var card = await _db.BusinessCards.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive, cancellationToken);
        if (card is null)
        {
            return null;
        }

        // Empty English fields fall back to the Turkish text.
        string? Text(string? tr, string? en) => languageCode == Languages.English && !string.IsNullOrWhiteSpace(en) ? en : tr;
        return new BusinessCardDto(
            card.Id, card.Slug, card.FirstName, card.LastName,
            Text(card.TitleTr, card.TitleEn), Text(card.DepartmentTr, card.DepartmentEn), Text(card.NoteTr, card.NoteEn),
            card.Mobile, card.Phone, card.WhatsApp, card.Email,
            card.LinkedIn, card.Instagram, card.Website, card.ExtraLinks, card.Photo);
    }

    public Task CountViewAsync(int id, CancellationToken cancellationToken = default) =>
        _db.BusinessCards.Where(c => c.Id == id).ExecuteUpdateAsync(s => s.SetProperty(c => c.Views, c => c.Views + 1), cancellationToken);
}

public sealed class FairService : IFairService
{
    private readonly ApplicationDbContext _db;

    public FairService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<FairDto?> GetAsync(string slug, string languageCode, CancellationToken cancellationToken = default)
    {
        var fair = await _db.Fairs.AsNoTracking().FirstOrDefaultAsync(f => f.Slug == slug && f.IsActive, cancellationToken);
        if (fair is null)
        {
            return null;
        }

        // Empty English fields fall back to the Turkish text.
        string? Text(string? tr, string? en) => languageCode == Languages.English && !string.IsNullOrWhiteSpace(en) ? en : tr;
        return new FairDto(
            fair.Id, fair.Slug, Text(fair.TitleTr, fair.TitleEn) ?? fair.Name, Text(fair.IntroTr, fair.IntroEn),
            fair.BrochurePdf, fair.EmailRequired, fair.ShowMarketingConsent, fair.ExtraLinks);
    }

    public async Task SaveLeadAsync(FairDto fair, string email, bool marketingConsent, string languageCode, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var consent = fair.ShowMarketingConsent && marketingConsent;
        var lead = await _db.FairLeads.FirstOrDefaultAsync(l => l.FairId == fair.Id && l.Email == normalized, cancellationToken);
        if (lead is null)
        {
            _db.FairLeads.Add(new FairLead
            {
                FairId = fair.Id,
                Email = normalized,
                MarketingConsent = consent,
                MarketingConsentDate = consent ? DateTime.UtcNow : null,
                LanguageCode = languageCode
            });
        }
        else if (consent && !lead.MarketingConsent)
        {
            lead.MarketingConsent = true;
            lead.MarketingConsentDate = DateTime.UtcNow;
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A double tap can insert the same e-mail twice at once; the unique index keeps one, which is the goal.
            if (!await _db.FairLeads.AsNoTracking().AnyAsync(l => l.FairId == fair.Id && l.Email == normalized, cancellationToken))
            {
                throw;
            }
        }
    }
}

public sealed class SiteContentService : ISiteContentService
{
    private readonly ApplicationDbContext _db;
    private readonly IContentCache _cache;

    public SiteContentService(ApplicationDbContext db, IContentCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<IReadOnlyList<ReferenceDto>> GetReferencesAsync(CancellationToken cancellationToken = default) =>
        _cache.GetOrCreateAsync("references", async () =>
            (IReadOnlyList<ReferenceDto>)await _db.References.AsNoTracking()
                .Where(r => r.IsActive)
                .OrderBy(r => r.SortOrder).ThenBy(r => r.Name)
                .Select(r => new ReferenceDto(r.Name, r.Logo, r.WebsiteUrl))
                .ToListAsync(cancellationToken));

    public async Task<IReadOnlyList<AnnouncementDto>> GetCurrentAnnouncementsAsync(string languageCode, CancellationToken cancellationToken = default)
    {
        // Every active announcement is cached; the date window is applied per request so one
        // starts and ends on time without waiting for the cache to expire.
        var all = await _cache.GetOrCreateAsync("announcements", async () =>
            await _db.Announcements.AsNoTracking()
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.StartDate)
                .ToListAsync(cancellationToken));

        var now = DateTime.UtcNow;
        var turkish = languageCode == Languages.Turkish;
        return all
            .Where(a => a.StartDate <= now && (a.EndDate is null || a.EndDate > now))
            .Select(a => new AnnouncementDto(
                a.Id,
                a.DisplayType,
                turkish ? a.MessageTr : a.MessageEn,
                a.LinkUrl,
                turkish ? a.LinkTextTr : a.LinkTextEn,
                a.DisplayType == AnnouncementDisplay.Popup ? a.Image : null,
                (a.UpdatedDate ?? a.CreatedDate).Ticks.ToString(CultureInfo.InvariantCulture)))
            .ToList();
    }
}
