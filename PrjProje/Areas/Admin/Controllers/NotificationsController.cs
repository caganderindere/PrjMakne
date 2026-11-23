using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PrjProje.Areas.Admin.Controllers;

/// <summary>SMTP settings and who gets an e-mail when a quote, message or job application arrives.</summary>
[Authorize(Policy = Policies.FullAccess)]
public class NotificationsController : AdminControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var settings = await _notifications.GetAsync(cancellationToken);
        return Page(new EmailEditModel
        {
            Host = settings.Host,
            Port = settings.Port,
            UseSsl = settings.UseSsl,
            UserName = settings.UserName,
            From = settings.From,
            SalesTo = settings.SalesTo,
            HrTo = settings.HrTo
        }, settings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(EmailEditModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await _notifications.SaveAsync(model, cancellationToken);
            if (result.Succeeded)
            {
                Success("E-posta ayarları kaydedildi.");
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
        }

        return Page(model, await _notifications.GetAsync(cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Test(string? to, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            Error("Test e-postasının gideceği adresi yazın.");
            return RedirectToAction(nameof(Index));
        }

        var error = await _notifications.SendTestAsync(to, cancellationToken);
        if (error is null)
        {
            Success($"Test e-postası {to} adresine gönderildi.");
        }
        else
        {
            Error(error);
        }

        return RedirectToAction(nameof(Index));
    }

    private IActionResult Page(EmailEditModel model, EmailSettings settings)
    {
        ViewBag.Settings = settings;
        return View("Index", model);
    }
}
