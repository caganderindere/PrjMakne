namespace PrjProje.Application.Common;

/// <summary>Turns the number an admin types (0531 ..., +90 ..., 0090 ...) into a wa.me link with an optional ready message.</summary>
public static class WhatsAppLink
{
    public static string? Build(string? phone, string? message)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            return null;
        }

        // Turkish numbers written as 0XXX... or XXX XXX XX XX get the 90 country code.
        if (digits.StartsWith("00")) digits = digits[2..];
        else if (digits.StartsWith('0')) digits = "90" + digits[1..];
        else if (digits.Length == 10) digits = "90" + digits;

        var link = $"https://wa.me/{digits}";
        return string.IsNullOrWhiteSpace(message) ? link : $"{link}?text={Uri.EscapeDataString(message.Trim())}";
    }
}
