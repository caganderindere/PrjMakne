using System.Globalization;
using Microsoft.AspNetCore.Html;
using PrjProje.Infrastructure.Services.Support;

namespace PrjProje.Web.Infrastructure;

public static class ViewHelpers
{
    /// <summary>An icon from the sprite (images/icons.svg): Lucide line icons, plus brand logos such as "whatsapp".</summary>
    public static IHtmlContent Icon(string name, string? cssClass = null) =>
        new HtmlString($"<svg class=\"icon{(cssClass is null ? null : " " + cssClass)}\" aria-hidden=\"true\" focusable=\"false\"><use href=\"/images/icons.svg#{name}\"></use></svg>");

    /// <summary>The 480 px variant written next to every uploaded image (cards, thumbnails); other paths unchanged.</summary>
    public static string SmallImage(string path) => Variant(path, 480);

    /// <summary>srcset for an uploaded image: the 480/960/1600 variants and the full file (at most 2000 px).</summary>
    public static string SrcSet(string path) =>
        IsUpload(path)
            ? string.Join(", ", ImageProcessor.SrcSetWidths.Select(width => $"{Variant(path, width)} {width}w").Append($"{path} {ImageProcessor.MaxEdge}w"))
            : path;

    private static bool IsUpload(string path) =>
        path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) && path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);

    private static string Variant(string path, int width) => IsUpload(path) ? $"{path[..^5]}-{width}.webp" : path;

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
