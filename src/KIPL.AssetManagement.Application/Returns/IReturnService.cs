using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Domain.Enums;

namespace KIPL.AssetManagement.Application.Returns;

public interface IReturnService
{
    Task<IReadOnlyList<ReturnListItemDto>> GetAllAsync(ReturnStage? stage, CancellationToken ct = default);
    Task<IReadOnlyList<ReturnListItemDto>> GetForEmployeeAsync(int employeeId, CancellationToken ct = default);
    Task<ReturnListItemDto?> GetAsync(int id, CancellationToken ct = default);
    Task<ReturnCountsDto> GetCountsAsync(CancellationToken ct = default);

    Task<Result<int>> RaiseAsync(RaiseReturnCommand command, CancellationToken ct = default);
    Task<Result> MarkHandedOverAsync(int returnId, string? tracking, CancellationToken ct = default);
    Task<Result> MarkReceivedAsync(int returnId, CancellationToken ct = default);
    Task<Result> InspectAsync(InspectReturnCommand command, CancellationToken ct = default);
}
