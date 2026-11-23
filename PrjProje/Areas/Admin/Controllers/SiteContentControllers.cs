using System.Text;
using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models.Admin;
using PrjProje.Infrastructure.Services.Support;
using PrjProje.Web.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PrjProje.Areas.Admin.Controllers;

public class ReferencesController : AdminControllerBase
{
    private readonly IReferenceAdminService _referenceService;

    public ReferencesController(IReferenceAdminService referenceService)
    {
        _referenceService = referenceService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _referenceService.GetListAsync(cancellationToken));

    public IActionResult Create() => View("Form", new ReferenceEditModel());

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await _referenceService.GetForEditAsync(id, cancellationToken);
        return model is null ? NotFound() : View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(LargeUploadLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = LargeUploadLimit)]
    public async Task<IActionResult> Save(ReferenceEditModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await _referenceService.SaveAsync(model, cancellationToken);
            if (result.Succeeded)
            {
                Success("Referans kaydedildi.");
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
        }

        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        ResultRedirect(await _referenceService.DeleteAsync(id, cancellationToken), "Referans silindi.");
}

public class AnnouncementsController : AdminControllerBase
{
    private readonly IAnnouncementAdminService _announcementService;

    public AnnouncementsController(IAnnouncementAdminService announcementService)
    {
        _announcementService = announcementService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _announcementService.GetListAsync(cancellationToken));

    public IActionResult Create() => View("Form", new AnnouncementEditModel());

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await _announcementService.GetForEditAsync(id, cancellationToken);
        return model is null ? NotFound() : View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(LargeUploadLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = LargeUploadLimit)]
    public async Task<IActionResult> Save(AnnouncementEditModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await _announcementService.SaveAsync(model, cancellationToken);
            if (result.Succeeded)
            {
                Success("Duyuru kaydedildi.");
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
        }

        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        ResultRedirect(await _announcementService.DeleteAsync(id, cancellationToken), "Duyuru silindi.");
}

[Authorize(Policy = Policies.CardManagement)]
public class BusinessCardsController : StaffControllerBase
{
    private readonly IBusinessCardAdminService _cardService;

    public BusinessCardsController(IBusinessCardAdminService cardService)
    {
        _cardService = cardService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _cardService.GetListAsync(cancellationToken));

    public IActionResult Create() => View("Form", new BusinessCardEditModel());

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await _cardService.GetForEditAsync(id, cancellationToken);
        return model is null ? NotFound() : View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(LargeUploadLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = LargeUploadLimit)]
    public async Task<IActionResult> Save(BusinessCardEditModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await _cardService.SaveAsync(model, cancellationToken);
            if (result.Succeeded)
            {
                Success("Kartvizit kaydedildi.");
                return RedirectToAction(nameof(Edit), new { id = result.Value });
            }

            AddErrors(result);
        }

        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        ResultRedirect(await _cardService.DeleteAsync(id, cancellationToken), "Kartvizit silindi.");

    public async Task<IActionResult> QrPng(int id, bool download, CancellationToken cancellationToken)
    {
        var card = await _cardService.GetForEditAsync(id, cancellationToken);
        if (card is null)
        {
            return NotFound();
        }

        var png = BusinessCardFiles.QrPng(CardUrl(card.Slug));
        return download ? File(png, "image/png", $"kartvizit-qr-{card.Slug}.png") : File(png, "image/png");
    }

    public async Task<IActionResult> QrSvg(int id, CancellationToken cancellationToken)
    {
        var card = await _cardService.GetForEditAsync(id, cancellationToken);
        return card is null
            ? NotFound()
            : File(Encoding.UTF8.GetBytes(BusinessCardFiles.QrSvg(CardUrl(card.Slug))), "image/svg+xml", $"kartvizit-qr-{card.Slug}.svg");
    }

    private string CardUrl(string? slug) => Url.Absolute(SiteRoutes.Path(SiteRoutes.BusinessCard, Languages.Default, slug));
}

[Authorize(Policy = Policies.Marketing)]
public class FairsController : StaffControllerBase
{
    private readonly IFairAdminService _fairService;

