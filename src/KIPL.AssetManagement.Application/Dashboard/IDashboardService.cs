namespace KIPL.AssetManagement.Application.Dashboard;

public interface IDashboardService
{
    Task<OperationsDashboardDto> GetOperationsAsync(CancellationToken ct = default);
    Task<EmployeeDashboardDto> GetForEmployeeAsync(int employeeId, bool includeTeam, CancellationToken ct = default);
}
