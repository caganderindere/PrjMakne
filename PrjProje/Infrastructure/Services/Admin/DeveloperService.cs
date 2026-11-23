using System.Diagnostics;
using System.Runtime.InteropServices;
using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models.Admin;
using PrjProje.Domain.Enums;
using PrjProje.Infrastructure.Data;
using PrjProje.Infrastructure.Services.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace PrjProje.Infrastructure.Services.Admin;

public sealed class DeveloperService : IDeveloperService
{
    private const int SlowDatabaseMs = 500;
    private const int StaleRequestDays = 3;
    private const long LowDiskBytes = 2L * 1024 * 1024 * 1024;
    private const long CriticalDiskBytes = 500L * 1024 * 1024;

    private readonly ApplicationDbContext _db;
    private readonly RecentLogStore _logs;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public DeveloperService(ApplicationDbContext db, RecentLogStore logs, IWebHostEnvironment environment, IConfiguration configuration)
    {
        _db = db;
        _logs = logs;
        _environment = environment;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<HealthCheck>> GetChecksAsync(CancellationToken cancellationToken = default)
    {
        var checks = new List<HealthCheck>();
        var database = await CheckDatabaseAsync(cancellationToken);
        checks.Add(database);
        if (database.State != HealthState.Error)
        {
            checks.Add(await CheckMigrationsAsync(cancellationToken));
            checks.Add(await CheckAccountsAsync(cancellationToken));
        }

        checks.Add(CheckUploadFolders());
        checks.Add(CheckDisk());
        checks.Add(CheckLogs());
        checks.Add(CheckConfiguration());
        return checks;
    }

    public async Task<DeveloperReport> GetReportAsync(CancellationToken cancellationToken = default)
    {
        var checks = await GetChecksAsync(cancellationToken);
        // Without a database the page must still open and show what is wrong.
        var databaseUp = checks[0].State != HealthState.Error;
        return new DeveloperReport
        {
            Checks = checks,
            DatabaseChecked = databaseUp,
            ContentIssues = databaseUp ? await GetContentIssuesAsync(cancellationToken) : Array.Empty<ContentIssue>(),
            MissingFiles = databaseUp ? await GetMissingFilesAsync(cancellationToken) : Array.Empty<MissingFile>(),
            Logs = _logs.Snapshot(),
            Runtime = GetRuntimeFacts()
        };
    }

    private async Task<HealthCheck> CheckDatabaseAsync(CancellationToken cancellationToken)
    {
        const string name = "Veritabanı";
        const string icon = "fa-database";
        var watch = Stopwatch.StartNew();
        try
        {
            if (!await _db.Database.CanConnectAsync(cancellationToken))
            {
                return new HealthCheck(name, icon, HealthState.Error, "Bağlantı kurulamadı.");
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new HealthCheck(name, icon, HealthState.Error, $"Bağlantı kurulamadı: {exception.Message}");
        }

        var elapsed = watch.ElapsedMilliseconds;
        return elapsed > SlowDatabaseMs
            ? new HealthCheck(name, icon, HealthState.Warning, $"Yavaş yanıt veriyor ({elapsed} ms).")
            : new HealthCheck(name, icon, HealthState.Ok, $"Bağlı, {elapsed} ms.");
    }

    private async Task<HealthCheck> CheckMigrationsAsync(CancellationToken cancellationToken)
    {
        var pending = (await _db.Database.GetPendingMigrationsAsync(cancellationToken)).Count();
        return pending == 0
            ? new HealthCheck("Şema", "fa-diagram-project", HealthState.Ok, "Tüm migration'lar uygulanmış.")
            : new HealthCheck("Şema", "fa-diagram-project", HealthState.Error, $"{pending} migration uygulanmamış.");
    }

    private async Task<HealthCheck> CheckAccountsAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var locked = await _db.Users.CountAsync(u => u.LockoutEnd > now, cancellationToken);
        return locked == 0
            ? new HealthCheck("Hesaplar", "fa-user-lock", HealthState.Ok, "Kilitli hesap yok.")
            : new HealthCheck("Hesaplar", "fa-user-lock", HealthState.Warning, $"{locked} hesap hatalı girişler nedeniyle kilitli.");
    }

    private HealthCheck CheckUploadFolders()
    {
        var problem = WriteProblem(Path.Combine(_environment.WebRootPath, FileStorageService.PublicUploadRoot))
            ?? WriteProblem(Path.Combine(_environment.ContentRootPath, FileStorageService.PrivateUploadRoot));
        return problem is null
            ? new HealthCheck("Yükleme klasörleri", "fa-folder-open", HealthState.Ok, "Yazılabilir.")
            : new HealthCheck("Yükleme klasörleri", "fa-folder-open", HealthState.Error, $"Dosya yazılamıyor: {problem}");
    }

    private static string? WriteProblem(string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);
            var probe = Path.Combine(folder, $".health-{Guid.NewGuid():N}");
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return exception.Message;
        }
    }

    private HealthCheck CheckDisk()
    {
        const string name = "Disk";
        const string icon = "fa-hard-drive";
        try
        {
            var free = new DriveInfo(_environment.ContentRootPath).AvailableFreeSpace;
            var state = free < CriticalDiskBytes ? HealthState.Error : free < LowDiskBytes ? HealthState.Warning : HealthState.Ok;
            return new HealthCheck(name, icon, state, $"{FormatBytes(free)} boş.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return new HealthCheck(name, icon, HealthState.Warning, "Disk bilgisi okunamadı.");
        }
    }

    private HealthCheck CheckLogs()
    {
        var entries = _logs.Snapshot();
        var dayAgo = DateTime.UtcNow.AddDays(-1);
        var hourAgo = DateTime.UtcNow.AddHours(-1);
        var errors = entries.Count(e => e.IsError && e.CreatedDate > dayAgo);
        var warnings = entries.Count(e => !e.IsError && e.CreatedDate > dayAgo);
        var state = entries.Any(e => e.IsError && e.CreatedDate > hourAgo) ? HealthState.Error
            : errors > 0 ? HealthState.Warning
            : HealthState.Ok;
        return new HealthCheck("Hatalar", "fa-bug", state, $"Son 24 saatte {errors} hata, {warnings} uyarı.");
    }

    private HealthCheck CheckConfiguration()
    {
        const string name = "Yapılandırma";
        const string icon = "fa-sliders";
        if (_environment.IsDevelopment())
        {
            return new HealthCheck(name, icon, HealthState.Ok, "Geliştirme ortamı.");
        }

        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(_configuration["Site:BaseUrl"]))
        {
            problems.Add("Site:BaseUrl boş; SEO adresleri gelen isteğin alan adından üretiliyor.");
        }

        if (!string.IsNullOrEmpty(_configuration["SeedAdmin:Password"]))
        {
            problems.Add("SeedAdmin:Password hâlâ tanımlı; ilk kurulumdan sonra kaldırılmalı.");
        }

        return problems.Count == 0
            ? new HealthCheck(name, icon, HealthState.Ok, "Üretim ayarları tamam.")
            : new HealthCheck(name, icon, HealthState.Warning, string.Join(" ", problems));
    }

    private async Task<IReadOnlyList<ContentIssue>> GetContentIssuesAsync(CancellationToken cancellationToken)
    {
        var staleBefore = DateTime.UtcNow.AddDays(-StaleRequestDays);
        var languageCount = Languages.All.Count;
        return new[]
        {
            new ContentIssue("Görseli olmayan aktif ürün",
                await _db.Products.CountAsync(p => p.IsActive && string.IsNullOrEmpty(p.Image) && !p.Images.Any(), cancellationToken), "Products"),
            new ContentIssue("Çevirisi eksik ürün",
                await _db.Products.CountAsync(p => p.Translations.Count < languageCount, cancellationToken), "Products"),
            new ContentIssue("Hiçbir dilde yayında olmayan blog yazısı",
                await _db.BlogPosts.CountAsync(b => !b.Translations.Any(t => t.Status == PublishStatus.Published), cancellationToken), "Blog"),
            new ContentIssue($"{StaleRequestDays} günden eski, işleme alınmamış teklif talebi",
                await _db.QuoteRequests.CountAsync(q => q.Status == QuoteStatus.New && q.CreatedDate < staleBefore, cancellationToken), "Quotes"),
            new ContentIssue($"{StaleRequestDays} günden eski, okunmamış mesaj",
                await _db.ContactMessages.CountAsync(m => !m.IsRead && m.CreatedDate < staleBefore, cancellationToken), "Messages")
        };
    }

    private async Task<IReadOnlyList<MissingFile>> GetMissingFilesAsync(CancellationToken cancellationToken)
    {
        var missing = new List<MissingFile>();
        async Task CollectAsync(string source, IQueryable<string?> paths)
        {
            foreach (var path in await paths.Where(p => p != null && p != "").Distinct().ToListAsync(cancellationToken))
            {
                if (!FileExists(path!))
                {
                    missing.Add(new MissingFile(source, path!));
                }
            }
        }

        await CollectAsync("Ürün", _db.Products.Select(p => p.Image));
        await CollectAsync("Ürün galerisi", _db.ProductImages.Select(i => (string?)i.ImagePath));
        await CollectAsync("Ürün kategorisi", _db.ProductCategories.Select(c => c.Image));
        await CollectAsync("Blog", _db.BlogPosts.Select(b => b.CoverImage));
        await CollectAsync("Galeri", _db.GalleryItems.Select(g => (string?)g.ImagePath));
        await CollectAsync("Üretim kabiliyeti", _db.Capabilities.Select(c => c.Image));
        await CollectAsync("Belge", _db.Documents.Select(d => (string?)d.FilePath));
        await CollectAsync("Katalog kapağı", _db.Documents.Select(d => d.CoverImagePath));
        await CollectAsync("Referans", _db.References.Select(r => (string?)r.Logo));
        await CollectAsync("Duyuru", _db.Announcements.Select(a => a.Image));
        await CollectAsync("Kartvizit", _db.BusinessCards.Select(c => c.Photo));
        await CollectAsync("Fuar broşürü", _db.Fairs.Select(f => (string?)f.BrochurePdf));
        return missing;
    }

    private bool FileExists(string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var relative = path.Split('?')[0].TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        return File.Exists(Path.Combine(_environment.WebRootPath, relative));
    }

    private IReadOnlyList<RuntimeFact> GetRuntimeFacts()
    {
        using var process = Process.GetCurrentProcess();
        var uptime = DateTime.Now - process.StartTime;
        var uploads = Path.Combine(_environment.WebRootPath, FileStorageService.PublicUploadRoot);
        var uploadSizes = Directory.Exists(uploads)
            ? Directory.EnumerateFiles(uploads, "*", SearchOption.AllDirectories).Select(f => new FileInfo(f).Length).ToList()
            : new List<long>();
        var connection = _db.Database.GetDbConnection();

        return new[]
        {
            new RuntimeFact("Ortam", _environment.EnvironmentName),
            new RuntimeFact(".NET", RuntimeInformation.FrameworkDescription),
            new RuntimeFact("İşletim sistemi", RuntimeInformation.OSDescription),
            new RuntimeFact("Çalışma süresi", $"{(int)uptime.TotalDays} gün {uptime.Hours} saat {uptime.Minutes} dk"),
            new RuntimeFact("Bellek", FormatBytes(process.WorkingSet64)),
            new RuntimeFact("Thread", process.Threads.Count.ToString()),
            new RuntimeFact("Veritabanı", $"{connection.Database} @ {connection.DataSource}"),
            new RuntimeFact("Yüklenen dosyalar", $"{uploadSizes.Count} dosya, {FormatBytes(uploadSizes.Sum())}")
        };
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        >= 1024L * 1024 * 1024 => $"{bytes / 1024d / 1024 / 1024:0.#} GB",
        >= 1024L * 1024 => $"{bytes / 1024d / 1024:0.#} MB",
        _ => $"{bytes / 1024d:0.#} KB"
    };
}
