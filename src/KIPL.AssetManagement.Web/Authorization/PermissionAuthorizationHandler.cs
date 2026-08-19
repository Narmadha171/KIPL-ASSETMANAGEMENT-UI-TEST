using KIPL.AssetManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace KIPL.AssetManagement.Web.Authorization;

/// <summary>
/// Succeeds when the signed-in principal carries a matching "permission" claim.
/// Those claims arrive from the role claims seeded out of the RolePermissions table.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return Task.CompletedTask;

        // Administrators bypass the matrix entirely so the system can never lock itself out.
        if (context.User.IsInRole(Roles.Admin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var granted = context.User.Claims
            .Any(c => c.Type == Permissions.ClaimType && c.Value == requirement.Permission);

        if (granted) context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
