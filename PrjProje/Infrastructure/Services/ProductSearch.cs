using System.Globalization;
using System.Text;

namespace PrjProje.Infrastructure.Services;

/// <summary>
/// Ranks products against a free-text query in memory: variant codes ("B 400", "YB 500") first, then the product name,
/// then category and descriptive fields. Text is folded so "kaucuk", "KAUÇUK" and "Kauçuk" match the same way.
/// </summary>
/// <remarks>shortcut: scores every product of a language per search; move to SQL full-text if the catalogue grows past a few thousand products.</remarks>
public static class ProductSearch
{
    public sealed class Document
    {
        /// <param name="variantCodes">Comma-separated, e.g. "B 300, B 400, YB 500".</param>
        public Document(string name, string? categoryName, string? details, string? variantCodes)
        {
            Name = new Field(name);
            Category = new Field(categoryName);
            Details = new Field(details);
            CodeText = new Field(variantCodes);
            Codes = (variantCodes ?? string.Empty)
                .Split([',', ';', '/', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(Compact)
                .Where(code => code.Length > 0)
                .ToArray();
        }

        internal Field Name { get; }
        internal Field Category { get; }
        internal Field Details { get; }
        internal Field CodeText { get; }
        internal string[] Codes { get; }
    }

    public sealed class Query
    {
        internal Query(string phrase, string[] tokens)
        {
            Phrase = phrase;
            Compact = phrase.Replace(" ", string.Empty);
            Tokens = tokens;
        }

        internal string Phrase { get; }
        internal string Compact { get; }
        internal string[] Tokens { get; }
    }

    internal sealed class Field
    {
        public Field(string? text)
        {
            Text = Normalize(text);
            Words = Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        public string Text { get; }
        public string[] Words { get; }
    }

    /// <summary>Returns null when the text has nothing searchable (only punctuation or spaces).</summary>
    public static Query? ParseQuery(string? text)
    {
        var phrase = Normalize(text);
        if (phrase.Length == 0)
        {
            return null;
        }

        var tokens = phrase.Split(' ').Distinct().ToArray();
        // Single characters ("a", "-") add noise to multi-word queries; keep them only when they are all there is.
        var meaningful = tokens.Where(t => t.Length > 1).ToArray();
        return new Query(phrase, meaningful.Length > 0 ? meaningful : tokens);
    }

    /// <summary>0 means "no match"; higher is more relevant.</summary>
    public static int Score(Document document, Query query)
    {
        var score = 0;

        // Buyers type model codes in many formats (spaces, dashes, dots), so codes are compared without separators.
        if (query.Compact.Length >= 3)
        {
            foreach (var code in document.Codes)
            {
                if (code == query.Compact)
                {
                    score = Math.Max(score, 1000);
                }
                else if (code.StartsWith(query.Compact, StringComparison.Ordinal))
                {
                    score = Math.Max(score, 700);
                }
                else if (code.Contains(query.Compact, StringComparison.Ordinal))
                {
                    score = Math.Max(score, 500);
                }
            }
        }

        var name = document.Name.Text;
        if (name == query.Phrase)
        {
            score += 900;
        }
        else if (name.StartsWith(query.Phrase + " ", StringComparison.Ordinal))
        {
            score += 600;
        }
        else if (name.Contains(" " + query.Phrase, StringComparison.Ordinal) || name.StartsWith(query.Phrase, StringComparison.Ordinal))
        {
            score += 400;
        }

        var wordScore = 0;
        foreach (var token in query.Tokens)
        {
            var best = Math.Max(
                Math.Max(Match(document.Name, token, 60), Match(document.CodeText, token, 50)),
                Math.Max(Match(document.Category, token, 25), Match(document.Details, token, 15)));

            // Every word must be found somewhere, unless the whole query already hit a code or the name.
            if (best == 0 && score == 0)
            {
                return 0;
            }

            wordScore += best;
        }

        return score + wordScore;
    }

    /// <summary>Lower-cases and strips accents (Turkish ı/İ included); anything that is not a letter or digit becomes one space.</summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;
        foreach (var c in value.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                if (pendingSpace && builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(c == 'ı' ? 'i' : char.ToLowerInvariant(c));
                pendingSpace = false;
            }
            else
            {
                pendingSpace = true;
            }
        }

        return builder.ToString();
    }

    /// <summary>Trims and collapses spaces, then drops empties and codes equal to an earlier one in another format ("YB 500" / "yb-500").</summary>
    public static IReadOnlyList<string> DistinctCodes(IEnumerable<string?> codes) =>
        codes
            .Select(code => string.Join(' ', (code ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries)))
            .Where(code => Compact(code).Length > 0)
            .DistinctBy(Compact)
            .ToList();

    private static string Compact(string? value) => Normalize(value).Replace(" ", string.Empty);

    private static int Match(Field field, string token, int weight)
    {
        var best = 0;
        foreach (var word in field.Words)
        {
            if (word == token)
            {
                return weight;
            }

            if (word.StartsWith(token, StringComparison.Ordinal))
            {
                best = Math.Max(best, weight * 3 / 4);
            }
            // Turkish suffixes: "takozlar" / "kauçukları" should still find "takoz" / "kauçuk".
            else if (word.Length >= 4 && token.StartsWith(word, StringComparison.Ordinal))
            {
                best = Math.Max(best, weight / 2);
            }
            else if (token.Length >= 3 && word.Contains(token, StringComparison.Ordinal))
            {
                best = Math.Max(best, weight / 3);
            }
        }

        return best;
    }
}
