using PrjProje.Application.Interfaces;
using PrjProje.Application.Models;
using PrjProje.Infrastructure.Services.Support;
using PrjProje.Web.Routing;
using PrjProje.Web.Seo;
using PrjProje.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace PrjProje.Web.Controllers;

public class ContactController : PublicControllerBase
{
    private readonly IContactService _contactService;
    private readonly ISiteSettingsService _settingsService;
    private readonly ISpamProtectionService _spamProtection;


    public ContactController(IContactService contactService, ISiteSettingsService settingsService, ISpamProtectionService spamProtection)
    {
        _contactService = contactService;
        _settingsService = settingsService;
        _spamProtection = spamProtection;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        PrepareSeo();
        return View(new ContactViewModel { Settings = await _settingsService.GetAsync(cancellationToken) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index([Bind(Prefix = nameof(ContactViewModel.Form))] ContactFormModel form, CancellationToken cancellationToken)
    {
        if (_spamProtection.IsLikelySpam(Request.Form))
        {
            TempData["FormSuccess"] = L["Contact.Success"].Value;
            return Redirect(Url.Localized(SiteRoutes.Contact));
        }

        if (!ModelState.IsValid)
        {
            PrepareSeo();
            return View(new ContactViewModel { Form = form, Settings = await _settingsService.GetAsync(cancellationToken) });
        }

        await _contactService.SubmitAsync(form, CreateRequestContext(), cancellationToken);
        TempData["FormSuccess"] = L["Contact.Success"].Value;
        return Redirect(Url.Localized(SiteRoutes.Contact));
    }

    /// <summary>Beacon from the WhatsApp button (site.js). Each visitor counts once a day, so reloading cannot inflate it.</summary>
    [HttpPost("api/whatsapp-click")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> WhatsAppClick([FromServices] IMemoryCache cache, CancellationToken cancellationToken)
    {
        var key = $"wa-click:{DateTime.UtcNow:yyyyMMdd}:{HttpContext.Connection.RemoteIpAddress}";
        if (!cache.TryGetValue(key, out _))
        {
            cache.Set(key, true, TimeSpan.FromDays(1));
            await _contactService.RecordWhatsAppClickAsync(cancellationToken);
        }

        return NoContent();
    }

    private void PrepareSeo()
    {
        var seo = SetSeo(L["Contact.MetaTitle"], L["Contact.MetaDescription"], SiteRoutes.Contact);
        seo.Breadcrumbs.Add(new BreadcrumbItem(L["Nav.Contact"], null));
    }
}

public class QuoteController : PublicControllerBase
{
    private const long MaxRequestBytes = 12 * 1024 * 1024;

    private readonly IQuoteService _quoteService;
    private readonly IProductService _productService;
    private readonly ISpamProtectionService _spamProtection;


    private readonly ICapabilityService _capabilityService;

    public QuoteController(IQuoteService quoteService, IProductService productService, ISpamProtectionService spamProtection, ICapabilityService capabilityService)
    {
        _capabilityService = capabilityService;
        _quoteService = quoteService;
        _productService = productService;
        _spamProtection = spamProtection;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? product, string? capability, CancellationToken cancellationToken)
    {
        var form = new QuoteFormModel();
        if (product is > 0)
        {
            var name = await _productService.GetProductNameAsync(product.Value, Lang, cancellationToken);
            if (name is not null)
            {
                form.ProductId = product;
                form.ProductName = name;
                form.Description = L["Quote.ProductMessage", name];
            }
        }
        else if (!string.IsNullOrEmpty(capability)
            && await _capabilityService.GetDetailAsync(Lang, capability, cancellationToken) is { } selected)
        {
            form.Description = L["Quote.CapabilityMessage", selected.Title];
        }

        PrepareSeo(product is > 0 || !string.IsNullOrEmpty(capability));
        return View(await BuildViewModelAsync(form, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<IActionResult> Index([Bind(Prefix = nameof(QuoteViewModel.Form))] QuoteFormModel form, CancellationToken cancellationToken)
    {
        if (_spamProtection.IsLikelySpam(Request.Form))
        {
            TempData["FormSuccess"] = L["Quote.Success"].Value;
            return Redirect(Url.Localized(SiteRoutes.Quote));
        }

        if (ModelState.IsValid)
        {
            var result = await _quoteService.SubmitAsync(form, CreateRequestContext(), cancellationToken);
            if (result.Succeeded)
            {
                TempData["FormSuccess"] = L["Quote.Success"].Value;
                return Redirect(Url.Localized(SiteRoutes.Quote));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError($"{nameof(QuoteViewModel.Form)}.{nameof(QuoteFormModel.Attachment)}", L[error]);
            }
        }

        PrepareSeo(false);
        return View(await BuildViewModelAsync(form, cancellationToken));
    }

    private async Task<QuoteViewModel> BuildViewModelAsync(QuoteFormModel form, CancellationToken cancellationToken) => new()
    {
        Form = form,
        Products = await _productService.GetProductOptionsAsync(Lang, cancellationToken),
        AllowedExtensions = string.Join(",", FileStorageService.AllowedAttachmentExtensions)
    };

    private void PrepareSeo(bool noIndex)
    {
        var seo = SetSeo(L["Quote.MetaTitle"], L["Quote.MetaDescription"], SiteRoutes.Quote);
        seo.NoIndex = noIndex;
        seo.Breadcrumbs.Add(new BreadcrumbItem(L["Nav.Quote"], null));
    }
}

public static class RateLimitPolicies
{
    public const string Forms = "forms";
    public const string Login = "login";
}
