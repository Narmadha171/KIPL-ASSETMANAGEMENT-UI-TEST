using KIPL.AssetManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace KIPL.AssetManagement.Web.Authorization;

/// <summary>
/// Builds an authorization policy on demand for any policy name that looks like a
/// permission, so pages can use [Authorize(Policy = Permissions.ManageInventory)]
/// without every permission being registered up front.
/// </summary>
public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var existing = await base.GetPolicyAsync(policyName);
        if (existing is not null) return existing;

        if (!Permissions.All.Contains(policyName)) return null;

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName))
            .Build();
    }
}
