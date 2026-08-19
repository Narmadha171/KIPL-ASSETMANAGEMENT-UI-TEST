using KIPL.AssetManagement.Application.Common.Models;

namespace KIPL.AssetManagement.Application.Audit;

public interface IAuditQueryService
{
    Task<PaginatedList<AuditEntryDto>> SearchAsync(AuditFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<AuditEntryDto>> GetRecentAsync(int take, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetDistinctActionsAsync(CancellationToken ct = default);
    Task<byte[]> ExportCsvAsync(AuditFilter filter, CancellationToken ct = default);
}
