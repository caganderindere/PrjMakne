namespace PrjProje.Application.Common;

/// <summary>
/// Groups the export countries (ISO codes from the admin settings) into the regions shown as tabs on the home page.
/// The public page shows Home.ExportRegion.{key} from the resources; a code not listed here falls under "other".
/// </summary>
public static class ExportRegions
{
    public const string Other = "other";

    public static readonly IReadOnlyList<string> Order = new[] { "europe", "middleeast", "africa", "asia", Other };

    private static readonly IReadOnlyDictionary<string, string> RegionOf = new Dictionary<string, string>
    {
        ["DE"] = "europe", ["FR"] = "europe", ["IT"] = "europe", ["NL"] = "europe", ["BE"] = "europe", ["PL"] = "europe",
        ["RO"] = "europe", ["BG"] = "europe", ["GR"] = "europe", ["GB"] = "europe", ["ES"] = "europe", ["PT"] = "europe",
        ["RU"] = "europe", ["UA"] = "europe", ["AT"] = "europe", ["CH"] = "europe", ["CZ"] = "europe", ["HU"] = "europe",
        ["RS"] = "europe", ["NO"] = "europe", ["SE"] = "europe", ["FI"] = "europe", ["DK"] = "europe",
        ["IQ"] = "middleeast", ["SA"] = "middleeast", ["AE"] = "middleeast", ["QA"] = "middleeast", ["KW"] = "middleeast",
        ["JO"] = "middleeast", ["LB"] = "middleeast", ["IR"] = "middleeast",
        ["EG"] = "africa", ["DZ"] = "africa", ["MA"] = "africa", ["LY"] = "africa", ["TN"] = "africa", ["NG"] = "africa", ["ZA"] = "africa",
        ["GE"] = "asia", ["AZ"] = "asia", ["KZ"] = "asia", ["UZ"] = "asia", ["TM"] = "asia", ["KG"] = "asia",
        ["AF"] = "asia", ["PK"] = "asia", ["MN"] = "asia"
    };

    public static string Of(string code) => RegionOf.GetValueOrDefault(code, Other);

    /// <summary>The regions that have at least one country, in tab order, each with its countries in the admin's order.</summary>
    public static IReadOnlyList<(string Key, IReadOnlyList<string> Codes)> Group(IEnumerable<string> codes)
    {
        var list = codes.ToList();
        return Order
            .Select(key => (Key: key, Codes: (IReadOnlyList<string>)list.Where(code => Of(code) == key).ToList()))
            .Where(region => region.Codes.Count > 0)
            .ToList();
    }
}
