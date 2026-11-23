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
