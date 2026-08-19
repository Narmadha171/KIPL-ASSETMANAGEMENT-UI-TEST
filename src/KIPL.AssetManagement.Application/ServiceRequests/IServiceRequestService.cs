using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Domain.Enums;

namespace KIPL.AssetManagement.Application.ServiceRequests;

public interface IServiceRequestService
{
    Task<IReadOnlyList<ServiceRequestDto>> GetAllAsync(ServiceRequestStatus? status, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceRequestDto>> GetForEmployeeAsync(int employeeId, CancellationToken ct = default);
    Task<int> GetOpenCountAsync(CancellationToken ct = default);

    Task<Result<int>> ReportAsync(ReportIssueCommand command, CancellationToken ct = default);

    /// <summary>IT takes the job on: the asset moves to Under service.</summary>
    Task<Result> AcceptAsync(int id, CancellationToken ct = default);

    /// <summary>IT rejects the report; the asset stays where it is.</summary>
    Task<Result> DismissAsync(int id, string reason, CancellationToken ct = default);

    Task<Result> ResolveAsync(int id, string resolution, CancellationToken ct = default);
}
