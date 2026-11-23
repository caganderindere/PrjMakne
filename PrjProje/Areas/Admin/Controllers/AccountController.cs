using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models.Admin;
using PrjProje.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace PrjProje.Areas.Admin.Controllers;

public class AccountController : StaffControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogger _activityLogger;

    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IActivityLogger activityLogger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _activityLogger = activityLogger;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToHome(User.IsInRole);
        }

        return View(new LoginModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Çok fazla hatalı deneme yapıldı. Hesabınız 15 dakika süreyle kilitlendi.");
            return View(model);
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
            return View(model);
        }

        await _activityLogger.LogAsync("Login", "User", user.Id, user.Email);
        // The cookie was only just issued, so this request's User has no roles yet: ask the store.
        var roles = await _userManager.GetRolesAsync(user);
        return Url.IsLocalUrl(model.ReturnUrl) && model.ReturnUrl!.StartsWith("/admin", StringComparison.OrdinalIgnoreCase)
            ? LocalRedirect(model.ReturnUrl)
            : RedirectToHome(roles.Contains);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [Route("admin/account/access-denied")]
    public IActionResult AccessDenied() => View();

    [HttpGet]
    public IActionResult ChangePassword()
    {
        ViewBag.Forced = User.HasClaim(PanelClaims.MustChangePassword, "1");
        return View(new ChangePasswordModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
    {
        ViewBag.Forced = User.HasClaim(PanelClaims.MustChangePassword, "1");
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (user.MustChangePassword && model.NewPassword == model.CurrentPassword)
        {
            ModelState.AddModelError(nameof(model.NewPassword), "Yöneticinin verdiği geçici şifreden farklı bir şifre seçin.");
            return View(model);
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        if (user.MustChangePassword)
        {
            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);
        }

        // Re-issues the cookie, so the forced-change claim goes away with the flag.
        await _signInManager.RefreshSignInAsync(user);
        await _activityLogger.LogAsync("PasswordChanged", "User", user.Id, user.Email);
        Success("Şifreniz güncellendi.");
        return RedirectToHome(User.IsInRole);
    }
}
