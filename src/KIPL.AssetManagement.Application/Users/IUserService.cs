using KIPL.AssetManagement.Application.Common.Models;

namespace KIPL.AssetManagement.Application.Users;

public interface IUserService
{
    Task<PaginatedList<UserListItemDto>> SearchAsync(string? search, string? role, int page, int pageSize, CancellationToken ct = default);
    Task<UserCountsDto> GetCountsAsync(CancellationToken ct = default);
    Task<UpdateUserCommand?> GetForEditAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, UpdateUserCommand>> GetForEditManyAsync(IEnumerable<int> ids, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeOptionDto>> GetEmployeeOptionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeOptionDto>> GetDirectReportsAsync(int managerId, CancellationToken ct = default);
    Task<IReadOnlyList<(int Id, string Name)>> GetDepartmentsAsync(CancellationToken ct = default);

    Task<Result<int>> CreateAsync(CreateUserCommand command, CancellationToken ct = default);
    Task<Result> UpdateAsync(UpdateUserCommand command, CancellationToken ct = default);
    Task<Result> DeactivateAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<RolePermissionMatrixDto>> GetRoleMatrixAsync(CancellationToken ct = default);
    Task<Result> SetPermissionAsync(string roleName, string permission, bool granted, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetAllRoleNamesAsync(CancellationToken ct = default);
    Task<Result> CreateRoleAsync(string roleName, CancellationToken ct = default);
}
