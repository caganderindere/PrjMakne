using System.Globalization;
using PrjProje.Application.Interfaces;
using Microsoft.Extensions.Localization;

namespace PrjProje.Web.Localization;

/// <summary>
/// Wraps the .resx based localizer factory so texts edited in the admin panel
/// ("Sayfa İçerikleri") replace the built-in SharedResource texts on every page.
/// </summary>
public sealed class ContentOverrideLocalizerFactory : IStringLocalizerFactory
{
    private readonly ResourceManagerStringLocalizerFactory _inner;
    private readonly IContentTextStore _store;

    public ContentOverrideLocalizerFactory(ResourceManagerStringLocalizerFactory inner, IContentTextStore store)
    {
        _inner = inner;
        _store = store;
    }

    public IStringLocalizer Create(Type resourceSource)
    {
        var localizer = _inner.Create(resourceSource);
        return resourceSource == typeof(SharedResource) ? new ContentOverrideLocalizer(localizer, _store) : localizer;
    }

    public IStringLocalizer Create(string baseName, string location) => _inner.Create(baseName, location);
}

internal sealed class ContentOverrideLocalizer : IStringLocalizer
{
    private readonly IStringLocalizer _inner;
    private readonly IContentTextStore _store;

    public ContentOverrideLocalizer(IStringLocalizer inner, IContentTextStore store)
    {
        _inner = inner;
        _store = store;
    }

    public LocalizedString this[string name]
    {
        get
        {
            var value = Find(name);
            return value is null ? _inner[name] : new LocalizedString(name, value, resourceNotFound: false);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var value = Find(name);
            if (value is null)
            {
                return _inner[name, arguments];
            }

            try
            {
                return new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, value, arguments), resourceNotFound: false);
            }
            catch (FormatException)
            {
                // An edited text with broken {0} placeholders must not break the page.
                return new LocalizedString(name, value, resourceNotFound: false);
            }
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => _inner.GetAllStrings(includeParentCultures);

    private string? Find(string name) =>
        _store.Find(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, name);
}
