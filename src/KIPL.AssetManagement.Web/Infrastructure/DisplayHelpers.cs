using KIPL.AssetManagement.Domain.Enums;

namespace KIPL.AssetManagement.Web.Infrastructure;

/// <summary>Maps domain enums onto the prototype's pill colours, icons and wording.</summary>
public static class DisplayHelpers
{
    public static string PillClass(AssetStatus status) => status switch
    {
        AssetStatus.InStock => "pill pill-success",
        AssetStatus.Assigned => "pill pill-signal",
        AssetStatus.InTransit => "pill pill-amber",
        AssetStatus.UnderService => "pill pill-amber",
        AssetStatus.Retired => "pill pill-neutral",
        AssetStatus.Lost => "pill pill-danger",
        _ => "pill pill-neutral"
    };

    public static string PillClass(RequestStatus status) => status switch
    {
        RequestStatus.Pending => "pill pill-amber",
        RequestStatus.Unclaimed => "pill pill-amber",
        RequestStatus.Claimed => "pill pill-signal",
        RequestStatus.OnlineOrdered => "pill pill-signal",
        RequestStatus.Dispatched => "pill pill-signal",
        RequestStatus.Completed => "pill pill-success",
        RequestStatus.Rejected => "pill pill-danger",
        RequestStatus.Cancelled => "pill pill-neutral",
        RequestStatus.PendingHrApproval => "pill pill-amber",
        _ => "pill pill-neutral"
    };

    public static string PillClass(ReturnStage stage) => stage switch
    {
        ReturnStage.AwaitingPickup => "pill pill-amber",
        ReturnStage.InTransit => "pill pill-signal",
        ReturnStage.ReceivedForInspection => "pill pill-signal",
        ReturnStage.Inspected => "pill pill-success",
        ReturnStage.Closed => "pill pill-success",
        _ => "pill pill-neutral"
    };

    public static string PillClass(UserStatus status) => status switch
    {
        UserStatus.Active => "pill pill-success",
        UserStatus.Inactive => "pill pill-neutral",
        UserStatus.Suspended => "pill pill-danger",
        _ => "pill pill-neutral"
    };

    public static string PillClass(ServiceUrgency urgency) => urgency switch
    {
        ServiceUrgency.CannotWork => "pill pill-danger",
        ServiceUrgency.AffectsMyWork => "pill pill-amber",
        _ => "pill pill-neutral"
    };

    public static string Label(AssetStatus status) => status switch
    {
        AssetStatus.InStock => "In stock",
        AssetStatus.InTransit => "In transit",
        AssetStatus.UnderService => "Under service",
        _ => status.ToString()
    };

    public static string Label(RequestStatus status) => status switch
    {
        RequestStatus.OnlineOrdered => "Ordered online",
        RequestStatus.Unclaimed => "Unclaimed",
        RequestStatus.PendingHrApproval => "Awaiting HR approval",
        _ => status.ToString()
    };

    public static string Label(ReturnStage stage) => stage switch
    {
        ReturnStage.AwaitingPickup => "Awaiting pickup",
        ReturnStage.InTransit => "In transit",
        ReturnStage.ReceivedForInspection => "Awaiting inspection",
        _ => stage.ToString()
    };

    public static string Label(ServiceUrgency urgency) => urgency switch
    {
        ServiceUrgency.CannotWork => "Can't work",
        ServiceUrgency.AffectsMyWork => "Affects my work",
        _ => "Minor"
    };

    public static string Label(AssetCategory category) => category switch
    {
        AssetCategory.DockingStation => "Docking Station",
        _ => category.ToString()
    };

    /// <summary>Emoji used for the asset card tiles, matching the prototype.</summary>
    public static string Emoji(AssetCategory category) => category switch
    {
        AssetCategory.Laptop => "💻",
        AssetCategory.Monitor => "🖥️",
        AssetCategory.Mouse => "🖱️",
        AssetCategory.Keyboard => "⌨️",
        AssetCategory.Headset => "🎧",
        AssetCategory.DockingStation => "🔌",
        AssetCategory.Storage => "💾",
        AssetCategory.Networking => "📡",
        AssetCategory.Peripheral => "🎨",
        AssetCategory.Furniture => "🪑",
        _ => "📦"
    };

    /// <summary>"Just now", "2 hours ago", "Jul 12" — the prototype's relative timestamps.</summary>
    public static string Relative(DateTime? utc)
    {
        if (utc is null) return "—";

        var delta = DateTime.UtcNow - utc.Value;
        if (delta.TotalMinutes < 2) return "Just now";
        if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes} mins ago";
        if (delta.TotalHours < 24) return $"{(int)delta.TotalHours} hour{((int)delta.TotalHours == 1 ? "" : "s")} ago";
        if (delta.TotalDays < 7) return $"{(int)delta.TotalDays} day{((int)delta.TotalDays == 1 ? "" : "s")} ago";
        return utc.Value.ToLocalTime().ToString("MMM d");
    }

    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][..1].ToUpperInvariant()
            : (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }

    public static string PermissionLabel(string permission) => permission switch
    {
        Permissions.ViewInventory => "View inventory",
        Permissions.ManageInventory => "Add / edit assets",
        Permissions.BulkImport => "Bulk import",
        Permissions.AssignAsset => "Assign assets",
        Permissions.RetireAsset => "Retire / report lost",
        Permissions.ApproveRequests => "Approve requests",
        Permissions.ApproveTeamRequests => "Approve team requests",
        Permissions.ClaimRequests => "Claim requests",
        Permissions.FulfilRequests => "Fulfil & dispatch",
        Permissions.RaiseRequests => "Raise requests",
        Permissions.TrackReturns => "Track returns",
        Permissions.InspectReturns => "Inspect returns",
        Permissions.RaiseReturns => "Raise returns",
        Permissions.ManageUsers => "Manage users",
        Permissions.ManageRoles => "Manage roles",
        Permissions.ViewAudit => "View audit trail",
        Permissions.ViewTeam => "View team",
        _ => permission
    };
}
