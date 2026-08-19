namespace KIPL.AssetManagement.Domain.Enums;

/// <summary>
/// Fine-grained permission claims. Roles are mapped to these in the seeder, and
/// authorization policies in the Web layer are built one-per-permission.
/// </summary>
public static class Permissions
{
    public const string ClaimType = "permission";

    public const string ViewInventory       = "inventory.view";
    public const string ManageInventory     = "inventory.manage";
    public const string BulkImport          = "inventory.bulkimport";
    public const string AssignAsset         = "inventory.assign";
    public const string RetireAsset         = "inventory.retire";

    public const string ApproveRequests     = "requests.approve";
    public const string ApproveTeamRequests = "requests.approve.team";
    public const string ClaimRequests       = "requests.claim";
    public const string FulfilRequests      = "requests.fulfil";
    public const string RaiseRequests       = "requests.raise";

    public const string TrackReturns        = "returns.track";
    public const string InspectReturns      = "returns.inspect";
    public const string RaiseReturns        = "returns.raise";

    public const string ManageUsers         = "users.manage";
    public const string ManageRoles         = "roles.manage";
    public const string ViewAudit           = "audit.view";
    public const string ViewTeam            = "team.view";

    public static readonly string[] All =
    {
        ViewInventory, ManageInventory, BulkImport, AssignAsset, RetireAsset,
        ApproveRequests, ApproveTeamRequests, ClaimRequests, FulfilRequests, RaiseRequests,
        TrackReturns, InspectReturns, RaiseReturns,
        ManageUsers, ManageRoles, ViewAudit, ViewTeam
    };

    /// <summary>Default permission grants per role, applied by the data seeder.</summary>
    public static IReadOnlyDictionary<string, string[]> ForRole { get; } =
        new Dictionary<string, string[]>
        {
            [Roles.Admin] = All,
            [Roles.ITAssetManager] = new[]
            {
                ViewInventory, ManageInventory, BulkImport, AssignAsset, RetireAsset,
                ApproveRequests, ClaimRequests, FulfilRequests, RaiseRequests,
                TrackReturns, InspectReturns, RaiseReturns, ViewAudit
            },
            [Roles.ITAssetExecutive] = new[]
            {
                // Also holds ApproveRequests: a Reporting Manager or HR raising a request for
                // themselves has nobody else to approve it, so it falls to whoever else is on
                // the shared approvals queue — HR, IT Asset Manager, and IT Asset Executive.
                ViewInventory, ManageInventory, AssignAsset,
                ApproveRequests, ClaimRequests, FulfilRequests, RaiseRequests,
                TrackReturns, InspectReturns, RaiseReturns
            },
            [Roles.HR] = new[]
            {
                // HR approves and rejects only — assigning the physical asset is IT's job,
                // done from the ExecApprovals queue once HR's approval clears the request.
                ViewInventory, ApproveRequests, RaiseRequests, RaiseReturns
            },
            [Roles.ReportingManager] = new[]
            {
                ApproveTeamRequests, ViewTeam, RaiseRequests, RaiseReturns
            },
            [Roles.Employee] = new[]
            {
                RaiseRequests, RaiseReturns
            }
        };
}
