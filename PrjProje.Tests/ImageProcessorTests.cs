using System.ComponentModel.DataAnnotations;
using PrjProje.Application.Models.Admin;
using PrjProje.Infrastructure.Services.Support;
using SkiaSharp;

namespace PrjProje.Tests;

public class ImageProcessorTests
{
    private static MemoryStream Png(int width, int height, SKColor color)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        bitmap.Erase(color);
        using var data = SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Png, 100);
        return new MemoryStream(data.ToArray());
    }

    private static SKBitmap Decode(byte[] webp) => SKBitmap.Decode(webp)!;

    private static bool IsWebp(byte[] data) =>
        data.Length > 12 && data.AsSpan(0, 4).SequenceEqual("RIFF"u8) && data.AsSpan(8, 4).SequenceEqual("WEBP"u8);

    [Fact]
    public void Large_png_becomes_webp_with_a_2000px_longest_edge()
    {
        var result = ImageProcessor.ToWebp(Png(4000, 3000, SKColors.SteelBlue));

        Assert.True(result.Succeeded);
        Assert.True(IsWebp(result.Value!));
        using var image = Decode(result.Value!);
        Assert.Equal((2000, 1500), (image.Width, image.Height));
    }

    [Fact]
    public void Portrait_images_are_limited_by_height()
    {
        using var image = Decode(ImageProcessor.ToWebp(Png(1200, 3000, SKColors.Gray)).Value!);
        Assert.Equal((800, 2000), (image.Width, image.Height));
    }

    [Fact]
    public void Small_images_are_not_upscaled()
    {
        using var image = Decode(ImageProcessor.ToWebp(Png(640, 480, SKColors.Gray)).Value!);
        Assert.Equal((640, 480), (image.Width, image.Height));
    }

    [Fact]
    public void Corrupt_bytes_return_an_error_instead_of_throwing()
    {
        var result = ImageProcessor.ToWebp(new MemoryStream([0x89, 0x50, 0x4E, 0x47, 0x00, 0x13, 0x37, 0x00, 0xFF]));
        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Transparency_survives()
    {
        using var image = Decode(ImageProcessor.ToWebp(Png(300, 200, SKColors.Transparent)).Value!);
        Assert.Equal(0, image.GetPixel(10, 10).Alpha);
    }

    [Fact]
    public void Srcset_variants_are_made_only_below_the_original_width()
    {
        var result = ImageProcessor.ToWebpSet(Png(1000, 750, SKColors.Gray));

        Assert.True(result.Succeeded);
        Assert.Equal([480, 960, 1000], result.Value!.Select(v => v.Width));
        Assert.All(result.Value!, v => Assert.True(IsWebp(v.Data)));
    }

    [Fact]
    public void Full_set_for_a_large_photo()
    {
        var result = ImageProcessor.ToWebpSet(Png(4000, 3000, SKColors.Gray));
        Assert.Equal([480, 960, 1600, 2000], result.Value!.Select(v => v.Width));
    }

    [Fact]
    public void Product_image_needs_turkish_alt_text()
    {
        var image = new ProductImageItem { ImagePath = "/uploads/products/a.webp", AltTr = " " };
        var errors = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(image, new ValidationContext(image), errors, validateAllProperties: true));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(ProductImageItem.AltTr)));
    }
}
