namespace PrjProje.Application.Common;

/// <summary>
/// Folds the many spellings of an old WordPress URL (trailing slash, case, percent-encoded Turkish, ?lang=en,
/// the GTranslate /en/ prefix, tracking parameters) into one lookup key plus the language it asked for.
/// </summary>
public static class LegacyPath
{
    public static (string Path, string? Lang) Normalize(string pathAndQuery)
    {
        var queryStart = pathAndQuery.IndexOf('?');
        var path = queryStart < 0 ? pathAndQuery : pathAndQuery[..queryStart];
        var query = queryStart < 0 ? string.Empty : pathAndQuery[(queryStart + 1)..];

        string? lang = query.Split('&').Any(p => p.Equals("lang=en", StringComparison.OrdinalIgnoreCase)) ? Languages.English : null;

        try
        {
            path = Uri.UnescapeDataString(path);
        }
        catch (UriFormatException)
        {
            // Left as typed; it simply will not match a stored path.
        }

        var segments = path.ToLowerInvariant().Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length > 0 && segments[0] == Languages.English)
        {
            lang = Languages.English;
            segments = segments[1..];
        }

        return ("/" + string.Join('/', segments), lang);
    }
}
