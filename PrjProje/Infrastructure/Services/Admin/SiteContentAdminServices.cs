using System.Collections;
using System.Globalization;
using System.Resources;
using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models.Admin;
using PrjProje.Domain.Entities;
using PrjProje.Domain.Enums;
using PrjProje.Infrastructure.Data;
using PrjProje.Infrastructure.Services.Support;
using Microsoft.EntityFrameworkCore;

namespace PrjProje.Infrastructure.Services.Admin;

public sealed class ReferenceAdminService : IReferenceAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IActivityLogger _activityLogger;
    private readonly IContentCache _cache;

    public ReferenceAdminService(ApplicationDbContext db, IFileStorageService storage, IActivityLogger activityLogger, IContentCache cache)
    {
        _db = db;
        _storage = storage;
        _activityLogger = activityLogger;
        _cache = cache;
    }

    public async Task<IReadOnlyList<ReferenceAdminItem>> GetListAsync(CancellationToken cancellationToken = default) =>
        await _db.References.AsNoTracking()
            .OrderBy(r => r.SortOrder).ThenBy(r => r.Name)
            .Select(r => new ReferenceAdminItem(r.Id, r.Name, r.Logo, r.WebsiteUrl, r.SortOrder, r.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<ReferenceEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var reference = await _db.References.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        return reference is null ? null : new ReferenceEditModel
        {
            Id = reference.Id,
            Name = reference.Name,
            WebsiteUrl = reference.WebsiteUrl,
            SortOrder = reference.SortOrder,
            IsActive = reference.IsActive,
            Logo = reference.Logo
        };
    }

    public async Task<ServiceResult<int>> SaveAsync(ReferenceEditModel model, CancellationToken cancellationToken = default)
    {
        Reference? reference;
        var isNew = model.Id == 0;
        if (isNew)
        {
            if (model.LogoFile is not { Length: > 0 })
            {
                return ServiceResult<int>.Failure("Lütfen bir logo seçin.");
            }

            reference = new Reference();
            _db.References.Add(reference);
        }
        else
        {
            reference = await _db.References.FirstOrDefaultAsync(r => r.Id == model.Id, cancellationToken);
            if (reference is null)
            {
                return ServiceResult<int>.Failure("Referans bulunamadı.");
            }
        }

        if (model.LogoFile is { Length: > 0 })
        {
            var logo = await AdminHelpers.ReplaceImageAsync(_storage, reference.Logo, model.LogoFile, false, ImageFolder.References, cancellationToken);
            if (!logo.Succeeded)
            {
                return ServiceResult<int>.Failure(logo.Errors.ToArray());
            }

            reference.Logo = logo.Value!;
        }

        reference.Name = model.Name.Trim();
        reference.WebsiteUrl = AdminHelpers.Clean(model.WebsiteUrl);
        reference.SortOrder = model.SortOrder;
        reference.IsActive = model.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
        await _activityLogger.LogAsync(isNew ? "Created" : "Updated", "Reference", reference.Id, reference.Name, cancellationToken);
        return ServiceResult<int>.Success(reference.Id);
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var reference = await _db.References.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (reference is null)
        {
            return ServiceResult.Failure("Referans bulunamadı.");
        }

        _db.References.Remove(reference);
        await _db.SaveChangesAsync(cancellationToken);
        _storage.DeletePublicFile(reference.Logo);
        _cache.Invalidate();
        await _activityLogger.LogAsync("Deleted", "Reference", id, reference.Name, cancellationToken);
        return ServiceResult.Success();
    }
}

