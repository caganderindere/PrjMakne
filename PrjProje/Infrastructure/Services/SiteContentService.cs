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
