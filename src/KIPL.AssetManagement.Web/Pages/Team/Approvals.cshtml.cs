using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Application.Requests;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KIPL.AssetManagement.Web.Pages.Team;

[Authorize(Policy = Permissions.ApproveTeamRequests)]
public class ApprovalsModel : PageModel
{
    private readonly IAssetRequestService _requests;
    private readonly ICurrentUserService _currentUser;

    public ApprovalsModel(IAssetRequestService requests, ICurrentUserService currentUser)
    {
        _requests = requests;
        _currentUser = currentUser;
    }

    public IReadOnlyList<RequestListItemDto> Pending { get; private set; } = Array.Empty<RequestListItemDto>();
    public IReadOnlyList<RequestListItemDto> Decided { get; private set; } = Array.Empty<RequestListItemDto>();

    public bool CanApprove => true;

    /// <summary>A manager may not approve their own request — it escalates to an administrator.</summary>
    public bool NeedsAdmin(RequestListItemDto request) => request.RequesterName == _currentUser.UserName;

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (_currentUser.EmployeeId is not int me) return RedirectToPage("/Account/AccessDenied");

        await LoadAsync(me, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(int requestId, CancellationToken ct)
    {
        if (_currentUser.EmployeeId is not int me) return Forbid();
        if (!await BelongsToMyTeamAsync(requestId, me, ct)) return Forbid();

        Finish(await _requests.EscalateToHrAsync(requestId, me, ct), "Approved — sent to HR for final sign-off.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int requestId, string reason, CancellationToken ct)
    {
        if (_currentUser.EmployeeId is not int me) return Forbid();
        if (!await BelongsToMyTeamAsync(requestId, me, ct)) return Forbid();

        Finish(await _requests.RejectAsync(requestId, me, reason, ct), "Request rejected.");
        return RedirectToPage();
    }

    /// <summary>Guards against a manager acting on a request outside their own reporting line.</summary>
    private async Task<bool> BelongsToMyTeamAsync(int requestId, int managerId, CancellationToken ct)
    {
        var mine = await _requests.GetPendingForManagerAsync(managerId, ct);
        return mine.Any(r => r.Id == requestId);
    }

    private async Task LoadAsync(int managerId, CancellationToken ct)
    {
        Pending = await _requests.GetPendingForManagerAsync(managerId, ct);

        var all = await _requests.SearchAsync(new RequestFilter { ManagerId = managerId, PageSize = 50 }, ct);
        Decided = all.Items.Where(r => r.Status != RequestStatus.Pending).ToList();

        ViewData["NavBadges"] = new Dictionary<string, int> { ["team"] = Pending.Count };
    }

    private void Finish(Result result, string success)
    {
        if (result.Succeeded) TempData["Success"] = success;
        else TempData["Error"] = result.Error;
    }
}
