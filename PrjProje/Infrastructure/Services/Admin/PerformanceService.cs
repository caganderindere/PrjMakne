using System.Diagnostics;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models.Admin;
using PrjProje.Domain.Entities;
using PrjProje.Infrastructure.Data;
using PrjProje.Infrastructure.Services.Support;
using Microsoft.EntityFrameworkCore;

namespace PrjProje.Infrastructure.Services.Admin;

public sealed class PerformanceService : IPerformanceService
{
    private const int SlowestRouteCount = 10;
    private const double BytesPerMb = 1024 * 1024;

    private readonly ApplicationDbContext _db;

    public PerformanceService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PerformanceReport> GetReportAsync(int days, CancellationToken cancellationToken = default)
    {
        days = days is 7 or 30 ? days : 1;
        var daily = days == 30;
        var currentHour = RequestMetricsRecorder.HourOf(DateTime.UtcNow);
        var today = DateTime.Now.Date;

        // Every slot is listed, even empty ones, so a quiet night shows as a gap instead of being skipped.
        var slots = daily
            ? Enumerable.Range(0, days).Select(i => today.AddDays(i - days + 1)).ToList()
            : Enumerable.Range(0, days * 24).Select(i => currentHour.AddHours(i - days * 24 + 1)).ToList();
        var from = daily ? slots[0].ToUniversalTime() : slots[0];
        DateTime SlotOf(DateTime hourUtc) => daily ? hourUtc.ToLocalTime().Date : hourUtc;

        var rows = await _db.RequestMetricHours.AsNoTracking().Where(m => m.HourUtc >= from).ToListAsync(cancellationToken);
        var servers = await _db.ServerMetricHours.AsNoTracking().Where(s => s.HourUtc >= from).ToListAsync(cancellationToken);
        var rowsBySlot = rows.ToLookup(r => SlotOf(r.HourUtc));
        var serversBySlot = servers.ToLookup(s => SlotOf(s.HourUtc));

        var points = slots.Select(slot =>
        {
            var totals = Combine(rowsBySlot[slot]);
            var peakMemory = serversBySlot[slot].Select(s => (long?)s.PeakMemoryBytes).Max();
            return new PerformancePoint(
                Label(slot, days),
                totals.RequestCount,
                Average(totals),
                EstimateP95(totals),
                Percent(totals.ClientErrorCount, totals.RequestCount),
                Percent(totals.ServerErrorCount, totals.RequestCount),
                peakMemory is null ? null : Math.Round(peakMemory.Value / BytesPerMb, 1));
        }).ToList();

        var routes = rows.GroupBy(r => r.Route)
            .Select(group =>
            {
                var totals = Combine(group);
                return new PerformanceRoute(group.Key, totals.RequestCount, Average(totals) ?? 0, EstimateP95(totals),
                    Percent(totals.ClientErrorCount + totals.ServerErrorCount, totals.RequestCount) ?? 0);
            })
            .OrderByDescending(r => r.AverageMs)
            .Take(SlowestRouteCount)
            .ToList();

        var overall = Combine(rows);
        using var process = Process.GetCurrentProcess();
        var uptime = DateTime.Now - process.StartTime;
        return new PerformanceReport
        {
            Points = points,
            SlowestRoutes = routes,
            TotalRequests = overall.RequestCount,
            AverageMs = Average(overall),
            P95Ms = EstimateP95(overall),
            ServerErrorPercent = Percent(overall.ServerErrorCount, overall.RequestCount),
            MemoryMb = Math.Round(Environment.WorkingSet / BytesPerMb, 1),
            Uptime = $"{(int)uptime.TotalDays} gün {uptime.Hours} saat {uptime.Minutes} dk"
        };
    }

    /// <summary>
    /// shortcut: reports the upper limit of the bucket holding the 95th percentile and tops out at 2500 ms;
    /// store finer buckets if exact values above that ever matter.
    /// </summary>
    public static int? EstimateP95(RequestMetricHour totals)
    {
        if (totals.RequestCount == 0)
        {
            return null;
        }

        var target = Math.Ceiling(totals.RequestCount * .95);
        var seen = totals.Under100Ms;
        if (seen >= target) return 100;
        seen += totals.Under250Ms;
        if (seen >= target) return 250;
        seen += totals.Under500Ms;
        if (seen >= target) return 500;
        seen += totals.Under1000Ms;
        return seen >= target ? 1000 : 2500;
    }

    private static RequestMetricHour Combine(IEnumerable<RequestMetricHour> rows)
    {
        var totals = new RequestMetricHour();
        foreach (var row in rows)
        {
            RequestMetricsRecorder.Merge(totals, row);
        }

        return totals;
    }

    private static double? Average(RequestMetricHour totals) =>
        totals.RequestCount == 0 ? null : Math.Round((double)totals.TotalMs / totals.RequestCount, 1);

    private static double? Percent(int part, int whole) =>
        whole == 0 ? null : Math.Round(part * 100d / whole, 2);

    private static string Label(DateTime slot, int days) => days switch
    {
        30 => slot.ToString("dd.MM"),
        7 => slot.ToLocalTime().ToString("dd.MM HH:00"),
        _ => slot.ToLocalTime().ToString("HH:00")
    };
}
