using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Domain.Enums;

namespace KIPL.AssetManagement.Application.Requests;

public interface IAssetRequestService
{
    Task<PaginatedList<RequestListItemDto>> SearchAsync(RequestFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<RequestListItemDto>> GetForEmployeeAsync(int employeeId, CancellationToken ct = default);
    Task<IReadOnlyList<RequestListItemDto>> GetPendingForManagerAsync(int managerEmployeeId, CancellationToken ct = default);
    Task<IReadOnlyList<RequestListItemDto>> GetPendingAsync(string? department, CancellationToken ct = default);
    Task<IReadOnlyList<RequestListItemDto>> GetAwaitingVerificationAsync(CancellationToken ct = default);
    Task<RequestListItemDto?> GetAsync(int id, CancellationToken ct = default);
    Task<RequestCountsDto> GetCountsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetDepartmentsAsync(CancellationToken ct = default);

    Task<Result<int>> RaiseAsync(RaiseRequestCommand command, CancellationToken ct = default);
    Task<Result> EscalateToHrAsync(int requestId, int managerEmployeeId, CancellationToken ct = default);
    Task<Result> ApproveAsync(int requestId, int approverEmployeeId, CancellationToken ct = default);
    Task<Result> RejectAsync(int requestId, int approverEmployeeId, string reason, CancellationToken ct = default);
    Task<Result> ClaimAsync(int requestId, int operationsEmployeeId, CancellationToken ct = default);

    /// <summary>Dispatches one or more assets and returns the challan for printing.</summary>
    Task<Result<ChallanDto>> FulfilAsync(FulfilRequestCommand command, CancellationToken ct = default);

    Task<Result> PlaceOnlineOrderAsync(OnlineOrderCommand command, CancellationToken ct = default);
    Task<Result> VerifyOnlineDeliveryAsync(VerifyOnlineOrderCommand command, CancellationToken ct = default);
    Task<Result> ConfirmReceiptAsync(ConfirmReceiptCommand command, CancellationToken ct = default);
    Task<Result> CancelAsync(int requestId, int employeeId, CancellationToken ct = default);

    /// <summary>Rebuilds the challan for a request that has already been dispatched.</summary>
    Task<Result<ChallanDto>> GetChallanAsync(int requestId, CancellationToken ct = default);
}
