using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PrjProje.Tests;

/// <summary>A missing key shows its raw name on the page ("Quote.ServiceMessage"), so every literal key must exist in both languages.</summary>
public class ResourceKeysTests
{
    private static readonly string Root = BrandLeakTests.RepoRoot();

    private static HashSet<string> Keys(string lang) =>
        XDocument.Load(Path.Combine(Root, "PrjProje", "Resources", $"SharedResource.{lang}.resx")).Root!
            .Elements("data").Select(e => (string)e.Attribute("name")!).ToHashSet();

    [Fact]
    public void Both_languages_have_the_same_keys() => Assert.Equal(Keys("tr").Order(), Keys("en").Order());

    [Fact]
    public void Every_key_used_in_code_exists()
    {
        var keys = Keys("tr");
        var source = Path.Combine(Root, "PrjProje");
        var used = Directory.EnumerateFiles(source, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cshtml") || path.EndsWith(".cs"))
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj" or "Areas"))
            .SelectMany(path => Regex.Matches(File.ReadAllText(path), @"\bL\[""([A-Za-z]+\.[A-Za-z0-9.]+)""").Select(m => (Key: m.Groups[1].Value, File: Path.GetFileName(path))))
            .Where(use => !keys.Contains(use.Key))
            .Select(use => $"{use.Key} ({use.File})")
            .Distinct()
            .ToList();

        Assert.Empty(used);
    }
}
