namespace PrjProje.Application.Common;

/// <summary>
/// What a visitor may attach to a quote request: drawings and photos up to 10 MB. The extension must be on the list
/// and the first bytes must match it, so an executable renamed to .pdf is refused.
/// </summary>
public static class QuoteAttachmentRules
{
    public const long MaxBytes = 10 * 1024 * 1024;

    /// <summary>Bytes needed from the start of the file for <see cref="Validate"/>.</summary>
    public const int HeaderLength = 512;

    private delegate bool ReadOnlySpanPredicate(ReadOnlySpan<byte> header);

    private static readonly Dictionary<string, ReadOnlySpanPredicate> Signatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = header => header.StartsWith("%PDF-"u8),
        [".jpg"] = IsJpeg,
        [".jpeg"] = IsJpeg,
        [".png"] = header => header.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        [".zip"] = header => header.StartsWith(new byte[] { 0x50, 0x4B, 0x03, 0x04 }),
        [".dwg"] = header => header.StartsWith("AC1"u8),
        [".dxf"] = IsPlainText,
        [".step"] = IsPlainText,
        [".stp"] = IsPlainText
    };

    public static IReadOnlyCollection<string> AllowedExtensions => Signatures.Keys;

    /// <summary>For the file input's accept attribute.</summary>
    public static string AcceptAttribute => string.Join(',', Signatures.Keys);

    /// <returns>Null when the file is acceptable, otherwise a SharedResource key describing the problem.</returns>
    public static string? Validate(string fileName, long length, ReadOnlySpan<byte> header)
    {
        if (length <= 0 || !Signatures.TryGetValue(Path.GetExtension(fileName), out var matches))
        {
            return "Validation.FileType";
        }

        if (length > MaxBytes)
        {
            return "Validation.FileSize";
        }

        return matches(header) ? null : "Validation.FileContent";
    }

    private static bool IsJpeg(ReadOnlySpan<byte> header) => header.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF });

    /// <summary>CAD exchange formats (DXF, STEP) are plain text; executables, markup and binaries are refused.</summary>
    private static bool IsPlainText(ReadOnlySpan<byte> header) =>
        header.Length > 0 && !header.StartsWith("MZ"u8) && header[0] != (byte)'<' && !header.Contains((byte)0);
}
