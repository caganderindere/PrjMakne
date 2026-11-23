using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PrjProje.Tests;

public class BrandAssetsTests
{
    private static readonly string Root = BrandLeakTests.RepoRoot();
    private static readonly string WebRoot = Path.Combine(Root, "PrjProje", "wwwroot");
    private static string Tokens => File.ReadAllText(Path.Combine(WebRoot, "css", "tokens.css"));

    public static TheoryData<string> LogoFiles()
    {
        var data = new TheoryData<string>();
        foreach (var folder in new[] { Path.Combine("docs", "brand", "logo"), Path.Combine("PrjProje", "wwwroot", "images", "brand") })
        {
            foreach (var name in new[] { "prj-logo.svg", "prj-logo-horizontal.svg", "prj-monogram.svg", "prj-logo-negative.svg", "prj-logo-mono-black.svg" })
            {
                data.Add(Path.Combine(folder, name));
            }
        }

        return data;
    }

    [Theory]
    [InlineData("--prj-red")]
    [InlineData("--prj-red-hover")]
    [InlineData("--prj-logo-red")]
    [InlineData("--prj-anthracite")]
    [InlineData("--prj-cream")]
    [InlineData("--prj-steel-50")]
    [InlineData("--prj-steel-500")]
    [InlineData("--prj-steel-900")]
    [InlineData("--font-display")]
    [InlineData("--font-heading")]
    [InlineData("--font-body")]
    [InlineData("--radius-md")]
    [InlineData("--space-4")]
    [InlineData("--shadow-md")]
    public void Token_is_defined(string name) => Assert.Matches($@"{Regex.Escape(name)}\s*:", Tokens);

    [Fact]
    public void Colour_tokens_match_the_brand_spec()
    {
        Assert.Equal("#E30613", Token("--prj-red"));
        Assert.Equal("#B8050F", Token("--prj-red-hover"));
        Assert.Equal("#FF0000", Token("--prj-logo-red"));
        Assert.Equal("#1C2127", Token("--prj-anthracite"));
    }

    [Theory]
    [InlineData("--prj-red", "#FFFFFF")]
    [InlineData("--prj-red-hover", "#FFFFFF")]
    [InlineData("--prj-anthracite", "#FFFFFF")]
    [InlineData("--prj-steel-600", "#FFFFFF")]
    public void Text_colour_pairs_meet_wcag_aa(string token, string background) =>
        Assert.True(Contrast(Token(token), background) >= 4.5, $"{token} on {background}: {Contrast(Token(token), background):F2}");

    [Fact]
    public void Logo_red_alone_is_below_aa_which_is_why_the_ui_uses_a_deeper_red() =>
        Assert.True(Contrast("#FF0000", "#FFFFFF") < 4.5);

    [Theory]
    [MemberData(nameof(LogoFiles))]
    public void Logo_is_valid_titled_svg(string relativePath)
    {
        var svg = XDocument.Load(Path.Combine(Root, relativePath)).Root!;
        XNamespace ns = "http://www.w3.org/2000/svg";
        Assert.Equal(ns + "svg", svg.Name);
        Assert.False(string.IsNullOrWhiteSpace((string?)svg.Attribute("viewBox")));
        Assert.Equal("PRJ Proje Makina A.Ş.", svg.Element(ns + "title")?.Value);
    }

    [Fact]
    public void Every_manifest_icon_exists()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(WebRoot, "site.webmanifest")));
        foreach (var icon in manifest.RootElement.GetProperty("icons").EnumerateArray())
        {
            var src = icon.GetProperty("src").GetString()!.TrimStart('/');
            Assert.True(File.Exists(Path.Combine(WebRoot, src)), src);
        }
    }

    [Theory]
    [InlineData("favicon.ico")]
    [InlineData("favicon.svg")]
    [InlineData("images/favicon-96.png")]
    [InlineData("images/apple-touch-icon.png")]
    [InlineData("images/brand/og-default.jpg")]
    public void Icon_file_exists(string path) => Assert.True(File.Exists(Path.Combine(WebRoot, path)), path);

    [Fact]
    public void Every_font_face_points_at_a_shipped_file()
    {
        var urls = Regex.Matches(Tokens, @"url\(""?/fonts/([^"")]+)""?\)").Select(m => m.Groups[1].Value).ToList();
        Assert.NotEmpty(urls);
        Assert.All(urls, file => Assert.True(File.Exists(Path.Combine(WebRoot, "fonts", file)), file));
    }

    [Theory]
    [InlineData("Inter")]
    [InlineData("Barlow")]
    [InlineData("Barlow Condensed")]
    public void Every_turkish_letter_is_in_a_face_of_the_family(string family)
    {
        var faces = Regex.Matches(Tokens, @"@font-face\s*\{(?<body>[^}]*)\}").Select(m => m.Groups["body"].Value)
            .Where(body => Regex.IsMatch(body, $@"font-family:\s*""{Regex.Escape(family)}"""))
            .ToList();
        var ranges = faces
            .SelectMany(body => Regex.Match(body, @"unicode-range:\s*([^;]+);").Groups[1].Value.Split(','))
            .Select(range => range.Trim()[2..].Split('-'))
            .Select(parts => (From: Convert.ToInt32(parts[0], 16), To: Convert.ToInt32(parts[^1], 16)))
            .ToList();

        Assert.All("çÇğĞıİöÖşŞüÜ", letter => Assert.Contains(ranges, r => letter >= r.From && letter <= r.To));
    }

    private static string Token(string name) =>
        Regex.Match(Tokens, $@"{Regex.Escape(name)}\s*:\s*(#[0-9A-Fa-f]{{6}})").Groups[1].Value.ToUpperInvariant();

    private static double Contrast(string a, string b)
    {
        var (la, lb) = (Luminance(a), Luminance(b));
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Luminance(string hex)
    {
        double Channel(int offset)
        {
            var c = int.Parse(hex.AsSpan(1 + offset, 2), NumberStyles.HexNumber) / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(0) + 0.7152 * Channel(2) + 0.0722 * Channel(4);
    }
}
