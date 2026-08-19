using KIPL.AssetManagement.Application.Requests;

namespace KIPL.AssetManagement.Web.Pages.Shared;

/// <summary>View model for the approval card partial.</summary>
public class ApprovalCardModel
{
    public ApprovalCardModel(RequestListItemDto request, bool canApprove, bool canFulfil, bool needsAdmin)
    {
        Request = request;
        CanApprove = canApprove;
        CanFulfil = canFulfil;
        NeedsAdmin = needsAdmin;
    }

    public RequestListItemDto Request { get; }
    public bool CanApprove { get; }
    public bool CanFulfil { get; }

    /// <summary>
    /// True when the requester is themselves an approver, so nobody at this level
    /// may action it — an administrator has to. Mirrors the prototype's
    /// "NEEDS ADMIN" badge.
    /// </summary>
    public bool NeedsAdmin { get; }
}
