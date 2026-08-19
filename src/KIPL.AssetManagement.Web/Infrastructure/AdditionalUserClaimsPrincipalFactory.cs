using System.Security.Claims;
using KIPL.AssetManagement.Domain.Enums;
using KIPL.AssetManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using KIPL.AssetManagement.Infrastructure.Persistence;

namespace KIPL.AssetManagement.Web.Infrastructure;

/// <summary>
/// Stamps the auth cookie with the user's display name, Employee id and the
/// permission claims of their role, so authorization needs no database round trip.
/// </summary>
public class AdditionalUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    private readonly ApplicationDbContext _db;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public AdditionalUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext db,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
        _db = db;
        _roleManager = roleManager;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        identity.AddClaim(new Claim("full_name", user.FullName));

        var employeeId = user.EmployeeId
            ?? await _db.Employees.Where(e => e.IdentityUserId == user.Id)
                                  .Select(e => (int?)e.Id).FirstOrDefaultAsync();

        if (employeeId.HasValue)
            identity.AddClaim(new Claim(CurrentUserService.EmployeeIdClaim, employeeId.Value.ToString()));

        foreach (var roleName in await UserManager.GetRolesAsync(user))
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null) continue;

            foreach (var claim in await _roleManager.GetClaimsAsync(role))
            {
                if (claim.Type != Permissions.ClaimType) continue;
                if (identity.HasClaim(claim.Type, claim.Value)) continue;
                identity.AddClaim(claim);
            }
        }

        return identity;
    }
}
