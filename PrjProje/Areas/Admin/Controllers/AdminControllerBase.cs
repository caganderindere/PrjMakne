using PrjProje.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PrjProje.Areas.Admin.Controllers;

/// <summary>Content pages: admins and editors. Pages open to other roles derive from <see cref="StaffControllerBase"/>.</summary>
[Authorize(Policy = Policies.ContentManagement)]
public abstract class AdminControllerBase : StaffControllerBase
{
}

/// <summary>Panel helpers without the content-management requirement; each subclass names its own policy.</summary>
[Area("Admin")]
[Authorize(Policy = Policies.Staff)]
public abstract class StaffControllerBase : Controller
{
    /// <summary>A user signing in with a password an admin set sees nothing but the password change page until they pick their own.</summary>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (this is not AccountController && User.HasClaim(PanelClaims.MustChangePassword, "1"))
        {
            context.Result = RedirectToAction("ChangePassword", "Account");
            return;
        }

        base.OnActionExecuting(context);
    }

    /// <summary>Where a user lands after signing in: both panel roles start on the dashboard.</summary>
    protected IActionResult RedirectToHome(Func<string, bool> isInRole) => RedirectToAction("Index", "Dashboard");

    protected const long LargeUploadLimit = 100 * 1024 * 1024;

    protected void Success(string message) => TempData["AdminSuccess"] = message;

    protected void Error(string message) => TempData["AdminError"] = message;

    protected void AddErrors(ServiceResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
    }

    protected IActionResult ResultRedirect(ServiceResult result, string successMessage, string action = "Index", object? routeValues = null)
    {
        if (result.Succeeded)
        {
            Success(successMessage);
        }
        else
        {
            Error(string.Join(" ", result.Errors));
        }

        return RedirectToAction(action, routeValues);
    }
}
