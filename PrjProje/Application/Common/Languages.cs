using System.Globalization;

namespace PrjProje.Application.Common;

/// <summary>
/// Single source of truth for supported content languages.
/// Adding a new language: add its code here, add a SharedResource.{code}.resx file
/// and add its URL segments in SiteRoutes.
/// </summary>
public static class Languages
{
    public const string Turkish = "tr";
    public const string English = "en";
    public const string Default = Turkish;

    public static readonly IReadOnlyList<string> All = new[] { Turkish, English };

    private static readonly Dictionary<string, string> DisplayNames = new()
    {
        [Turkish] = "Türkçe",
        [English] = "English"
    };

    private static readonly Dictionary<string, string> OpenGraphLocales = new()
    {
        [Turkish] = "tr_TR",
        [English] = "en_US"
    };

    // Flag file (images/flags/{flag}.svg) shown in the language menu; English uses the UK flag.
    private static readonly Dictionary<string, string> Flags = new()
    {
        [Turkish] = "tr",
        [English] = "gb"
    };

    public static bool IsSupported(string? code) =>
        !string.IsNullOrWhiteSpace(code) && All.Contains(code, StringComparer.OrdinalIgnoreCase);

    public static string Current
    {
        get
        {
            var code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return IsSupported(code) ? code : Default;
        }
    }

    public static string DisplayName(string code) => DisplayNames.TryGetValue(code, out var name) ? name : code.ToUpperInvariant();

    public static string Flag(string code) => Flags.TryGetValue(code, out var flag) ? flag : code;

    public static string OpenGraphLocale(string code) => OpenGraphLocales.TryGetValue(code, out var locale) ? locale : code;
}
