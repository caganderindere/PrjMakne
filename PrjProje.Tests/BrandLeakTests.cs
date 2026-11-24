namespace PrjProje.Tests;

/// <summary>
/// The site was bootstrapped from another customer's codebase. No trace of that project may ship,
/// so every text file in the repository is scanned for its name.
/// </summary>
public class BrandLeakTests
{
    private static readonly string[] ScannedFolders = ["PrjProje", "PrjProje.Tests", "tools", "content"];
    private static readonly string[] TextExtensions =
        [".cs", ".cshtml", ".css", ".js", ".json", ".resx", ".xml", ".config", ".csproj", ".webmanifest", ".txt", ".svg", ".html", ".md"];

    [Fact]
    public void No_source_file_mentions_the_reference_project()
    {
        var root = RepoRoot();
        // Built from parts so this file does not match itself: the old brand, its material and its facility claim.
        string[] forbidden = [string.Concat("asp", "rubber"), string.Concat("kau", "çuk"), string.Concat("25.000 m", "²")];
        var offenders = ScannedFolders
            .Select(folder => Path.Combine(root, folder))
            .Where(Directory.Exists)
            .SelectMany(dir => Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            .Where(path => !IsBuildOutput(path) && TextExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
            .Where(path => forbidden.Any(term => File.ReadAllText(path).Contains(term, StringComparison.OrdinalIgnoreCase)
                                                 || Path.GetFileName(path).Contains(term, StringComparison.OrdinalIgnoreCase)))
            .Select(path => Path.GetRelativePath(root, path))
            .ToList();

        Assert.Empty(offenders);
    }

    private static bool IsBuildOutput(string path) =>
        path.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj" or "node_modules" or "lib");

    internal static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "PrjProje.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("PrjProje.sln not found above the test output folder.");
    }
}
