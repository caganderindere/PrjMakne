using System.Security.Claims;
using PrjProje.Application.Common;
using PrjProje.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace PrjProje.Infrastructure.Services.Support;

/// <summary>Puts the user's full name and the forced password change in the sign-in cookie, so the panel needs no database query per page.</summary>
public class PanelClaimsFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public PanelClaimsFactory(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            identity.AddClaim(new Claim(PanelClaims.FullName, user.FullName));
        }

        if (user.MustChangePassword)
        {
            identity.AddClaim(new Claim(PanelClaims.MustChangePassword, "1"));
        }

        return identity;
    }
}
