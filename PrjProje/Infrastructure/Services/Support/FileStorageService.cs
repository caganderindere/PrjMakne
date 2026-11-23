using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace PrjProje.Infrastructure.Services.Support;

public class FileUploadOptions
{
    public const string SectionName = "Uploads";

    public long MaxImageBytes { get; set; } = 8 * 1024 * 1024;
    public long MaxDocumentBytes { get; set; } = 20 * 1024 * 1024;
    public long MaxAttachmentBytes { get; set; } = 10 * 1024 * 1024;
    public int MaxImageWidth { get; set; } = 1920;
    public int SmallImageWidth { get; set; } = 640;
    public int WebpQuality { get; set; } = 82;
}

public sealed class FileStorageService : IFileStorageService
{
    public const string SmallImageSuffix = "-sm";
    public const string PublicUploadRoot = "uploads";
    public const string PrivateUploadRoot = "App_Data/quote-files";

    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    private static readonly Dictionary<string, Func<byte[], bool>> AttachmentSignatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = IsPdf,
        [".jpg"] = IsJpeg,
        [".jpeg"] = IsJpeg,
        [".png"] = IsPng,
        [".zip"] = header => StartsWith(header, 0x50, 0x4B, 0x03, 0x04),
        [".dwg"] = header => StartsWith(header, 0x41, 0x43, 0x31),
        [".dxf"] = IsPlainText,
        [".step"] = IsPlainText,
        [".stp"] = IsPlainText,
        [".igs"] = IsPlainText,
        [".iges"] = IsPlainText
    };

    // CVs: PDF, Word (.docx is a zip package, .doc an OLE compound file).
    private static readonly Dictionary<string, Func<byte[], bool>> ResumeSignatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = IsPdf,
        [".docx"] = header => StartsWith(header, 0x50, 0x4B, 0x03, 0x04),
        [".doc"] = header => StartsWith(header, 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1)
    };

    public static IReadOnlyCollection<string> AllowedResumeExtensions => ResumeSignatures.Keys;

    private readonly IWebHostEnvironment _environment;
    private readonly FileUploadOptions _options;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(IWebHostEnvironment environment, IOptions<FileUploadOptions> options, ILogger<FileStorageService> logger)
    {
        _environment = environment;
        _options = options.Value;
        _logger = logger;
    }

    public static IReadOnlyCollection<string> AllowedAttachmentExtensions => AttachmentSignatures.Keys;

    public async Task<ServiceResult<StoredFile>> SaveImageAsync(IFormFile file, ImageFolder folder, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (file.Length == 0 || !ImageExtensions.Contains(extension))
        {
            return ServiceResult<StoredFile>.Failure($"{file.FileName}: Yalnızca JPG, PNG, WEBP veya GIF görseller yüklenebilir.");
        }

        if (file.Length > _options.MaxImageBytes)
        {
            return ServiceResult<StoredFile>.Failure($"{file.FileName}: Görsel boyutu en fazla {_options.MaxImageBytes / 1024 / 1024} MB olabilir.");
        }

        await using var memory = new MemoryStream();
        await file.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();

        if (!IsJpeg(bytes) && !IsPng(bytes) && !IsWebp(bytes) && !IsGif(bytes))
        {
            return ServiceResult<StoredFile>.Failure($"{file.FileName}: Dosya içeriği geçerli bir görsel değil.");
        }

        // Re-encoding through SkiaSharp guarantees the stored file is a clean bitmap:
        // EXIF/GPS metadata and any appended payloads are discarded.
        using var original = SKBitmap.Decode(bytes);
        if (original is null)
        {
            return ServiceResult<StoredFile>.Failure($"{file.FileName}: Görsel okunamadı.");
        }

        var now = DateTime.UtcNow;
        var relativeFolder = $"{PublicUploadRoot}/{folder.ToString().ToLowerInvariant()}/{now.Year:D4}/{now.Month:D2}";
        var physicalFolder = Path.Combine(_environment.WebRootPath, relativeFolder.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(physicalFolder);

        var baseName = SlugHelper.Generate(Path.GetFileNameWithoutExtension(file.FileName), 50);
        var fileName = $"{(string.IsNullOrEmpty(baseName) ? "image" : baseName)}-{Guid.NewGuid().ToString("N")[..12]}";
        var largePath = Path.Combine(physicalFolder, fileName + ".webp");
        var smallPath = Path.Combine(physicalFolder, fileName + SmallImageSuffix + ".webp");

        var largeSize = WriteWebp(original, _options.MaxImageWidth, largePath);
        WriteWebp(original, _options.SmallImageWidth, smallPath);

        return ServiceResult<StoredFile>.Success(new StoredFile($"/{relativeFolder}/{fileName}.webp", largeSize, file.FileName));
    }

    public async Task<ServiceResult<StoredFile>> SaveDocumentAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (file.Length == 0 || extension != ".pdf")
        {
            return ServiceResult<StoredFile>.Failure("Yalnızca PDF dosyaları yüklenebilir.");
        }

        if (file.Length > _options.MaxDocumentBytes)
        {
            return ServiceResult<StoredFile>.Failure($"PDF boyutu en fazla {_options.MaxDocumentBytes / 1024 / 1024} MB olabilir.");
        }

        var header = await ReadHeaderAsync(file, cancellationToken);
        if (!IsPdf(header))
        {
            return ServiceResult<StoredFile>.Failure("Dosya içeriği geçerli bir PDF değil.");
        }

        var relativeFolder = $"{PublicUploadRoot}/documents/{DateTime.UtcNow:yyyy}";
        var physicalFolder = Path.Combine(_environment.WebRootPath, relativeFolder.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(physicalFolder);

        var baseName = SlugHelper.Generate(Path.GetFileNameWithoutExtension(file.FileName), 60);
        var fileName = $"{(string.IsNullOrEmpty(baseName) ? "document" : baseName)}-{Guid.NewGuid().ToString("N")[..8]}.pdf";
        await using (var stream = new FileStream(Path.Combine(physicalFolder, fileName), FileMode.CreateNew))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return ServiceResult<StoredFile>.Success(new StoredFile($"/{relativeFolder}/{fileName}", file.Length, file.FileName));
    }

    public Task<ServiceResult<StoredFile>> SavePrivateAttachmentAsync(IFormFile file, CancellationToken cancellationToken = default) =>
        SavePrivateAsync(file, AttachmentSignatures, cancellationToken);

    public Task<ServiceResult<StoredFile>> SaveResumeAsync(IFormFile file, CancellationToken cancellationToken = default) =>
        SavePrivateAsync(file, ResumeSignatures, cancellationToken);

    private async Task<ServiceResult<StoredFile>> SavePrivateAsync(IFormFile file, IReadOnlyDictionary<string, Func<byte[], bool>> signatures, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (file.Length == 0 || !signatures.TryGetValue(extension, out var signatureCheck))
        {
            return ServiceResult<StoredFile>.Failure("Validation.FileType");
        }

        if (file.Length > _options.MaxAttachmentBytes)
        {
            return ServiceResult<StoredFile>.Failure("Validation.FileSize");
        }

        var header = await ReadHeaderAsync(file, cancellationToken);
        if (!signatureCheck(header))
        {
            return ServiceResult<StoredFile>.Failure("Validation.FileContent");
        }

        var relativeFolder = $"{DateTime.UtcNow:yyyy}/{DateTime.UtcNow:MM}";
        var physicalFolder = Path.Combine(PrivateRootPath, relativeFolder.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(physicalFolder);

        // Random name without the user's file name prevents path tricks; the original name is kept only in the database.
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using (var stream = new FileStream(Path.Combine(physicalFolder, fileName), FileMode.CreateNew))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var originalName = Path.GetFileName(file.FileName);
        if (originalName.Length > 200)
        {
            originalName = originalName[^200..];
        }

        return ServiceResult<StoredFile>.Success(new StoredFile($"{relativeFolder}/{fileName}", file.Length, originalName));
    }

    public string? GetPrivateAttachmentPhysicalPath(string relativePath)
    {
        var root = Path.GetFullPath(PrivateRootPath);
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        return fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath) ? fullPath : null;
    }

    public void DeletePublicFile(string? webPath)
    {
        if (string.IsNullOrWhiteSpace(webPath) || !webPath.StartsWith($"/{PublicUploadRoot}/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var root = Path.GetFullPath(Path.Combine(_environment.WebRootPath, PublicUploadRoot));
        var fullPath = Path.GetFullPath(Path.Combine(_environment.WebRootPath, webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        TryDelete(fullPath);
        if (fullPath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
        {
            TryDelete(fullPath[..^5] + SmallImageSuffix + ".webp");
        }
    }

    public void DeletePrivateFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        var fullPath = GetPrivateAttachmentPhysicalPath(relativePath);
        if (fullPath is not null)
        {
            TryDelete(fullPath);
        }
    }

    private string PrivateRootPath => Path.Combine(_environment.ContentRootPath, PrivateUploadRoot.Replace('/', Path.DirectorySeparatorChar));

    private long WriteWebp(SKBitmap source, int maxWidth, string path)
    {
        var bitmap = source;
        SKBitmap? resized = null;
        if (source.Width > maxWidth)
        {
            var height = (int)Math.Round(source.Height * (maxWidth / (double)source.Width));
            resized = source.Resize(new SKImageInfo(maxWidth, height), SKFilterQuality.High);
            bitmap = resized ?? source;
        }

        try
        {
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Webp, _options.WebpQuality);
            using var output = File.Create(path);
            data.SaveTo(output);
            return data.Size;
        }
        finally
        {
            resized?.Dispose();
        }
    }

    private void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "File could not be deleted: {Path}", path);
        }
    }

    private static async Task<byte[]> ReadHeaderAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var buffer = new byte[512];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken);
        return buffer[..read];
    }

    private static bool StartsWith(byte[] data, params byte[] signature) =>
        data.Length >= signature.Length && data.AsSpan(0, signature.Length).SequenceEqual(signature);

    private static bool IsPdf(byte[] header) => StartsWith(header, 0x25, 0x50, 0x44, 0x46, 0x2D);
    private static bool IsJpeg(byte[] header) => StartsWith(header, 0xFF, 0xD8, 0xFF);
    private static bool IsPng(byte[] header) => StartsWith(header, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A);
    private static bool IsGif(byte[] header) => StartsWith(header, 0x47, 0x49, 0x46, 0x38);

    private static bool IsWebp(byte[] header) =>
        StartsWith(header, 0x52, 0x49, 0x46, 0x46) && header.Length >= 12 &&
        header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;

    /// <summary>CAD exchange formats (DXF, STEP, IGES) are plain text; reject binaries, executables and markup.</summary>
    private static bool IsPlainText(byte[] header)
    {
        if (header.Length == 0 || StartsWith(header, 0x4D, 0x5A) || header[0] == (byte)'<')
        {
            return false;
        }

        return !header.Contains((byte)0);
    }
}
