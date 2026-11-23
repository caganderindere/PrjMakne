using PrjProje.Application.Interfaces;
using PrjProje.Application.Models;
using PrjProje.Web.Routing;
using Microsoft.AspNetCore.Mvc;

namespace PrjProje.Web.Controllers;

/// <summary>Trade fair QR landing: collects the visitor's e-mail, then shows the brochure and the company's accounts.</summary>
public class FairController : PublicControllerBase
{
    private readonly IFairService _fairService;
    private readonly ISiteSettingsService _settingsService;
    private readonly ISpamProtectionService _spamProtection;

    private readonly ICloudflareService _cloudflare;

    public FairController(IFairService fairService, ISiteSettingsService settingsService, ISpamProtectionService spamProtection, ICloudflareService cloudflare)
    {
        _cloudflare = cloudflare;
        _fairService = fairService;
        _settingsService = settingsService;
        _spamProtection = spamProtection;
    }

    [HttpGet]
    public async Task<IActionResult> Show(string slug, CancellationToken cancellationToken)
    {
        var fair = await _fairService.GetAsync(slug, Lang, cancellationToken);
        if (fair is null)
        {
            return NotFound();
        }

        return await FormViewAsync(fair, new FairLeadFormModel(), cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Show(string slug, FairLeadFormModel form, CancellationToken cancellationToken)
    {
        var fair = await _fairService.GetAsync(slug, Lang, cancellationToken);
        if (fair is null)
        {
            return NotFound();
        }

        // A visitor who autofills in under the spam timer still gets the brochure; only the e-mail is dropped.
        if (_spamProtection.IsLikelySpam(Request.Form))
        {
            return BrochureRedirect(fair);
        }

        if (fair.EmailRequired && string.IsNullOrWhiteSpace(form.Email))
        {
            ModelState.AddModelError(nameof(form.Email), L["Validation.Required", L["Form.Email"].Value]);
        }

        if (ModelState.IsValid && !await _cloudflare.VerifyAsync(TurnstileForm.Fair, HttpContext, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, L["Validation.Turnstile"]);
        }

        if (!ModelState.IsValid)
        {
            return await FormViewAsync(fair, form, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(form.Email))
        {
            await _fairService.SaveLeadAsync(fair, form.Email, form.MarketingConsent, Lang, cancellationToken);
        }

        return BrochureRedirect(fair);
    }

    [HttpGet]
    public async Task<IActionResult> Brochure(string slug, CancellationToken cancellationToken)
    {
        var fair = await _fairService.GetAsync(slug, Lang, cancellationToken);
        if (fair is null)
        {
            return NotFound();
        }

        if (fair.EmailRequired && !Request.Cookies.ContainsKey(AccessCookie(fair)))
        {
            return Redirect(Url.Localized(SiteRoutes.Fair, fair.Slug));
        }

        ViewBag.Settings = await _settingsService.GetAsync(cancellationToken);
        return View(fair);
    }

    private async Task<IActionResult> FormViewAsync(FairDto fair, FairLeadFormModel form, CancellationToken cancellationToken)
    {
        ViewBag.Fair = fair;
        ViewBag.Settings = await _settingsService.GetAsync(cancellationToken);
        return View("Show", form);
    }

    private IActionResult BrochureRedirect(FairDto fair)
    {
        Response.Cookies.Append(AccessCookie(fair), "1", new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            MaxAge = TimeSpan.FromDays(30)
        });
        return Redirect(Url.Localized(SiteRoutes.FairBrochure, fair.Slug));
    }

    private static string AccessCookie(FairDto fair) => $"fair_{fair.Id}";
}
