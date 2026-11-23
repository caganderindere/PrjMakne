using System.Globalization;
using PrjProje.Infrastructure.Services.Support;

namespace PrjProje.Web.Infrastructure;

public static class ViewHelpers
{
    /// <summary>Returns the small (mobile/card) variant generated next to every uploaded WebP image.</summary>
    public static string SmallImage(string path) =>
        path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) && path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
            ? path[..^5] + FileStorageService.SmallImageSuffix + ".webp"
            : path;

    public static string SrcSet(string path) =>
        SmallImage(path) == path ? path : $"{SmallImage(path)} 640w, {path} 1920w";

    public static string FileSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size.ToString(unit == 0 ? "0" : "0.#", CultureInfo.CurrentCulture)} {units[unit]}";
    }

    public static string Truncate(string? text, int length) =>
        string.IsNullOrEmpty(text) || text.Length <= length ? text ?? string.Empty : text[..(length - 1)].TrimEnd() + "…";
}