public sealed class AnnouncementAdminService : IAnnouncementAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IActivityLogger _activityLogger;
    private readonly IContentCache _cache;

    public AnnouncementAdminService(ApplicationDbContext db, IFileStorageService storage, IActivityLogger activityLogger, IContentCache cache)
    {
        _db = db;
        _storage = storage;
        _activityLogger = activityLogger;
        _cache = cache;
    }

    public async Task<IReadOnlyList<AnnouncementAdminItem>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var items = await _db.Announcements.AsNoTracking()
            .OrderByDescending(a => a.StartDate)
            .Select(a => new AnnouncementAdminItem(a.Id, a.DisplayType, a.MessageTr, a.StartDate, a.EndDate, a.IsActive))
            .ToListAsync(cancellationToken);

        return items
            .Select(a => a with { StartDate = AdminHelpers.UtcToLocal(a.StartDate)!.Value, EndDate = AdminHelpers.UtcToLocal(a.EndDate) })
            .ToList();
    }

    public async Task<AnnouncementEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var announcement = await _db.Announcements.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        return announcement is null ? null : new AnnouncementEditModel
        {
            Id = announcement.Id,
            DisplayType = announcement.DisplayType,
            MessageTr = announcement.MessageTr,
            MessageEn = announcement.MessageEn,
            LinkUrl = announcement.LinkUrl,
            LinkTextTr = announcement.LinkTextTr,
            LinkTextEn = announcement.LinkTextEn,
            StartDate = AdminHelpers.UtcToLocal(announcement.StartDate)!.Value,
            EndDate = AdminHelpers.UtcToLocal(announcement.EndDate),
            IsActive = announcement.IsActive,
            Image = announcement.Image
        };
    }

    public async Task<ServiceResult<int>> SaveAsync(AnnouncementEditModel model, CancellationToken cancellationToken = default)
    {
        if (model.EndDate is not null && model.EndDate <= model.StartDate)
        {
            return ServiceResult<int>.Failure("Bitiş tarihi başlangıç tarihinden sonra olmalıdır.");
        }

        var linkUrl = AdminHelpers.Clean(model.LinkUrl);
        if (linkUrl is not null && !linkUrl.StartsWith('/') && !Uri.TryCreate(linkUrl, UriKind.Absolute, out _))
        {
            return ServiceResult<int>.Failure("Bağlantı adresi https://... ile ya da sitedeki bir sayfa için / ile başlamalıdır.");
        }

        Announcement? announcement;
        var isNew = model.Id == 0;
        if (isNew)
        {
            announcement = new Announcement();
            _db.Announcements.Add(announcement);
        }
        else
        {
            announcement = await _db.Announcements.FirstOrDefaultAsync(a => a.Id == model.Id, cancellationToken);
            if (announcement is null)
            {
                return ServiceResult<int>.Failure("Duyuru bulunamadı.");
            }
        }

        var image = await AdminHelpers.ReplaceImageAsync(_storage, announcement.Image, model.ImageFile, model.RemoveImage, ImageFolder.Announcements, cancellationToken);
        if (!image.Succeeded)
        {
            return ServiceResult<int>.Failure(image.Errors.ToArray());
        }

        announcement.Image = image.Value;
        announcement.DisplayType = model.DisplayType;
        announcement.MessageTr = model.MessageTr.Trim();
        announcement.MessageEn = model.MessageEn.Trim();
        announcement.LinkUrl = linkUrl;
        announcement.LinkTextTr = AdminHelpers.Clean(model.LinkTextTr);
        announcement.LinkTextEn = AdminHelpers.Clean(model.LinkTextEn);
        announcement.StartDate = AdminHelpers.LocalToUtc(model.StartDate)!.Value;
        announcement.EndDate = AdminHelpers.LocalToUtc(model.EndDate);
        announcement.IsActive = model.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
        await _activityLogger.LogAsync(isNew ? "Created" : "Updated", "Announcement", announcement.Id, announcement.MessageTr, cancellationToken);
        return ServiceResult<int>.Success(announcement.Id);
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var announcement = await _db.Announcements.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (announcement is null)
        {
            return ServiceResult.Failure("Duyuru bulunamadı.");
        }

        _db.Announcements.Remove(announcement);
        await _db.SaveChangesAsync(cancellationToken);
        _storage.DeletePublicFile(announcement.Image);
        _cache.Invalidate();
        await _activityLogger.LogAsync("Deleted", "Announcement", id, announcement.MessageTr, cancellationToken);
        return ServiceResult.Success();
    }
}

public sealed class ContentTextAdminService : IContentTextAdminService
{
    // Built-in texts live in Resources/SharedResource.{lang}.resx (see SharedResource.cs for the name).
    private static readonly ResourceManager Resources = new("PrjProje.Resources.SharedResource", typeof(SharedResource).Assembly);

    private static readonly IReadOnlyList<ContentTextGroup> AllGroups = new[]
    {
        new ContentTextGroup("home", "Ana Sayfa", "Hero başlığı ve sloganı ve ana sayfa bölüm metinleri.", new[] { "Home." }),
        new ContentTextGroup("about", "Kurumsal", "Kurumsal (Hakkımızda) sayfası: tarihçe, vizyon, misyon, tesis.", new[] { "About." }),
        new ContentTextGroup("certificates", "Belgeler", "Belgeler ve sertifikalar sayfası metinleri.", new[] { "Certificates." }),
        new ContentTextGroup("services", "Hizmetler", "Hizmetler sayfasının sabit metinleri.", new[] { "Services." }),
        new ContentTextGroup("general", "Genel", "Menü, alt bilgi, teklif çağrısı (CTA), çerez bandı ve ortak buton metinleri.", new[] { "Nav.", "Footer.", "Cta.", "Common.", "Cookie." }),
        new ContentTextGroup("pages", "Diğer Sayfalar", "Ürünler, kılavuzlar, videolar, blog, iletişim ve teklif sayfalarının metinleri.", new[] { "Products.", "Manuals.", "Blog.", "Videos.", "Contact.", "Quote." }),
        new ContentTextGroup("legal", "Yasal Metinler", "KVKK / gizlilik ve çerez politikası metinleri.", new[] { "Legal." })
    };

    private readonly ApplicationDbContext _db;
    private readonly IContentTextStore _store;
    private readonly IActivityLogger _activityLogger;

