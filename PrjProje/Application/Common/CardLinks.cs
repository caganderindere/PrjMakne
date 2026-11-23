namespace PrjProje.Application.Common;

/// <summary>Turns what admins type (0532 ..., linkedin.com/in/x, @handle) into working tel:, wa.me and https links.</summary>
public static class CardLinks
{
    /// <summary>Digits with a leading +; Turkish numbers written as 0XXX... or XXX... get +90.</summary>
    public static string? Phone(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return null;
        if (value!.TrimStart().StartsWith('+')) return "+" + digits;
        if (digits.StartsWith("00")) return "+" + digits[2..];
        if (digits.StartsWith('0')) return "+90" + digits[1..];
        return digits.Length == 10 ? "+90" + digits : "+" + digits;
    }

    public static string? WhatsApp(string? value) => Phone(value) is { } phone ? $"https://wa.me/{phone.TrimStart('+')}" : null;

    /// <summary>Anything without an allowed scheme gets https://, so a typed "javascript:..." can never run.</summary>
    public static string? Web(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        var hasScheme = new[] { "http://", "https://", "mailto:", "tel:" }.Any(s => value.StartsWith(s, StringComparison.OrdinalIgnoreCase));
        return hasScheme ? value : "https://" + value;
    }

    public static string? LinkedIn(string? value) => Profile(value, "linkedin.com", "https://www.linkedin.com/in/");

    public static string? Instagram(string? value) => Profile(value, "instagram.com", "https://www.instagram.com/");

    public static IEnumerable<(string Label, string Url)> ExtraLinks(string? value) =>
        (value ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split('|', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2 && parts[1].Length > 0)
            .Select(parts => (parts[0], Web(parts[1])!));

    private static string? Profile(string? value, string domain, string baseUrl) =>
        string.IsNullOrWhiteSpace(value) ? null
        : value.Contains(domain, StringComparison.OrdinalIgnoreCase) ? Web(value)
        : baseUrl + value.Trim().Trim('/').TrimStart('@');
}
