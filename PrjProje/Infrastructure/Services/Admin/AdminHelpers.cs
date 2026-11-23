using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace PrjProje.Infrastructure.Services.Admin;

internal static class AdminHelpers
{
    /// <summary>
    /// Builds a slug from the requested value (or the fallback text) and appends -2, -3... until it is unique
    /// within <paramref name="existingSlugs"/> (which must already be filtered by language and exclude the current record).
    /// </summary>
    public static async Task<string> UniqueSlugAsync(IQueryable<string> existingSlugs, string? requested, string fallbackSource, CancellationToken cancellationToken)
    {
        var baseSlug = SlugHelper.Generate(string.IsNullOrWhiteSpace(requested) ? fallbackSource : requested);
        if (string.IsNullOrEmpty(baseSlug))
        {
            baseSlug = $"item-{Guid.NewGuid().ToString("N")[..6]}";
        }

        var prefix = baseSlug + "-";
        var taken = await existingSlugs
            .Where(s => s == baseSlug || s.StartsWith(prefix))
            .ToListAsync(cancellationToken);

        if (!taken.Contains(baseSlug))
        {
            return baseSlug;
        }

        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"{baseSlug}-{suffix}";
            if (!taken.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static DateTime? LocalToUtc(DateTime? value) =>
        value is null ? null : DateTime.SpecifyKind(value.Value, DateTimeKind.Local).ToUniversalTime();

    public static DateTime? UtcToLocal(DateTime? value) =>
        value is null ? null : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc).ToLocalTime();

    /// <summary>
    /// Replaces an entity image: uploads the new file, or removes the current one when requested.
    /// Returns the resulting web path or the upload errors.
    /// </summary>
    public static async Task<ServiceResult<string?>> ReplaceImageAsync(
        IFileStorageService storage,
        string? currentPath,
        IFormFile? newFile,
        bool remove,
        ImageFolder folder,
        CancellationToken cancellationToken)
    {
        if (newFile is { Length: > 0 })
        {
            var upload = await storage.SaveImageAsync(newFile, folder, cancellationToken);
            if (!upload.Succeeded)
            {
                return ServiceResult<string?>.Failure(upload.Errors.ToArray());
            }

            storage.DeletePublicFile(currentPath);
            return ServiceResult<string?>.Success(upload.Value!.RelativePath);
        }

        if (remove)
        {
            storage.DeletePublicFile(currentPath);
            return ServiceResult<string?>.Success(null);
        }

        return ServiceResult<string?>.Success(currentPath);
    }

    /// <summary>Parses "Name: Value" lines; lines without a colon are ignored.</summary>
    public static IEnumerable<(string Name, string Value)> ParseSpecifications(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = line.IndexOf(':');
            if (separator <= 0 || separator == line.Length - 1)
            {
                continue;
            }

            var name = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (name.Length > 0 && value.Length > 0)
            {
                yield return (name.Length > 150 ? name[..150] : name, value.Length > 500 ? value[..500] : value);
            }
        }
    }
}
