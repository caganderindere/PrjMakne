using PrjProje.Domain.Entities;
using PrjProje.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PrjProje.Infrastructure.Services.Support;

/// <summary>Collects per-route request totals in memory until the next flush to the database.</summary>
public sealed class RequestMetricsRecorder
{
    private readonly object _gate = new();
    private Dictionary<(DateTime Hour, string Route), RequestMetricHour> _pending = new();

    public static DateTime HourOf(DateTime utc) => new(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, DateTimeKind.Utc);

    public void Record(string route, int statusCode, long elapsedMs)
    {
        var sample = new RequestMetricHour
        {
            HourUtc = HourOf(DateTime.UtcNow),
            Route = route,
            RequestCount = 1,
            ClientErrorCount = statusCode is >= 400 and < 500 ? 1 : 0,
            ServerErrorCount = statusCode >= 500 ? 1 : 0,
            TotalMs = elapsedMs,
            Under100Ms = elapsedMs < 100 ? 1 : 0,
            Under250Ms = elapsedMs is >= 100 and < 250 ? 1 : 0,
            Under500Ms = elapsedMs is >= 250 and < 500 ? 1 : 0,
            Under1000Ms = elapsedMs is >= 500 and < 1000 ? 1 : 0,
            Under2500Ms = elapsedMs is >= 1000 and < 2500 ? 1 : 0,
            Over2500Ms = elapsedMs >= 2500 ? 1 : 0
        };

        lock (_gate)
        {
            AddToPending(sample);
        }
    }

    public IReadOnlyCollection<RequestMetricHour> Drain()
    {
        lock (_gate)
        {
            var drained = _pending.Values;
            _pending = new Dictionary<(DateTime Hour, string Route), RequestMetricHour>();
            return drained;
        }
    }

    /// <summary>Puts totals back after a failed flush so they are retried instead of lost.</summary>
    public void Restore(IEnumerable<RequestMetricHour> rows)
    {
        lock (_gate)
        {
            foreach (var row in rows)
            {
                AddToPending(row);
            }
        }
    }

    public static void Merge(RequestMetricHour target, RequestMetricHour source)
    {
        target.RequestCount += source.RequestCount;
        target.ClientErrorCount += source.ClientErrorCount;
        target.ServerErrorCount += source.ServerErrorCount;
        target.TotalMs += source.TotalMs;
        target.Under100Ms += source.Under100Ms;
        target.Under250Ms += source.Under250Ms;
        target.Under500Ms += source.Under500Ms;
        target.Under1000Ms += source.Under1000Ms;
        target.Under2500Ms += source.Under2500Ms;
        target.Over2500Ms += source.Over2500Ms;
    }

    private void AddToPending(RequestMetricHour sample)
    {
        var key = (sample.HourUtc, sample.Route);
        if (_pending.TryGetValue(key, out var existing))
        {
            Merge(existing, sample);
        }
        else
        {
            _pending[key] = sample;
        }
    }
}

/// <summary>Writes the collected totals to the database once a minute and removes rows past the retention period.</summary>
public sealed class RequestMetricsFlushService : BackgroundService
{
    private const int RetentionDays = 30;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromMinutes(1);

    private readonly RequestMetricsRecorder _recorder;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RequestMetricsFlushService> _logger;

    public RequestMetricsFlushService(RequestMetricsRecorder recorder, IServiceScopeFactory scopeFactory, ILogger<RequestMetricsFlushService> logger)
    {
        _recorder = recorder;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(FlushInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await FlushAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown; StopAsync writes the remaining totals.
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await FlushAsync(cancellationToken);
    }

    private async Task FlushAsync(CancellationToken cancellationToken)
    {
        var drained = _recorder.Drain();
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var hours = drained.Select(r => r.HourUtc).Distinct().ToList();
            var existing = await db.RequestMetricHours.Where(m => hours.Contains(m.HourUtc)).ToListAsync(cancellationToken);
            foreach (var row in drained)
            {
                var match = existing.FirstOrDefault(m => m.HourUtc == row.HourUtc && m.Route == row.Route);
                if (match is null)
                {
                    // A copy, so a failed save cannot leave a tracked row inside the totals that get retried.
                    var added = new RequestMetricHour { HourUtc = row.HourUtc, Route = row.Route };
                    RequestMetricsRecorder.Merge(added, row);
                    db.RequestMetricHours.Add(added);
                }
                else
                {
                    RequestMetricsRecorder.Merge(match, row);
                }
            }

            var hour = RequestMetricsRecorder.HourOf(DateTime.UtcNow);
            var server = await db.ServerMetricHours.FirstOrDefaultAsync(s => s.HourUtc == hour, cancellationToken);
            if (server is null)
            {
                db.ServerMetricHours.Add(new ServerMetricHour { HourUtc = hour, PeakMemoryBytes = Environment.WorkingSet });
            }
            else
            {
                server.PeakMemoryBytes = Math.Max(server.PeakMemoryBytes, Environment.WorkingSet);
            }

            await db.SaveChangesAsync(cancellationToken);

            var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);
            await db.RequestMetricHours.Where(m => m.HourUtc < cutoff).ExecuteDeleteAsync(cancellationToken);
            await db.ServerMetricHours.Where(s => s.HourUtc < cutoff).ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _recorder.Restore(drained);
            _logger.LogWarning(exception, "Performans verileri veritabanına yazılamadı; bir sonraki denemede tekrar denenecek.");
        }
        catch (OperationCanceledException)
        {
            _recorder.Restore(drained);
            throw;
        }
    }
}
