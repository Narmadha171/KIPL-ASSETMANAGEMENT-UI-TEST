using KIPL.AssetManagement.Application.Audit;
using KIPL.AssetManagement.Application.Inventory;
using KIPL.AssetManagement.Application.Requests;
using KIPL.AssetManagement.Application.Returns;

namespace KIPL.AssetManagement.Application.Dashboard;

public record OperationsDashboardDto(
    InventoryCountsDto Inventory,
    RequestCountsDto Requests,
    ReturnCountsDto Returns,
    int OpenServiceRequests,
    IReadOnlyList<RequestListItemDto> ActionQueue,
    IReadOnlyList<AuditEntryDto> RecentActivity,
    IReadOnlyList<CategoryBreakdownDto> ByCategory);

public record CategoryBreakdownDto(string Category, int Total, int InStock, int Assigned);

public record EmployeeDashboardDto(
    string DisplayName,
    int AssetsAssigned,
    int OpenRequests,
    int PendingActions,
    IReadOnlyList<AssetListItemDto> Assets,
    IReadOnlyList<RequestListItemDto> Requests,
    IReadOnlyList<ReturnListItemDto> Returns,
    IReadOnlyList<RequestListItemDto> TeamApprovals);
