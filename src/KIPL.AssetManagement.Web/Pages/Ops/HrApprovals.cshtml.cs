using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Inventory;
using KIPL.AssetManagement.Application.Requests;
using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KIPL.AssetManagement.Web.Pages.Ops;

[Authorize(Policy = Permissions.ApproveRequests)]
public class HrApprovalsModel : ApprovalsPageModel
{
    public HrApprovalsModel(
        IAssetRequestService requests, IInventoryService inventory, ICurrentUserService currentUser)
        : base(requests, inventory, currentUser) { }

    [BindProperty(SupportsGet = true)] public string? Department { get; set; }

    public IReadOnlyList<string> Departments { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<RequestListItemDto> AwaitingVerification { get; private set; } = Array.Empty<RequestListItemDto>();
    public ChallanDto? Challan { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Pending = await Requests.GetPendingAsync(Department, ct);
        Departments = await Requests.GetDepartmentsAsync(ct);
        AwaitingVerification = await Requests.GetAwaitingVerificationAsync(ct);
        await LoadAssignableAsync(ct);

        if (TempData["ChallanRequestId"] is int challanRequestId)
        {
            var result = await Requests.GetChallanAsync(challanRequestId, ct);
            if (result.Succeeded) Challan = result.Value;
        }

        ViewData["NavBadges"] = new Dictionary<string, int>
        {
            ["approvals"] = Pending.Count + AwaitingVerification.Count
        };
    }

    /// <summary>
    /// HR checks in a delivered online order. A serial number is mandatory; a
    /// declared mismatch must carry an explanation, and both land in the audit trail.
    /// </summary>
    public async Task<IActionResult> OnPostVerifyAsync(
        int requestId,
        string serialNumber,
        string? assetTag,
        bool tagMismatch,
        string? mismatchNote,
        IFormFile? photo,
        CancellationToken ct)
    {
        if (!CanApprove) return Forbid();

        await using var stream = photo?.OpenReadStream();
        var command = new VerifyOnlineOrderCommand
        {
            RequestId = requestId,
            SerialNumber = serialNumber,
            AssetTag = assetTag,
            TagMismatch = tagMismatch,
            MismatchNote = mismatchNote,
            Photo = stream is null || photo is null
                ? null
                : new PhotoUpload(stream, photo.FileName, photo.ContentType, photo.Length)
        };

        Finish(await Requests.VerifyOnlineDeliveryAsync(command, ct),
            "Delivery verified — the item is now in inventory.");

        return RedirectToPage();
    }
}
