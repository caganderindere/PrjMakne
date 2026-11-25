using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace PrjProje.Web.Infrastructure;

/// <summary>
/// Rich text from the panel holds plain &lt;img&gt; tags. Giving each one the width and height of its file lets the
/// browser reserve the space before the picture arrives, so the text does not jump while the page loads (CLS).
/// Only local files are measured; the sizes are read from the image header once and kept.
/// </summary>
public sealed partial class ContentImageSizes
{
    private readonly IWebHostEnvironment _environment;
    private readonly ConcurrentDictionary<string, (int Width, int Height)?> _sizes = new(StringComparer.OrdinalIgnoreCase);

    public ContentImageSizes(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string Apply(string? html) =>
        string.IsNullOrEmpty(html) ? string.Empty : ImageTag().Replace(html, match =>
        {
            var tag = match.Value;
            if (SizeAttribute().IsMatch(tag) || SourceAttribute().Match(tag) is not { Success: true } source ||
                Measure(source.Groups[1].Value) is not { } size)
            {
                return tag;
            }

            return tag.Insert(4, $" width=\"{size.Width}\" height=\"{size.Height}\"");
        });

    private (int Width, int Height)? Measure(string src) => _sizes.GetOrAdd(src, path =>
    {
        if (!path.StartsWith('/') || path.StartsWith("//"))
        {
            return null;
        }

        var file = _environment.WebRootFileProvider.GetFileInfo(path.Split('?')[0]);
        if (!file.Exists || file.PhysicalPath is null)
        {
            return null;
        }

        using var codec = SKCodec.Create(file.PhysicalPath);
        return codec is null ? null : (codec.Info.Width, codec.Info.Height);
    });

    [GeneratedRegex(@"<img\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ImageTag();

    [GeneratedRegex(@"\s(width|height)\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex SizeAttribute();

    [GeneratedRegex(@"\ssrc\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex SourceAttribute();
}
