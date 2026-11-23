using System.Text;
using PrjProje.Application.Common;
using PrjProje.Application.Models;
using QRCoder;
using SkiaSharp;

namespace PrjProje.Infrastructure.Services.Support;

/// <summary>Builds what a digital business card hands out: the QR code and the .vcf contact file.</summary>
public static class BusinessCardFiles
{
    // A card's address is a random 128-bit token instead of the person's name, so only someone holding the printed QR
    // code can open it: it cannot be guessed from the name or found by trying addresses.
    public static string NewToken() => Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

    public static bool IsToken(string? slug) => slug is { Length: 32 } && slug.All(char.IsAsciiHexDigitLower);

    public static byte[] QrPng(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(data).GetGraphic(20, new byte[] { 23, 25, 30 }, new byte[] { 255, 255, 255 });
    }

    public static string QrSvg(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        return new SvgQRCode(data).GetGraphic(10, "#17191e", "#ffffff");
    }

    /// <summary>For drawing inside a page: a viewBox instead of fixed width/height, so CSS can size it without cropping.</summary>
    public static string QrSvgScalable(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        return new SvgQRCode(data).GetGraphic(10, "#17191e", "#ffffff", true, SvgQRCode.SizingMode.ViewBoxAttribute);
    }

    /// <summary>A vCard 3.0 contact, which both iOS and Android import from a download. It leaves out the card's own
    /// address, so a contact shared on from someone's phone does not carry the private link with it.</summary>
    public static string VCard(BusinessCardDto card, string company, string webRootPath)
    {
        var lines = new List<string>
        {
            "BEGIN:VCARD",
            "VERSION:3.0",
            $"N:{Escape(card.LastName)};{Escape(card.FirstName)};;;",
            $"FN:{Escape(card.FullName)}",
            $"ORG:{Escape(company)}{(string.IsNullOrWhiteSpace(card.Department) ? "" : ";" + Escape(card.Department))}"
        };
        if (!string.IsNullOrWhiteSpace(card.Title)) lines.Add($"TITLE:{Escape(card.Title)}");
        if (CardLinks.Phone(card.Mobile) is { } mobile) lines.Add($"TEL;TYPE=CELL:{mobile}");
        if (CardLinks.Phone(card.Phone) is { } phone) lines.Add($"TEL;TYPE=WORK,VOICE:{phone}");
        if (!string.IsNullOrWhiteSpace(card.Email)) lines.Add($"EMAIL;TYPE=INTERNET,WORK:{Escape(card.Email)}");
        if (CardLinks.Web(card.Website) is { } website) lines.Add($"URL;TYPE=WORK:{website}");
        if (CardLinks.LinkedIn(card.LinkedIn) is { } linkedIn) lines.Add($"X-SOCIALPROFILE;TYPE=linkedin:{linkedIn}");
        if (CardLinks.Instagram(card.Instagram) is { } instagram) lines.Add($"X-SOCIALPROFILE;TYPE=instagram:{instagram}");
        if (Photo(card.Photo, webRootPath) is { } photo) lines.Add($"PHOTO;ENCODING=b;TYPE=JPEG:{photo}");
        lines.Add("END:VCARD");
        return string.Join("\r\n", lines.Select(Fold)) + "\r\n";
    }

    // Uploads are stored as WebP, which phones do not reliably read inside a vCard, so the small copy is re-encoded as JPEG.
    private static string? Photo(string? webPath, string webRootPath)
    {
        if (webPath is null) return null;
        var path = Path.Combine(webRootPath, webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        var small = Path.ChangeExtension(path, null) + FileStorageService.SmallImageSuffix + ".webp";
        using var bitmap = SKBitmap.Decode(File.Exists(small) ? small : path);
        using var jpeg = bitmap?.Encode(SKEncodedImageFormat.Jpeg, 80);
        return jpeg is null ? null : Convert.ToBase64String(jpeg.ToArray());
    }

    private static string Escape(string value) =>
        value.Trim().Replace("\\", "\\\\").Replace(";", @"\;").Replace(",", @"\,").Replace("\r", "").Replace("\n", @"\n");

    // vCard lines are folded at 75 octets; continuation lines start with a space.
    private static string Fold(string line)
    {
        if (Encoding.UTF8.GetByteCount(line) <= 75) return line;
        var builder = new StringBuilder();
        var length = 0;
        foreach (var rune in line.EnumerateRunes())
        {
            if (length + rune.Utf8SequenceLength > 75)
            {
                builder.Append("\r\n ");
                length = 1;
            }

            builder.Append(rune.ToString());
            length += rune.Utf8SequenceLength;
        }

        return builder.ToString();
    }
}
