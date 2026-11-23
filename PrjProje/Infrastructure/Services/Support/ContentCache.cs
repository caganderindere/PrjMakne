using PrjProje.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace PrjProje.Infrastructure.Services.Support;

public sealed class ContentCache : IContentCache
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(10);
    private readonly IMemoryCache _cache;
    private readonly object _sync = new();
    private CancellationTokenSource _resetToken = new();

    public ContentCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? duration = null)
    {
        var cacheKey = $"content:{key}";
        if (_cache.TryGetValue(cacheKey, out T? cached) && cached is not null)
        {
            return cached;
        }

        var value = await factory();

        CancellationToken token;
        lock (_sync)
        {
            token = _resetToken.Token;
        }

        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(duration ?? DefaultDuration)
            .AddExpirationToken(new CancellationChangeToken(token));

        _cache.Set(cacheKey, value, options);
        return value;
    }

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
}