    public FairsController(IFairAdminService fairService)
    {
        _fairService = fairService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _fairService.GetListAsync(cancellationToken));

    public IActionResult Create() => View("Form", new FairEditModel());

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await _fairService.GetForEditAsync(id, cancellationToken);
        return model is null ? NotFound() : View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(LargeUploadLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = LargeUploadLimit)]
    public async Task<IActionResult> Save(FairEditModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await _fairService.SaveAsync(model, cancellationToken);
            if (result.Succeeded)
            {
                Success("Fuar kaydedildi.");
                return RedirectToAction(nameof(Edit), new { id = result.Value });
            }

            AddErrors(result);
        }

        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        ResultRedirect(await _fairService.DeleteAsync(id, cancellationToken), "Fuar silindi.");

    public async Task<IActionResult> Leads(int id, CancellationToken cancellationToken)
    {
        var model = await _fairService.GetLeadsAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    public async Task<IActionResult> LeadsCsv(int id, CancellationToken cancellationToken)
    {
        var model = await _fairService.GetLeadsAsync(id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        // Semicolons and a BOM so Excel on a Turkish Windows splits the columns and reads the characters correctly.
        var csv = new StringBuilder("﻿E-posta;Tanıtım izni;İzin tarihi;Dil;Kayıt tarihi\r\n");
        foreach (var lead in model.Leads)
        {
            csv.Append(CsvCell(lead.Email)).Append(';')
                .Append(lead.MarketingConsent ? "Evet" : "Hayır").Append(';')
                .Append(lead.MarketingConsentDate?.ToString("dd.MM.yyyy HH:mm")).Append(';')
                .Append(lead.LanguageCode).Append(';')
                .Append(lead.CreatedDate?.ToString("dd.MM.yyyy HH:mm")).Append("\r\n");
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"fuar-{model.FairSlug}-eposta.csv");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLead(int id, int fairId, CancellationToken cancellationToken) =>
        ResultRedirect(await _fairService.DeleteLeadAsync(id, cancellationToken), "E-posta silindi.", nameof(Leads), new { id = fairId });

    public async Task<IActionResult> QrPng(int id, bool download, CancellationToken cancellationToken)
    {
        var fair = await _fairService.GetForEditAsync(id, cancellationToken);
        if (fair is null)
        {
            return NotFound();
        }

        var png = BusinessCardFiles.QrPng(FairUrl(fair.Slug));
        return download ? File(png, "image/png", $"fuar-qr-{fair.Slug}.png") : File(png, "image/png");
    }

    public async Task<IActionResult> QrSvg(int id, CancellationToken cancellationToken)
    {
        var fair = await _fairService.GetForEditAsync(id, cancellationToken);
        return fair is null
            ? NotFound()
            : File(Encoding.UTF8.GetBytes(BusinessCardFiles.QrSvg(FairUrl(fair.Slug))), "image/svg+xml", $"fuar-qr-{fair.Slug}.svg");
    }

    private string FairUrl(string? slug) => Url.Absolute(SiteRoutes.Path(SiteRoutes.Fair, Languages.Default, slug));

    // The address comes from a public form and Excel would run a cell starting with these as a formula.
    private static string CsvCell(string value) =>
        "\"" + ("=+-@\t\r".Contains(value[0]) ? "'" + value : value).Replace("\"", "\"\"") + "\"";
}

public class ContentTextsController : AdminControllerBase
{
    private readonly IContentTextAdminService _contentTextService;

    public ContentTextsController(IContentTextAdminService contentTextService)
    {
        _contentTextService = contentTextService;
    }

    public async Task<IActionResult> Index(string? group, string? q, CancellationToken cancellationToken) =>
        View(await _contentTextService.GetPageAsync(group, q, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(string? group, string? q, List<ContentTextRow> rows, CancellationToken cancellationToken)
    {
        var result = await _contentTextService.SaveAsync(rows, cancellationToken);
        if (result.Succeeded)
        {
            Success(result.Value == 0 ? "Değişiklik yapılmadı." : $"{result.Value} metin güncellendi.");
        }
        else
        {
            Error(string.Join(" ", result.Errors));
        }

        return RedirectToAction(nameof(Index), new { group, q });
    }
}
