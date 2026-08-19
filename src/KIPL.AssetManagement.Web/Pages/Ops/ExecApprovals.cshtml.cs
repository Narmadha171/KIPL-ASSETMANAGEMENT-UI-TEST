using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Inventory;
using KIPL.AssetManagement.Application.Requests;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace KIPL.AssetManagement.Web.Pages.Ops;

/// <summary>
/// The IT operations queue. Executives cannot approve, so this screen leads with
/// the approved-but-unfulfilled work and shows pending items read-only.
/// </summary>
public class ExecApprovalsModel : ApprovalsPageModel
{
    public ExecApprovalsModel(
        IAssetRequestService requests, IInventoryService inventory, ICurrentUserService currentUser)
        : base(requests, inventory, currentUser) { }

    [BindProperty(SupportsGet = true)] public string? Department { get; set; }

    public IReadOnlyList<string> Departments { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<RequestListItemDto> ToFulfil { get; private set; } = Array.Empty<RequestListItemDto>();
    public RequestCountsDto Counts { get; private set; } = null!;
    public ChallanDto? Challan { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var open = await Requests.SearchAsync(new RequestFilter { OnlyOpen = true, PageSize = 200 }, ct);

        ToFulfil = open.Items
            .Where(r => r.Status is RequestStatus.Unclaimed or RequestStatus.Claimed or RequestStatus.OnlineOrdered)
            .ToList();

        Pending = await Requests.GetPendingAsync(Department, ct);
        Departments = await Requests.GetDepartmentsAsync(ct);
        Counts = await Requests.GetCountsAsync(ct);
        await LoadAssignableAsync(ct);

        if (TempData["ChallanRequestId"] is int challanRequestId)
        {
            var result = await Requests.GetChallanAsync(challanRequestId, ct);
            if (result.Succeeded) Challan = result.Value;
        }

        ViewData["NavBadges"] = new Dictionary<string, int>
        {
            ["approvals"] = ToFulfil.Count + Pending.Count
        };
    }
}
