using KIPL.AssetManagement.Application.Audit;
using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Inventory;
using KIPL.AssetManagement.Application.Requests;
using KIPL.AssetManagement.Application.Returns;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KIPL.AssetManagement.Application.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _db;
    private readonly IInventoryService _inventory;
    private readonly IAssetRequestService _requests;
    private readonly IReturnService _returns;
    private readonly IAuditQueryService _audit;

    public DashboardService(
        IApplicationDbContext db,
        IInventoryService inventory,
        IAssetRequestService requests,
        IReturnService returns,
        IAuditQueryService audit)
    {
        _db = db;
        _inventory = inventory;
        _requests = requests;
        _returns = returns;
        _audit = audit;
    }

    public async Task<OperationsDashboardDto> GetOperationsAsync(CancellationToken ct = default)
    {
        var inventory = await _inventory.GetCountsAsync(ct);
        var requestCounts = await _requests.GetCountsAsync(ct);
        var returnCounts = await _returns.GetCountsAsync(ct);

        var openService = await _db.ServiceRequests
            .CountAsync(s => s.Status == ServiceRequestStatus.Open || s.Status == ServiceRequestStatus.InProgress, ct);

        var queue = await _requests.SearchAsync(new RequestFilter { OnlyOpen = true, PageSize = 8 }, ct);
        var recent = await _audit.GetRecentAsync(8, ct);

        var byCategoryRaw = await _db.Assets.AsNoTracking()
            .GroupBy(a => a.Category)
            .Select(g => new
            {
                Category = g.Key,
                Total = g.Count(),
                InStock = g.Count(x => x.Status == AssetStatus.InStock),
                Assigned = g.Count(x => x.Status == AssetStatus.Assigned)
            })
            .ToListAsync(ct);

        var byCategory = byCategoryRaw
            .OrderByDescending(x => x.Total)
            .Select(x => new CategoryBreakdownDto(x.Category.ToString(), x.Total, x.InStock, x.Assigned))
            .ToList();

        return new OperationsDashboardDto(
            inventory, requestCounts, returnCounts, openService, queue.Items, recent, byCategory);
    }

    public async Task<EmployeeDashboardDto> GetForEmployeeAsync(int employeeId, bool includeTeam, CancellationToken ct = default)
    {
        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        var displayName = employee?.FullName ?? "there";

        var assets = await _inventory.GetForEmployeeAsync(employeeId, ct);
        var requests = await _requests.GetForEmployeeAsync(employeeId, ct);
        var returns = await _returns.GetForEmployeeAsync(employeeId, ct);

        var teamApprovals = includeTeam
            ? await _requests.GetPendingForManagerAsync(employeeId, ct)
            : Array.Empty<Requests.RequestListItemDto>();

        var openRequests = requests.Count(r =>
            r.Status is not (RequestStatus.Completed or RequestStatus.Rejected or RequestStatus.Cancelled));

        // Things the employee personally has to act on right now.
        var pendingActions = requests.Count(r => r.Status == RequestStatus.Dispatched)
                           + returns.Count(r => r.Stage == ReturnStage.AwaitingPickup)
                           + teamApprovals.Count;

        return new EmployeeDashboardDto(
            displayName, assets.Count, openRequests, pendingActions,
            assets, requests, returns, teamApprovals);
    }
}
