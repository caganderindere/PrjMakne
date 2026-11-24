using System.Text.RegularExpressions;

namespace PrjProje.Application.Common;

/// <summary>Reads the 11-character video id from whatever YouTube link an editor pastes (watch, youtu.be, shorts, embed).</summary>
public static partial class YouTubeId
{
    private static readonly string[] Hosts = ["youtube.com", "www.youtube.com", "m.youtube.com", "youtu.be", "www.youtube-nocookie.com"];

    public static string? Parse(string? input)
    {
        var text = input?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        if (IdPattern().IsMatch(text))
        {
            return text;
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || !Hosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string? candidate = uri.Host.EndsWith("youtu.be", StringComparison.OrdinalIgnoreCase)
            ? segments.FirstOrDefault()
            : segments is ["watch"]
                ? uri.Query.TrimStart('?').Split('&').Select(p => p.Split('=', 2)).FirstOrDefault(p => p[0] == "v" && p.Length == 2)?[1]
                : segments is ["shorts" or "embed" or "live", var id, ..] ? id : null;

        return candidate is not null && IdPattern().IsMatch(candidate) ? candidate : null;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{11}$")]
    private static partial Regex IdPattern();
}
