using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Application.Inventory;
using KIPL.AssetManagement.Application.Requests;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KIPL.AssetManagement.Web.Pages.Ops;

/// <summary>
/// Shared behaviour for the HR, Executive and Team approval screens. They differ
/// in which requests they show and which buttons appear, but the actions they
/// post are identical, so the handlers live here once.
/// </summary>
public abstract class ApprovalsPageModel : PageModel
{
    protected ApprovalsPageModel(
        IAssetRequestService requests,
        IInventoryService inventory,
        ICurrentUserService currentUser)
    {
        Requests = requests;
        Inventory = inventory;
        CurrentUser = currentUser;
    }

    protected IAssetRequestService Requests { get; }
    protected IInventoryService Inventory { get; }
    protected ICurrentUserService CurrentUser { get; }

    public IReadOnlyList<RequestListItemDto> Pending { get; protected set; } = Array.Empty<RequestListItemDto>();
    public IReadOnlyList<AssetListItemDto> AssignableAssets { get; protected set; } = Array.Empty<AssetListItemDto>();

    public bool CanApprove => CurrentUser.HasPermission(Permissions.ApproveRequests)
                           || CurrentUser.HasPermission(Permissions.ApproveTeamRequests);
    public bool CanFulfil => CurrentUser.HasPermission(Permissions.FulfilRequests);

    /// <summary>
    /// A request cannot be actioned at this level when the requester is themselves
    /// an approver — the prototype's "NEEDS ADMIN" case.
    /// </summary>
    public bool NeedsAdmin(RequestListItemDto request)
    {
        if (User.IsInRole(Roles.Admin)) return false;
        return request.RequesterName == CurrentUser.UserName;
    }

    protected async Task LoadAssignableAsync(CancellationToken ct)
        => AssignableAssets = await Inventory.GetAssignableAsync(null, ct);

    // ---------------------------------------------------------- handlers

    public async Task<IActionResult> OnPostApproveAsync(int requestId, CancellationToken ct)
    {
        if (!CanApprove) return Forbid();
        if (CurrentUser.EmployeeId is not int me) return Forbid();

        Finish(await Requests.ApproveAsync(requestId, me, ct), "Request approved.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int requestId, string reason, CancellationToken ct)
    {
        if (!CanApprove) return Forbid();
        if (CurrentUser.EmployeeId is not int me) return Forbid();

        Finish(await Requests.RejectAsync(requestId, me, reason, ct), "Request rejected.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClaimAsync(int requestId, CancellationToken ct)
    {
        if (!CurrentUser.HasPermission(Permissions.ClaimRequests)) return Forbid();
        if (CurrentUser.EmployeeId is not int me) return Forbid();

        Finish(await Requests.ClaimAsync(requestId, me, ct), "Request claimed — it is yours to fulfil.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostFulfilAsync(
        int requestId,
        List<int> assetIds,
        FulfilmentMode mode,
        string? courierService,
        string? trackingNumber,
        string? pickupPoint,
        IFormFile? photo,
        CancellationToken ct)
    {
        if (!CanFulfil) return Forbid();

        await using var stream = photo?.OpenReadStream();
        var command = new FulfilRequestCommand
        {
            RequestId = requestId,
            AssetIds = assetIds ?? new List<int>(),
            Mode = mode,
            CourierService = courierService,
            TrackingNumber = trackingNumber,
            PickupPoint = pickupPoint,
            Photo = stream is null || photo is null
                ? null
                : new PhotoUpload(stream, photo.FileName, photo.ContentType, photo.Length)
        };

        var result = await Requests.FulfilAsync(command, ct);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return RedirectToPage();
        }

        TempData["Success"] = "Asset dispatched — delivery challan ready.";
        // Hand off enough state for the current page to fetch and auto-open the challan modal.
        TempData["ChallanRequestId"] = requestId;
        TempData["OpenModal"] = "deliveryChallan";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostOrderAsync(
        int requestId, string orderId, string vendor, string? brand, string? model, CancellationToken ct)
    {
        if (!CanFulfil) return Forbid();

        var command = new OnlineOrderCommand
        {
            RequestId = requestId,
            OrderId = orderId,
            Vendor = vendor,
            Brand = brand ?? string.Empty,
            Model = model
        };

        Finish(await Requests.PlaceOnlineOrderAsync(command, ct), "Online order logged.");
        return RedirectToPage();
    }

    protected void Finish(Result result, string success)
    {
        if (result.Succeeded) TempData["Success"] = success;
        else TempData["Error"] = result.Error;
    }
}
