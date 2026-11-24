using System.Security.Claims;
using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models;
using PrjProje.Application.Models.Admin;
using PrjProje.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PrjProje.Areas.Admin.Controllers;

[Authorize(Policy = Policies.ContentManagement)]
public class MessagesController : StaffControllerBase
{
    private readonly IContactAdminService _contactService;

    public MessagesController(IContactAdminService contactService)
    {
        _contactService = contactService;
    }

    public async Task<IActionResult> Index(bool? unread, int page = 1, CancellationToken cancellationToken = default)
    {
        ViewBag.Unread = unread;
        return View(await _contactService.GetListAsync(unread, page, cancellationToken));
    }

    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var message = await _contactService.GetAndMarkReadAsync(id, cancellationToken);
        return message is null ? NotFound() : View(message);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        ResultRedirect(await _contactService.DeleteAsync(id, cancellationToken), "Mesaj silindi.");
}

[Authorize(Policy = Policies.ContentManagement)]
public class QuotesController : StaffControllerBase
{
    private readonly IQuoteAdminService _quoteService;

    public QuotesController(IQuoteAdminService quoteService)
    {
        _quoteService = quoteService;
    }

    public async Task<IActionResult> Index(QuoteStatus? status, int page = 1, CancellationToken cancellationToken = default)
    {
        ViewBag.Status = status;
        return View(await _quoteService.GetListAsync(status, page, cancellationToken));
    }

    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var quote = await _quoteService.GetAsync(id, cancellationToken);
        return quote is null ? NotFound() : View(quote);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(QuoteStatusUpdateModel model, CancellationToken cancellationToken) =>
        ResultRedirect(await _quoteService.UpdateStatusAsync(model, cancellationToken), "Teklif durumu güncellendi.", nameof(Detail), new { id = model.Id });

    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var attachment = await _quoteService.GetAttachmentAsync(id, cancellationToken);
        if (attachment is null)
        {
            return NotFound();
        }

        // Always served as a download with a neutral content type, never rendered inline.
        return PhysicalFile(attachment.PhysicalPath, "application/octet-stream", attachment.DownloadName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        ResultRedirect(await _quoteService.DeleteAsync(id, cancellationToken), "Teklif talebi silindi.");
}

[Authorize(Policy = Policies.UserManagement)]
public class UsersController : AdminControllerBase
{
    private readonly IUserAdminService _userService;

    public UsersController(IUserAdminService userService)
    {
        _userService = userService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _userService.GetListAsync(cancellationToken));

    public IActionResult Create() => View("Form", new UserEditModel());

    public async Task<IActionResult> Edit(string id)
    {
        var model = await _userService.GetForEditAsync(id);
        return model is null ? NotFound() : View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(UserEditModel model)
    {
        if (ModelState.IsValid)
        {
            var result = string.IsNullOrEmpty(model.Id)
                ? await _userService.CreateAsync(model)
                : await _userService.UpdateAsync(model, CurrentUserId);

            if (result.Succeeded)
            {
                Success("Kullanıcı kaydedildi.");
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
        }

        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id) =>
        ResultRedirect(await _userService.DeleteAsync(id, CurrentUserId), "Kullanıcı silindi.");
}

[Authorize(Policy = Policies.FullAccess)]
public class SettingsController : AdminControllerBase
{
    private readonly ISiteSettingsService _settingsService;
    private readonly IFileStorageService _fileStorage;
    private readonly IActivityLogger _activityLogger;

    public SettingsController(ISiteSettingsService settingsService, IFileStorageService fileStorage, IActivityLogger activityLogger)
    {
        _settingsService = settingsService;
        _fileStorage = fileStorage;
        _activityLogger = activityLogger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _settingsService.GetAsync(cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(LargeUploadLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = LargeUploadLimit)]
    public async Task<IActionResult> Index(
        SiteSettingsDto model,
        IFormFile? heroImageFile,
        bool removeHeroImage,
        CancellationToken cancellationToken)
    {
        var current = await _settingsService.GetAsync(cancellationToken);
        model.HeroImage = current.HeroImage;

        if (!string.IsNullOrWhiteSpace(model.MapEmbedUrl) && !model.MapEmbedUrl.StartsWith("https://www.google.com/maps", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.MapEmbedUrl), "Harita adresi https://www.google.com/maps ile başlamalıdır.");
        }

        // The ID ends up in a script URL on every public page, so only the GA4 format is accepted.
        model.GoogleAnalyticsId = model.GoogleAnalyticsId?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(model.GoogleAnalyticsId) && !System.Text.RegularExpressions.Regex.IsMatch(model.GoogleAnalyticsId, "^G-[A-Z0-9]{4,20}$"))
        {
            ModelState.AddModelError(nameof(model.GoogleAnalyticsId), "Ölçüm kimliği G- ile başlamalıdır (ör. G-AB12CD34EF).");
        }

        model.HeroVideo ??= string.Empty;
        if (model.HeroVideo.Length > 0 && (!model.HeroVideo.StartsWith('/') || !model.HeroVideo.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(model.HeroVideo), "Video yolu / ile başlamalı ve .mp4 ile bitmelidir (ör. /videos/hero.mp4).");
        }

        model.FoundedYear = model.FoundedYear?.Trim();
        if (!string.IsNullOrEmpty(model.FoundedYear) && (!int.TryParse(model.FoundedYear, out var year) || year < 1900 || year > DateTime.Today.Year))
        {
            ModelState.AddModelError(nameof(model.FoundedYear), $"Kuruluş yılı 1900 ile {DateTime.Today.Year} arasında bir yıl olmalıdır.");
        }

        model.CompletedProjects = model.CompletedProjects?.Trim();
        if (!string.IsNullOrEmpty(model.CompletedProjects) && (!int.TryParse(model.CompletedProjects, out var projects) || projects < 1))
        {
            ModelState.AddModelError(nameof(model.CompletedProjects), "Proje sayısı pozitif bir tam sayı olmalıdır.");
        }

        if (heroImageFile is { Length: > 0 })
        {
            var upload = await _fileStorage.SaveImageAsync(heroImageFile, ImageFolder.Site, cancellationToken);
            if (upload.Succeeded)
            {
                _fileStorage.DeletePublicFile(current.HeroImage);
                model.HeroImage = upload.Value!.RelativePath;
            }
            else
            {
                ModelState.AddModelError(string.Empty, string.Join(" ", upload.Errors));
            }
        }
        else if (removeHeroImage)
        {
            _fileStorage.DeletePublicFile(current.HeroImage);
            model.HeroImage = null;
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _settingsService.SaveAsync(model, cancellationToken);
        await _activityLogger.LogAsync("Updated", "SiteSettings", null, null, cancellationToken);
        Success("Site ayarları kaydedildi.");
        return RedirectToAction(nameof(Index));
    }
}
