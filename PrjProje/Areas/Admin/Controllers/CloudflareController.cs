using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models.Admin;
using PrjProje.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PrjProje.Areas.Admin.Controllers;

[Authorize(Policy = Policies.FullAccess)]
public class CloudflareController : AdminControllerBase
{
    private readonly ICloudflareService _cloudflare;

    public CloudflareController(ICloudflareService cloudflare)
    {
        _cloudflare = cloudflare;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var settings = await _cloudflare.GetAsync(cancellationToken);
        return await PageAsync(new CloudflareEditModel
        {
            UseProxy = settings.UseProxy,
            TurnstileSiteKey = settings.SiteKey,
            ProtectLogin = settings.ProtectLogin,
            ProtectContact = settings.ProtectContact,
            ProtectQuote = settings.ProtectQuote,
            ProtectFair = settings.ProtectFair
        }, cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(CloudflareEditModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await _cloudflare.SaveAsync(model, cancellationToken);
            if (result.Succeeded)
            {
                Success("Cloudflare ayarları kaydedildi.");
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
        }

        return await PageAsync(model, cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestTurnstile(CancellationToken cancellationToken)
    {
        var error = await _cloudflare.TestAsync(HttpContext, cancellationToken);
        if (error is null)
        {
            Success("Turnstile anahtarları çalışıyor.");
        }
        else
        {
            Error(error);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> PageAsync(CloudflareEditModel model, CancellationToken cancellationToken)
    {
        var settings = await _cloudflare.GetAsync(cancellationToken);
        var viaCloudflare = HttpContext.Items.ContainsKey(CloudflareRealIp.ViaProxyItem) || CloudflareRealIp.IsCloudflare(HttpContext.Connection.RemoteIpAddress);
        ViewBag.Status = new CloudflareStatus(
            viaCloudflare,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers["CF-Ray"].ToString(),
            settings.Secret is not null,
            settings.SecretUnreadable,
            settings.TurnstileDisabled,
            settings.SiteKey);
        return View("Index", model);
    }
}
