namespace PrjProje.Application.Common;

/// <summary>
/// The areas an applicant can pick on the careers page. The form posts the key; the public page shows
/// Careers.Area.{key} from the resources and the admin panel shows the Turkish name below.
/// </summary>
public static class CareerAreas
{
    public static readonly IReadOnlyDictionary<string, string> Turkish = new Dictionary<string, string>
    {
        ["production"] = "Üretim",
        ["quality"] = "Kalite",
        ["engineering"] = "Mühendislik / Ar-Ge",
        ["sales"] = "Satış ve Dış Ticaret",
        ["office"] = "İdari ve Mali İşler",
        ["intern"] = "Staj",
        ["general"] = "Genel Başvuru"
    };

    public static string Name(string key) => Turkish.GetValueOrDefault(key, key);
}
