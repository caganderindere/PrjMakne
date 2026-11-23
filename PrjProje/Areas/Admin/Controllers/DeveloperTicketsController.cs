using System.Security.Claims;
using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models.Admin;
using PrjProje.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PrjProje.Areas.Admin.Controllers;

/// <summary>Every panel user can report to the developer; only admins see all reports and manage them.</summary>
public class DeveloperTicketsController : AdminControllerBase
{
    private readonly IDeveloperTicketService _ticketService;

    public DeveloperTicketsController(IDeveloperTicketService ticketService)
    {
        _ticketService = ticketService;
    }

    private TicketViewer Viewer => new(User.FindFirstValue(ClaimTypes.NameIdentifier)!, Roles.HasFullAccess(User));

    public async Task<IActionResult> Index(TicketStatus? status, int page = 1, CancellationToken cancellationToken = default)
    {
        ViewBag.Status = status;
        return View(await _ticketService.GetListAsync(Viewer, status, page, cancellationToken));
    }

    public IActionResult Create() => View(new TicketCreateModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TicketCreateModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _ticketService.CreateAsync(Viewer, User.Identity?.Name ?? "-", model, cancellationToken);
        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(model);
        }

        Success("Bildiriminiz geliştiriciye iletildi.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var ticket = await _ticketService.GetAsync(Viewer, id, cancellationToken);
        return ticket is null ? NotFound() : View(ticket);
    }

    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var attachment = await _ticketService.GetAttachmentAsync(Viewer, id, cancellationToken);
        if (attachment is null)
        {
            return NotFound();
        }

        // Always served as a download with a neutral content type, never rendered inline.
        return PhysicalFile(attachment.PhysicalPath, "application/octet-stream", attachment.DownloadName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.FullAccess)]
    public async Task<IActionResult> Update(TicketUpdateModel model, CancellationToken cancellationToken) =>
        ResultRedirect(await _ticketService.UpdateAsync(model, cancellationToken), "Bildirim güncellendi.", nameof(Detail), new { id = model.Id });

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.FullAccess)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        ResultRedirect(await _ticketService.DeleteAsync(id, cancellationToken), "Bildirim silindi.");
}
