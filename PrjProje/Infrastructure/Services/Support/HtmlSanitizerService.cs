using PrjProje.Application.Interfaces;
using Ganss.Xss;

namespace PrjProje.Infrastructure.Services.Support;

public sealed class HtmlSanitizerService : IHtmlSanitizerService
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlSanitizerService()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("https");
        _sanitizer.AllowedSchemes.Add("http");
        _sanitizer.AllowedSchemes.Add("mailto");
        _sanitizer.AllowedAttributes.Add("class");
        _sanitizer.AllowedTags.Remove("form");
        _sanitizer.AllowedTags.Remove("input");
        _sanitizer.AllowedTags.Remove("button");
        _sanitizer.AllowedTags.Remove("textarea");
        _sanitizer.AllowedTags.Remove("select");
        _sanitizer.AllowedAtRules.Clear();
        _sanitizer.PostProcessNode += (_, e) =>
        {
            if (e.Node is AngleSharp.Dom.IElement element && element.TagName.Equals("A", StringComparison.OrdinalIgnoreCase))
            {
                var href = element.GetAttribute("href");
                if (href is not null && href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    element.SetAttribute("rel", "noopener noreferrer");
                }
            }
        };
    }

    public string Sanitize(string? html) => string.IsNullOrWhiteSpace(html) ? string.Empty : _sanitizer.Sanitize(html).Trim();
}
