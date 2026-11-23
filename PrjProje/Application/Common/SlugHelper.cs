using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PrjProje.Application.Common;

public static partial class SlugHelper
{
    private static readonly Dictionary<char, string> CharacterMap = new()
    {
        ['ç'] = "c",
        ['Ç'] = "c",
        ['ğ'] = "g",
        ['Ğ'] = "g",
        ['ı'] = "i",
        ['İ'] = "i",
        ['I'] = "i",
        ['ö'] = "o",
        ['Ö'] = "o",
        ['ş'] = "s",
        ['Ş'] = "s",
        ['ü'] = "u",
        ['Ü'] = "u",
        ['ß'] = "ss",
        ['æ'] = "ae",
        ['ø'] = "o",
        ['&'] = "-"
    };

    public static string Generate(string? text, int maxLength = 160)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        foreach (var character in text.Trim())
        {
            builder.Append(CharacterMap.TryGetValue(character, out var mapped) ? mapped : character.ToString());
        }

        var normalized = builder.ToString().Normalize(NormalizationForm.FormD);
        var ascii = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                ascii.Append(character);
            }
        }

        var slug = ascii.ToString().ToLowerInvariant();
        slug = InvalidCharacters().Replace(slug, " ");
        slug = Whitespace().Replace(slug, "-");
        slug = MultipleDashes().Replace(slug, "-").Trim('-');

        if (slug.Length > maxLength)
        {
            slug = slug[..maxLength].TrimEnd('-');
        }

        return slug;
    }

    [GeneratedRegex("[^a-z0-9\\s-]")]
    private static partial Regex InvalidCharacters();

    [GeneratedRegex("\\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex("-{2,}")]
    private static partial Regex MultipleDashes();
}
