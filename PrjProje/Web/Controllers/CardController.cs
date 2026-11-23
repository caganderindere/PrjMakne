using System.Text;
using PrjProje.Application.Interfaces;
using PrjProje.Infrastructure.Services.Support;
using Microsoft.AspNetCore.Mvc;

namespace PrjProje.Web.Controllers;

/// <summary>Digital business cards: the page the printed QR code opens, and the contact file it offers to save.</summary>
public class CardController : PublicControllerBase
{
    private readonly IBusinessCardService _cardService;
    private readonly ISiteSettingsService _settingsService;
    private readonly IWebHostEnvironment _environment;

    public CardController(IBusinessCardService cardService, ISiteSettingsService settingsService, IWebHostEnvironment environment)
    {
        _cardService = cardService;
        _settingsService = settingsService;
        _environment = environment;
    }

    public async Task<IActionResult> Show(string slug, CancellationToken cancellationToken)
    {
        KeepPrivate();
        var card = await _cardService.GetAsync(slug, Lang, cancellationToken);
        if (card is null)
        {
            return NotFound();
        }

        await _cardService.CountViewAsync(card.Id, cancellationToken);
        ViewBag.Settings = await _settingsService.GetAsync(cancellationToken);
        return View(card);
    }

    public async Task<IActionResult> VCard(string slug, CancellationToken cancellationToken)
    {
        KeepPrivate();
        var card = await _cardService.GetAsync(slug, Lang, cancellationToken);
        if (card is null)
        {
            return NotFound();
        }

        var settings = await _settingsService.GetAsync(cancellationToken);
        var vcard = BusinessCardFiles.VCard(card, settings.CompanyName, _environment.WebRootPath);
        // Named after the person, not the token, so a forwarded file does not carry the private link either.
        return File(Encoding.UTF8.GetBytes(vcard), "text/vcard; charset=utf-8", $"{card.FullName}.vcf");
    }

    // The address itself is the secret: keep it out of search engines and caches, and never send it on as a referrer.
    private void KeepPrivate()
    {
        var headers = Response.Headers;
        headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
        headers["Referrer-Policy"] = "no-referrer";
        headers.CacheControl = "private, no-store";
    }
}
