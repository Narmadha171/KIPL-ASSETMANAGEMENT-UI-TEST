using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Application.Requests;
using KIPL.AssetManagement.Domain.Enums;

namespace KIPL.AssetManagement.Application.Inventory;

public interface IInventoryService
{
    Task<PaginatedList<AssetListItemDto>> SearchAsync(AssetFilter filter, CancellationToken ct = default);
    Task<AssetDetailDto?> GetAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, AssetDetailDto>> GetDetailsAsync(IEnumerable<int> ids, CancellationToken ct = default);
    Task<InventoryCountsDto> GetCountsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AssetListItemDto>> GetAssignableAsync(AssetCategory? category, CancellationToken ct = default);
    Task<IReadOnlyList<AssetListItemDto>> GetForEmployeeAsync(int employeeId, CancellationToken ct = default);

    Task<Result<int>> CreateAsync(CreateAssetCommand command, CancellationToken ct = default);
    Task<Result> UpdateAsync(UpdateAssetCommand command, CancellationToken ct = default);
    Task<Result<ChallanDto>> AssignAsync(
        int assetId, int employeeId, string? notes,
        FulfilmentMode mode, string? courierService, string? trackingNumber, string? pickupPoint,
        CancellationToken ct = default);
    Task<Result> UnassignAsync(int assetId, CancellationToken ct = default);
    Task<Result> RetireAsync(int assetId, string? reason, CancellationToken ct = default);
    Task<Result> ReportLostAsync(int assetId, string? lastSeenWith, CancellationToken ct = default);
    Task<Result> SendToServiceAsync(int assetId, CancellationToken ct = default);
}
