using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Domain.Enums;

namespace KIPL.AssetManagement.Web.Infrastructure;

public record NavItem(string Page, string Label, string Icon, bool IsDivider = false, string? BadgeKey = null, string? RequiredPermission = null)
{
    public static NavItem Divider() => new("", "", "", true);
}

/// <summary>
/// Builds the left rail per role — the server-side equivalent of the prototype's
/// roleConfig table, with the same ordering and labels.
/// </summary>
public static class Navigation
{
    /// <summary>
    /// Builds the rail for the signed-in user, then drops any item whose gating
    /// permission has been switched off for their role on the Roles & permissions
    /// page — the rail always reflects the live grant, not just the role's defaults.
    /// </summary>
    public static IReadOnlyList<NavItem> For(string role, ICurrentUserService currentUser)
    {
        var visible = RawFor(role)
            .Where(item => item.IsDivider || item.RequiredPermission is null || currentUser.HasPermission(item.RequiredPermission))
            .ToList();

        // Drop a divider that ended up with nothing left on one side of it.
        var result = new List<NavItem>();
        foreach (var item in visible)
        {
            if (item.IsDivider && (result.Count == 0 || result[^1].IsDivider)) continue;
            result.Add(item);
        }
        while (result.Count > 0 && result[^1].IsDivider) result.RemoveAt(result.Count - 1);

        return result;
    }

    private static IReadOnlyList<NavItem> RawFor(string role) => role switch
    {
        Roles.Admin => new[]
        {
            new NavItem("/Ops/Dashboard", "Dashboard", "grid"),
            new NavItem("/Ops/Inventory", "Inventory", "package", RequiredPermission: Permissions.ViewInventory),
            new NavItem("/Ops/Roles", "Roles & Permissions", "shield", RequiredPermission: Permissions.ManageRoles),
            new NavItem("/Ops/Users", "Users", "users", RequiredPermission: Permissions.ManageUsers),
            new NavItem("/Ops/Audit", "Audit Trail", "filetext", RequiredPermission: Permissions.ViewAudit),
            NavItem.Divider(),
            new NavItem("/Ops/HrApprovals", "Approvals", "checkcheck", RequiredPermission: Permissions.ApproveRequests),
            new NavItem("/Ops/ReturnTracking", "Return Tracking", "inbox", RequiredPermission: Permissions.TrackReturns),
            new NavItem("/Ops/ServiceQueue", "Service Queue", "wrench", RequiredPermission: Permissions.ManageInventory),
        },

        Roles.ITAssetManager => new[]
        {
            new NavItem("/Ops/Dashboard", "Dashboard", "grid"),
            new NavItem("/Ops/Inventory", "Inventory", "package", RequiredPermission: Permissions.ViewInventory),
            new NavItem("/Me/Assets", "My Assets", "laptop"),
            new NavItem("/Me/Requests", "My Requests", "search", BadgeKey: "mine", RequiredPermission: Permissions.RaiseRequests),
            new NavItem("/Me/Returns", "My Return", "undo", RequiredPermission: Permissions.RaiseReturns),
            new NavItem("/Ops/ExecApprovals", "Approvals", "checkcheck", RequiredPermission: Permissions.ClaimRequests),
            new NavItem("/Ops/ReturnTracking", "Return Tracking", "inbox", RequiredPermission: Permissions.TrackReturns),
            new NavItem("/Ops/ServiceQueue", "Service Queue", "wrench", RequiredPermission: Permissions.ManageInventory),
            new NavItem("/Ops/Audit", "Audit Trail", "filetext", RequiredPermission: Permissions.ViewAudit),
        },

        Roles.ITAssetExecutive => new[]
        {
            new NavItem("/Ops/Dashboard", "Dashboard", "grid"),
            new NavItem("/Ops/Inventory", "Inventory", "package", RequiredPermission: Permissions.ViewInventory),
            new NavItem("/Me/Assets", "My Assets", "laptop"),
            new NavItem("/Me/Requests", "My Requests", "search", BadgeKey: "mine", RequiredPermission: Permissions.RaiseRequests),
            new NavItem("/Me/Returns", "My Return", "undo", RequiredPermission: Permissions.RaiseReturns),
            new NavItem("/Ops/ExecApprovals", "Approvals", "checkcheck", RequiredPermission: Permissions.ClaimRequests),
            new NavItem("/Ops/ReturnTracking", "Return Tracking", "inbox", RequiredPermission: Permissions.TrackReturns),
            new NavItem("/Ops/ServiceQueue", "Service Queue", "wrench", RequiredPermission: Permissions.ManageInventory),
        },

        Roles.HR => new[]
        {
            new NavItem("/Ops/Dashboard", "Dashboard", "grid"),
            new NavItem("/Ops/Inventory", "Inventory", "package", RequiredPermission: Permissions.ViewInventory),
            new NavItem("/Me/Assets", "My Assets", "laptop"),
            new NavItem("/Me/Requests", "My Requests", "search", BadgeKey: "mine", RequiredPermission: Permissions.RaiseRequests),
            new NavItem("/Me/Returns", "My Return", "undo", RequiredPermission: Permissions.RaiseReturns),
            new NavItem("/Ops/HrApprovals", "Approvals", "checkcheck", RequiredPermission: Permissions.ApproveRequests),
        },

        Roles.ReportingManager => new[]
        {
            new NavItem("/Me/Dashboard", "Dashboard", "grid"),
            new NavItem("/Me/Assets", "My Assets", "laptop"),
            new NavItem("/Me/Requests", "My Requests", "search", BadgeKey: "mine", RequiredPermission: Permissions.RaiseRequests),
            new NavItem("/Me/Returns", "My Returns", "undo", RequiredPermission: Permissions.RaiseReturns),
            NavItem.Divider(),
            new NavItem("/Team/Approvals", "Team Approvals", "checkcheck", BadgeKey: "team", RequiredPermission: Permissions.ApproveTeamRequests),
            new NavItem("/Team/Assets", "Team Assets", "users", RequiredPermission: Permissions.ViewTeam),
        },

        _ => new[]
        {
            new NavItem("/Me/Dashboard", "Dashboard", "grid"),
            new NavItem("/Me/Assets", "My Assets", "laptop"),
            new NavItem("/Me/Requests", "My Requests", "search", BadgeKey: "mine", RequiredPermission: Permissions.RaiseRequests),
            new NavItem("/Me/Returns", "My Returns", "undo", RequiredPermission: Permissions.RaiseReturns),
        }
    };

    public static string RailSubtitle(string role) => role switch
    {
        Roles.Admin => "ASSET MANAGEMENT",
        Roles.ITAssetManager or Roles.ITAssetExecutive => "ASSET OPS",
        Roles.HR => "HR PORTAL",
        _ => "MY ASSETS"
    };

    public static string LandingPage(string role) => role switch
    {
        Roles.Admin or Roles.ITAssetManager or Roles.ITAssetExecutive or Roles.HR => "/Ops/Dashboard",
        _ => "/Me/Dashboard"
    };
}
