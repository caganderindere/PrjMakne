using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PrjProje.Areas.Admin.Controllers;

/// <summary>Marketing summary (quotes, messages, fair leads, WhatsApp) and the Google Analytics report from the Looker Studio address in the site settings.</summary>
[Authorize(Policy = Policies.Marketing)]
public class AnalyticsController : StaffControllerBase
{
    private readonly ISiteSettingsService _settingsService;
    private readonly IDashboardService _dashboardService;

    public AnalyticsController(ISiteSettingsService settingsService, IDashboardService dashboardService)
    {
        _settingsService = settingsService;
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewBag.Settings = await _settingsService.GetAsync(cancellationToken);
        return View(await _dashboardService.GetMarketingAsync(cancellationToken));
    }
}