    public ContentTextAdminService(ApplicationDbContext db, IContentTextStore store, IActivityLogger activityLogger)
    {
        _db = db;
        _store = store;
        _activityLogger = activityLogger;
    }

    public IReadOnlyList<ContentTextGroup> Groups => AllGroups;

    public async Task<ContentTextPageModel> GetPageAsync(string? groupId, string? search, CancellationToken cancellationToken = default)
    {
        var group = AllGroups.FirstOrDefault(g => g.Id == groupId) ?? AllGroups[0];
        var defaultsTr = ReadDefaults(Languages.Turkish);
        var defaultsEn = ReadDefaults(Languages.English);

        IEnumerable<string> keys = defaultsTr.Keys.Union(defaultsEn.Keys);
        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            // A search runs across every group, matching the key or either language's text.
            keys = keys.Where(k => AllGroups.Any(g => g.KeyPrefixes.Any(p => k.StartsWith(p, StringComparison.Ordinal))) &&
                                   (k.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                                    Matches(defaultsTr, k, term) || Matches(defaultsEn, k, term)));
        }
        else
        {
            keys = keys.Where(k => group.KeyPrefixes.Any(p => k.StartsWith(p, StringComparison.Ordinal)));
        }

        var keyList = keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        var overrides = await _db.ContentTexts.AsNoTracking()
            .Where(t => keyList.Contains(t.Key))
            .ToListAsync(cancellationToken);

        string? Override(string key, string language) => overrides.FirstOrDefault(o => o.Key == key && o.LanguageCode == language)?.Value;

        return new ContentTextPageModel
        {
            Groups = AllGroups,
            Group = group,
            Search = term,
            Rows = keyList.Select(key =>
            {
                var tr = Override(key, Languages.Turkish);
                var en = Override(key, Languages.English);
                return new ContentTextRow
                {
                    Key = key,
                    DefaultTr = defaultsTr.GetValueOrDefault(key),
                    DefaultEn = defaultsEn.GetValueOrDefault(key),
                    ValueTr = tr ?? defaultsTr.GetValueOrDefault(key),
                    ValueEn = en ?? defaultsEn.GetValueOrDefault(key),
                    IsCustomized = tr is not null || en is not null
                };
            }).ToList()
        };
    }

    /// <summary>
    /// Stores a value only when it differs from the built-in text; an empty or unchanged value removes the override.
    /// Returns how many texts were changed.
    /// </summary>
    public async Task<ServiceResult<int>> SaveAsync(IReadOnlyList<ContentTextRow> rows, CancellationToken cancellationToken = default)
    {
        var defaults = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            [Languages.Turkish] = ReadDefaults(Languages.Turkish),
            [Languages.English] = ReadDefaults(Languages.English)
        };

        var keys = rows.Select(r => r.Key).Where(k => defaults.Values.Any(d => d.ContainsKey(k))).Distinct().ToList();
        var existing = await _db.ContentTexts.Where(t => keys.Contains(t.Key)).ToListAsync(cancellationToken);
        var changes = 0;

        foreach (var row in rows.Where(r => keys.Contains(r.Key)))
        {
            foreach (var (language, value) in new[] { (Languages.Turkish, row.ValueTr), (Languages.English, row.ValueEn) })
            {
                var normalized = Normalize(value);
                var builtIn = Normalize(defaults[language].GetValueOrDefault(row.Key));
                var current = existing.FirstOrDefault(t => t.Key == row.Key && t.LanguageCode == language);

                if (string.IsNullOrEmpty(normalized) || normalized == builtIn)
                {
                    if (current is not null)
                    {
                        _db.ContentTexts.Remove(current);
                        changes++;
                    }
                }
                else if (current is null)
                {
                    _db.ContentTexts.Add(new ContentText { Key = row.Key, LanguageCode = language, Value = normalized });
                    changes++;
                }
                else if (current.Value != normalized)
                {
                    current.Value = normalized;
                    current.UpdatedDate = DateTime.UtcNow;
                    changes++;
                }
            }
        }

        if (changes > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _store.Invalidate();
            await _activityLogger.LogAsync("Updated", "ContentText", null, $"{changes} metin", cancellationToken);
        }

        return ServiceResult<int>.Success(changes);
    }

    private static bool Matches(IReadOnlyDictionary<string, string> texts, string key, string term) =>
        texts.TryGetValue(key, out var text) && text.Contains(term, StringComparison.OrdinalIgnoreCase);

    // Browsers submit textarea line breaks as \r\n; compare and store them as \n.
    private static string Normalize(string? value) => (value ?? string.Empty).Replace("\r\n", "\n").Trim();

    private static IReadOnlyDictionary<string, string> ReadDefaults(string languageCode)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var set = Resources.GetResourceSet(new CultureInfo(languageCode), createIfNotExists: true, tryParents: false);
        if (set is null)
        {
            return result;
        }

        foreach (DictionaryEntry entry in set)
        {
            if (entry.Key is string key && entry.Value is string value)
            {
                result[key] = value;
            }
        }

        return result;
    }
}
