using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Dashboard;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace KIPL.AssetManagement.Web.Pages.Me;

public class DashboardModel : EmployeePageModel
{
    private readonly IDashboardService _dashboard;

    public DashboardModel(IDashboardService dashboard, ICurrentUserService currentUser)
        : base(currentUser) => _dashboard = dashboard;

    public EmployeeDashboardDto Data { get; private set; } = null!;
    public bool IsManager => CurrentUser.HasPermission(Permissions.ApproveTeamRequests);

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (EmployeeId is not int me)
            return RedirectToPage("/Account/AccessDenied");

        Data = await _dashboard.GetForEmployeeAsync(me, IsManager, ct);

        ViewData["NavBadges"] = new Dictionary<string, int>
        {
            ["mine"] = Data.Requests.Count(r => r.Status == RequestStatus.Dispatched),
            ["team"] = Data.TeamApprovals.Count
        };

        return Page();
    }
}
