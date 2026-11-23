using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models.Admin;
using PrjProje.Domain.Entities;
using PrjProje.Infrastructure.Data;
using PrjProje.Infrastructure.Services.Admin;
using PrjProje.Infrastructure.Services.Support;
using Microsoft.EntityFrameworkCore;

namespace PrjProje.Infrastructure.Services;

/// <summary>Singleton lookup: the whole table is a few hundred rows, cached as one dictionary and rebuilt after a panel edit.</summary>
public sealed class LegacyRedirectLookup : ILegacyRedirectLookup
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IContentCache _cache;
    private readonly ILogger<LegacyRedirectLookup> _logger;

    public LegacyRedirectLookup(IServiceScopeFactory scopeFactory, IContentCache cache, ILogger<LegacyRedirectLookup> logger)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<LegacyRedirectTarget?> FindAsync(string path, CancellationToken cancellationToken = default)
    {
        var table = await _cache.GetOrCreateAsync("legacy-redirects", async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return (IReadOnlyDictionary<string, LegacyRedirectTarget>)await db.LegacyRedirects.AsNoTracking()
                .ToDictionaryAsync(r => r.FromPath, r => new LegacyRedirectTarget(r.Id, r.ToPath, r.ToPathEn), StringComparer.Ordinal, cancellationToken);
        }, TimeSpan.FromHours(6));

        return table.GetValueOrDefault(path);
    }

    public void RecordHit(int id) => _ = Task.Run(async () =>
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.LegacyRedirects.Where(r => r.Id == id).ExecuteUpdateAsync(s => s.SetProperty(r => r.HitCount, r => r.HitCount + 1));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not count a hit for legacy redirect {Id}", id);
        }
    });
}

public sealed class LegacyRedirectAdminService : ILegacyRedirectAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IActivityLogger _activityLogger;
    private readonly IContentCache _cache;

    public LegacyRedirectAdminService(ApplicationDbContext db, IActivityLogger activityLogger, IContentCache cache)
    {
        _db = db;
        _activityLogger = activityLogger;
        _cache = cache;
    }

    public async Task<IReadOnlyList<LegacyRedirectAdminItem>> GetListAsync(CancellationToken cancellationToken = default) =>
        await _db.LegacyRedirects.AsNoTracking()
            .OrderBy(r => r.FromPath)
            .Select(r => new LegacyRedirectAdminItem(r.Id, r.FromPath, r.ToPath, r.ToPathEn, r.HitCount))
            .ToListAsync(cancellationToken);

    public async Task<LegacyRedirectEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var redirect = await _db.LegacyRedirects.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        return redirect is null ? null : new LegacyRedirectEditModel
        {
            Id = redirect.Id,
            FromPath = redirect.FromPath,
            ToPath = redirect.ToPath,
            ToPathEn = redirect.ToPathEn,
            HitCount = redirect.HitCount
        };
    }

    public async Task<ServiceResult<int>> SaveAsync(LegacyRedirectEditModel model, CancellationToken cancellationToken = default)
    {
        // People paste the full old link; only its path matters, stored in the same form the middleware looks up.
        var from = Uri.TryCreate(model.FromPath.Trim(), UriKind.Absolute, out var absolute) ? absolute.PathAndQuery : model.FromPath.Trim();
        var (fromPath, _) = LegacyPath.Normalize(from.StartsWith('/') ? from : "/" + from);
        if (fromPath == "/")
        {
            return ServiceResult<int>.Failure("Ana sayfa zaten /tr adresine yönlenir; eski adres boş olamaz.");
        }

        if (await _db.LegacyRedirects.AnyAsync(r => r.FromPath == fromPath && r.Id != model.Id, cancellationToken))
        {
            return ServiceResult<int>.Failure($"{fromPath} için zaten bir yönlendirme var.");
        }

        LegacyRedirect? redirect;
        var isNew = model.Id == 0;
        if (isNew)
        {
            redirect = new LegacyRedirect();
            _db.LegacyRedirects.Add(redirect);
        }
        else
        {
            redirect = await _db.LegacyRedirects.FirstOrDefaultAsync(r => r.Id == model.Id, cancellationToken);
            if (redirect is null)
            {
                return ServiceResult<int>.Failure("Yönlendirme bulunamadı.");
            }

            redirect.UpdatedDate = DateTime.UtcNow;
        }

        redirect.FromPath = fromPath;
        redirect.ToPath = model.ToPath.Trim();
        redirect.ToPathEn = AdminHelpers.Clean(model.ToPathEn);

        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
        await _activityLogger.LogAsync(isNew ? "Created" : "Updated", "LegacyRedirect", redirect.Id, fromPath, cancellationToken);
        return ServiceResult<int>.Success(redirect.Id);
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var redirect = await _db.LegacyRedirects.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (redirect is null)
        {
            return ServiceResult.Failure("Yönlendirme bulunamadı.");
        }

        _db.LegacyRedirects.Remove(redirect);
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
        await _activityLogger.LogAsync("Deleted", "LegacyRedirect", id, redirect.FromPath, cancellationToken);
        return ServiceResult.Success();
    }
}
