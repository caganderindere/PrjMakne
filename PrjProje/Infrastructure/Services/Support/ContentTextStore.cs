using PrjProje.Application.Interfaces;
using PrjProje.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace PrjProje.Infrastructure.Services.Support;

/// <summary>
/// In-memory view of the admin text overrides. Localized string lookups are synchronous,
/// so the table is loaded once (synchronously, in its own scope) and kept until an edit invalidates it.
/// </summary>
public sealed class ContentTextStore : IContentTextStore
{
    private const string CacheKey = "content-texts";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ContentTextStore> _logger;
    private readonly object _sync = new();
    private CancellationTokenSource _resetToken = new();

    public ContentTextStore(IServiceScopeFactory scopeFactory, IMemoryCache cache, ILogger<ContentTextStore> logger)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
        _logger = logger;
    }

    public string? Find(string languageCode, string key) =>
        Load().TryGetValue((languageCode, key), out var value) ? value : null;

    public void Invalidate()
    {
        CancellationTokenSource previous;
        lock (_sync)
        {
            previous = _resetToken;
            _resetToken = new CancellationTokenSource();
        }

        previous.Cancel();
        previous.Dispose();
    }

    private IReadOnlyDictionary<(string Language, string Key), string> Load()
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyDictionary<(string, string), string>? cached) && cached is not null)
        {
            return cached;
        }

        IReadOnlyDictionary<(string, string), string> values;
        var duration = TimeSpan.FromHours(6);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            values = db.ContentTexts.AsNoTracking()
                .Select(t => new { t.LanguageCode, t.Key, t.Value })
                .AsEnumerable()
                .ToDictionary(t => (t.LanguageCode, t.Key), t => t.Value);
        }
        catch (Exception exception)
        {
            // The site must keep working with the built-in texts if the table is unavailable.
            _logger.LogWarning(exception, "Content text overrides could not be loaded; built-in texts are used.");
            values = new Dictionary<(string, string), string>();
            duration = TimeSpan.FromMinutes(1);
        }

        CancellationToken token;
        lock (_sync)
        {
            token = _resetToken.Token;
        }

        _cache.Set(CacheKey, values, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(duration)
            .AddExpirationToken(new CancellationChangeToken(token)));
        return values;
    }
}
