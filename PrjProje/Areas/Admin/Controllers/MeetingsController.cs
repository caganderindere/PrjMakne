using System.Security.Claims;
using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models.Admin;
using PrjProje.Infrastructure.Services.Support;
using PrjProje.Web.Routing;
using Microsoft.AspNetCore.Mvc;

namespace PrjProje.Areas.Admin.Controllers;

/// <summary>
/// Meeting minutes: every panel user can write them, but a given minutes document opens only for its writer and the
/// panel users ticked as participants (the service enforces this; others get 404 as if it did not exist).
/// </summary>
public class MeetingsController : StaffControllerBase
{
    private readonly IMeetingMinuteService _minutes;

    public MeetingsController(IMeetingMinuteService minutes)
    {
        _minutes = minutes;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public async Task<IActionResult> Index(string? q, int page = 1, CancellationToken cancellationToken = default)
    {
        ViewBag.Search = q;
        return View(await _minutes.GetListAsync(UserId, q, page, cancellationToken));
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        // The writer usually attended, so they start ticked.
        var model = new MeetingMinuteEditModel { MeetingDate = now.Date.AddHours(now.Hour), ParticipantUserIds = [UserId] };
        return await FormAsync(model, cancellationToken);
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await _minutes.GetForEditAsync(id, UserId, cancellationToken);
        return model is null ? NotFound() : await FormAsync(model, cancellationToken);
    }

    private async Task<IActionResult> FormAsync(MeetingMinuteEditModel model, CancellationToken cancellationToken)
    {
        ViewBag.Users = await _minutes.GetUserOptionsAsync(model.ParticipantUserIds, cancellationToken);
        ViewBag.ParticipantSuggestions = await _minutes.GetParticipantSuggestionsAsync(cancellationToken);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(MeetingMinuteEditModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var userName = User.FindFirst(PanelClaims.FullName)?.Value ?? User.Identity?.Name ?? "-";
            var result = await _minutes.SaveAsync(model, UserId, userName, cancellationToken);
            if (result.Succeeded)
            {
                // Straight to the filled-in document, ready to print.
                return RedirectToAction(nameof(Print), new { id = result.Value });
            }

            AddErrors(result);
        }

        return await FormAsync(model, cancellationToken);
    }

    public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
    {
        var minute = await _minutes.GetAsync(id, UserId, cancellationToken);
        if (minute is null)
        {
            return NotFound();
        }

        // Scanning the printed code opens this record in the panel (after sign-in, for its participants), so a paper
        // copy can be checked against the original.
        ViewBag.QrSvg = BusinessCardFiles.QrSvgScalable(Url.Absolute(Url.Action(nameof(Print), new { id })!));
        return View(minute);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        ResultRedirect(await _minutes.DeleteAsync(id, UserId, cancellationToken), "Tutanak silindi.");
}
