using PrjProje.Application.Common;
using SkiaSharp;

namespace PrjProje.Infrastructure.Services.Support;

/// <summary>
/// Turns any uploaded photo (phone JPEG, CMYK JPEG, transparent PNG, huge camera file) into clean WebP: EXIF rotation
/// applied, metadata dropped, longest edge capped. A file Skia cannot read is reported, never thrown.
/// </summary>
public static class ImageProcessor
{
    public const int MaxEdge = 2000;
    public const int Quality = 82;

    /// <summary>Widths written next to every upload for srcset; the full-size file is the last entry.</summary>
    public static readonly IReadOnlyList<int> SrcSetWidths = [480, 960, 1600];

    public static ServiceResult<byte[]> ToWebp(Stream input, int maxEdge = MaxEdge, int quality = Quality)
    {
        using var source = Decode(input);
        if (source is null)
        {
            return ServiceResult<byte[]>.Failure("Görsel okunamadı; dosya bozuk veya desteklenmeyen bir biçimde.");
        }

        var (width, height) = Fit(source.Width, source.Height, maxEdge, maxEdge);
        return ServiceResult<byte[]>.Success(Encode(source, width, height, quality));
    }

    /// <summary>The srcset widths smaller than the image, plus the image itself capped at <see cref="MaxEdge"/>.</summary>
    public static ServiceResult<IReadOnlyList<(int Width, byte[] Data)>> ToWebpSet(Stream input, int quality = Quality)
    {
        using var source = Decode(input);
        if (source is null)
        {
            return ServiceResult<IReadOnlyList<(int Width, byte[] Data)>>.Failure("Görsel okunamadı; dosya bozuk veya desteklenmeyen bir biçimde.");
        }

        var (fullWidth, fullHeight) = Fit(source.Width, source.Height, MaxEdge, MaxEdge);
        var set = SrcSetWidths
            .Where(width => width < fullWidth)
            .Select(width => (width, Encode(source, width, (int)Math.Round(fullHeight * (width / (double)fullWidth)), quality)))
            .Append((fullWidth, Encode(source, fullWidth, fullHeight, quality)))
            .ToList();
        return ServiceResult<IReadOnlyList<(int Width, byte[] Data)>>.Success(set);
    }

    private static (int Width, int Height) Fit(int width, int height, int maxWidth, int maxHeight)
    {
        var scale = Math.Min(1.0, Math.Min(maxWidth / (double)width, maxHeight / (double)height));
        return (Math.Max(1, (int)Math.Round(width * scale)), Math.Max(1, (int)Math.Round(height * scale)));
    }

    private static SKBitmap? Decode(Stream input)
    {
        try
        {
            using var data = SKData.Create(input);
            using var codec = data is null ? null : SKCodec.Create(data);
            if (codec is null)
            {
                return null;
            }

            var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            var bitmap = new SKBitmap(info);
            var result = codec.GetPixels(info, bitmap.GetPixels());
            if (result is not (SKCodecResult.Success or SKCodecResult.IncompleteInput))
            {
                bitmap.Dispose();
                return null;
            }

            return Orient(bitmap, codec.EncodedOrigin);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException)
        {
            return null;
        }
    }

    /// <summary>Phones store portrait photos sideways with an EXIF flag; the pixels are turned so the flag is not needed.</summary>
    private static SKBitmap Orient(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        if (origin is SKEncodedOrigin.TopLeft or SKEncodedOrigin.Default)
        {
            return bitmap;
        }

        var swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var rotated = new SKBitmap(swap ? bitmap.Height : bitmap.Width, swap ? bitmap.Width : bitmap.Height, bitmap.ColorType, bitmap.AlphaType);
        using (var canvas = new SKCanvas(rotated))
        {
            var (w, h) = (bitmap.Width, bitmap.Height);
            switch (origin)
            {
                case SKEncodedOrigin.TopRight: canvas.Scale(-1, 1, w / 2f, 0); break;
                case SKEncodedOrigin.BottomRight: canvas.RotateDegrees(180, w / 2f, h / 2f); break;
                case SKEncodedOrigin.BottomLeft: canvas.Scale(1, -1, 0, h / 2f); break;
                case SKEncodedOrigin.LeftTop: canvas.Translate(h, 0); canvas.RotateDegrees(90); canvas.Scale(1, -1, 0, h / 2f); break;
                case SKEncodedOrigin.RightTop: canvas.Translate(h, 0); canvas.RotateDegrees(90); break;
                case SKEncodedOrigin.RightBottom: canvas.Translate(0, w); canvas.RotateDegrees(-90); canvas.Scale(1, -1, 0, h / 2f); break;
                case SKEncodedOrigin.LeftBottom: canvas.Translate(0, w); canvas.RotateDegrees(-90); break;
            }

            canvas.DrawBitmap(bitmap, 0, 0);
        }

        bitmap.Dispose();
        return rotated;
    }

    private static byte[] Encode(SKBitmap source, int width, int height, int quality)
    {
        using var resized = width == source.Width && height == source.Height
            ? null
            : source.Resize(new SKImageInfo(width, height, source.ColorType, source.AlphaType), SKFilterQuality.High);
        using var image = SKImage.FromBitmap(resized ?? source);
        using var data = image.Encode(SKEncodedImageFormat.Webp, quality);
        return data.ToArray();
    }
}
