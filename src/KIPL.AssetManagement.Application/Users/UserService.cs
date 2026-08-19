using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KIPL.AssetManagement.Application.Users;

public class UserService : IUserService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly IIdentityService _identity;

    public UserService(IApplicationDbContext db, IAuditService audit, IIdentityService identity)
    {
        _db = db;
        _audit = audit;
        _identity = identity;
    }

    public async Task<PaginatedList<UserListItemDto>> SearchAsync(string? search, string? role, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Employees.AsNoTracking().Include(e => e.Department).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => e.FullName.Contains(term) || e.Email.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(role)) query = query.Where(e => e.RoleName == role);

        var projected = query
            .OrderBy(e => e.FullName)
            .Select(e => new UserListItemDto(
                e.Id,
                e.FullName,
                e.FullName,
                e.Email,
                e.RoleName,
                e.Department != null ? e.Department.Name : "—",
                e.LastLoginUtc,
                e.Status,
                e.AssignedAssets.Count));

        var result = await PaginatedList<UserListItemDto>.CreateAsync(projected, page, pageSize, ct);

        // Initials are computed client-side to keep the projection translatable to SQL.
        var items = result.Items.Select(x => x with { Initials = ToInitials(x.FullName) }).ToList();
        return new PaginatedList<UserListItemDto>(items, result.TotalCount, result.PageNumber, result.PageSize);
    }

    private static string ToInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][..1].ToUpperInvariant()
            : (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }

    public async Task<UserCountsDto> GetCountsAsync(CancellationToken ct = default)
    {
        var total = await _db.Employees.CountAsync(ct);
        var active = await _db.Employees.CountAsync(e => e.Status == UserStatus.Active, ct);
        var admins = await _db.Employees.CountAsync(e => e.RoleName == Roles.Admin || e.RoleName == Roles.ITAssetManager, ct);
        var inactive = await _db.Employees.CountAsync(e => e.Status != UserStatus.Active, ct);
        return new UserCountsDto(total, active, admins, inactive);
    }

    public async Task<UpdateUserCommand?> GetForEditAsync(int id, CancellationToken ct = default)
        => await _db.Employees.AsNoTracking().Where(e => e.Id == id)
            .Select(e => new UpdateUserCommand
            {
                Id = e.Id,
                FullName = e.FullName,
                Email = e.Email,
                RoleName = e.RoleName,
                DepartmentId = e.DepartmentId,
                ManagerId = e.ManagerId,
                Status = e.Status
            })
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyDictionary<int, UpdateUserCommand>> GetForEditManyAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        var commands = await _db.Employees.AsNoTracking().Where(e => idList.Contains(e.Id))
            .Select(e => new UpdateUserCommand
            {
                Id = e.Id,
                FullName = e.FullName,
                Email = e.Email,
                RoleName = e.RoleName,
                DepartmentId = e.DepartmentId,
                ManagerId = e.ManagerId,
                Status = e.Status
            })
            .ToListAsync(ct);

        return commands.ToDictionary(c => c.Id);
    }

    public async Task<IReadOnlyList<EmployeeOptionDto>> GetEmployeeOptionsAsync(CancellationToken ct = default)
        => await _db.Employees.AsNoTracking().Include(e => e.Department)
            .Where(e => e.Status == UserStatus.Active)
            .OrderBy(e => e.FullName)
            .Select(e => new EmployeeOptionDto(e.Id, e.FullName, e.Department != null ? e.Department.Name : "—"))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EmployeeOptionDto>> GetDirectReportsAsync(int managerId, CancellationToken ct = default)
        => await _db.Employees.AsNoTracking().Include(e => e.Department)
            .Where(e => e.ManagerId == managerId)
            .OrderBy(e => e.FullName)
            .Select(e => new EmployeeOptionDto(e.Id, e.FullName, e.Department != null ? e.Department.Name : "—"))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<(int Id, string Name)>> GetDepartmentsAsync(CancellationToken ct = default)
    {
        var list = await _db.Departments.AsNoTracking().OrderBy(d => d.Name)
            .Select(d => new { d.Id, d.Name }).ToListAsync(ct);
        return list.Select(x => (x.Id, x.Name)).ToList();
    }

    public async Task<Result<int>> CreateAsync(CreateUserCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.FullName)) return Result<int>.Failure("Full name is required.");
        if (string.IsNullOrWhiteSpace(command.Email)) return Result<int>.Failure("Email is required.");

        var email = command.Email.Trim().ToLowerInvariant();
        if (await _db.Employees.AnyAsync(e => e.Email == email, ct))
            return Result<int>.Failure("A user with that email already exists.");

        var identityResult = await _identity.CreateUserAsync(email, command.FullName.Trim(), command.RoleName, command.Password);
        if (!identityResult.Succeeded) return Result<int>.Failure(identityResult.Error!);

        var employee = new Employee
        {
            FullName = command.FullName.Trim(),
            Email = email,
            RoleName = command.RoleName,
            DepartmentId = command.DepartmentId,
            ManagerId = command.ManagerId,
            Status = UserStatus.Active,
            IdentityUserId = identityResult.Value
        };

        _db.Employees.Add(employee);
        await _audit.LogAsync("User created", employee.FullName, command.RoleName, ct);
        await _db.SaveChangesAsync(ct);
        return Result<int>.Success(employee.Id);
    }

    public async Task<Result> UpdateAsync(UpdateUserCommand command, CancellationToken ct = default)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == command.Id, ct);
        if (employee is null) return Result.Failure("User not found.");

        if (command.ManagerId == employee.Id) return Result.Failure("A user cannot report to themselves.");

        var roleChanged = employee.RoleName != command.RoleName;

        employee.FullName = command.FullName.Trim();
        employee.Email = command.Email.Trim().ToLowerInvariant();
        employee.RoleName = command.RoleName;
        employee.DepartmentId = command.DepartmentId;
        employee.ManagerId = command.ManagerId;
        employee.Status = command.Status;
        employee.ModifiedUtc = DateTime.UtcNow;

        if (employee.IdentityUserId is not null)
            await _identity.SyncUserAsync(employee.IdentityUserId, employee.Email, employee.FullName, employee.RoleName, employee.Status == UserStatus.Active);

        await _audit.LogAsync(roleChanged ? "Role changed" : "User edited", employee.FullName,
            roleChanged ? command.RoleName : null, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct = default)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (employee is null) return Result.Failure("User not found.");

        var holding = await _db.Assets.CountAsync(a => a.AssignedToEmployeeId == id, ct);
        if (holding > 0)
            return Result.Failure($"{employee.FullName} still holds {holding} asset(s). Process the returns first.");

        employee.Status = UserStatus.Inactive;
        if (employee.IdentityUserId is not null)
            await _identity.SetLockoutAsync(employee.IdentityUserId, true);

        await _audit.LogAsync("User deactivated", employee.FullName, ct: ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<IReadOnlyList<string>> GetAllRoleNamesAsync(CancellationToken ct = default)
    {
        var dynamic = await _identity.GetAllRoleNamesAsync();
        // Built-ins first in their canonical order, then any custom roles alphabetically.
        var custom = dynamic.Where(r => !Roles.All.Contains(r)).OrderBy(r => r);
        return Roles.All.Concat(custom).ToList();
    }

    public async Task<Result> CreateRoleAsync(string roleName, CancellationToken ct = default)
    {
        var result = await _identity.CreateRoleAsync(roleName);
        if (result.Succeeded)
            await _audit.LogAsync("Role created", roleName.Trim(), ct: ct);
        return result;
    }

    public async Task<IReadOnlyList<RolePermissionMatrixDto>> GetRoleMatrixAsync(CancellationToken ct = default)
    {
        var grants = await _db.RolePermissions.AsNoTracking().ToListAsync(ct);
        var counts = await _db.Employees.AsNoTracking()
            .GroupBy(e => e.RoleName).Select(g => new { Role = g.Key, Count = g.Count() }).ToListAsync(ct);
        var roleNames = await GetAllRoleNamesAsync(ct);

        return roleNames.Select(role =>
        {
            var map = Permissions.All.ToDictionary(
                p => p,
                p => grants.FirstOrDefault(g => g.RoleName == role && g.Permission == p)?.IsGranted ?? false);

            return new RolePermissionMatrixDto(role, map, counts.FirstOrDefault(c => c.Role == role)?.Count ?? 0);
        }).ToList();
    }

    public async Task<Result> SetPermissionAsync(string roleName, string permission, bool granted, CancellationToken ct = default)
    {
        var knownRoles = await GetAllRoleNamesAsync(ct);
        if (!knownRoles.Contains(roleName)) return Result.Failure("Unknown role.");
        if (!Permissions.All.Contains(permission)) return Result.Failure("Unknown permission.");
        if (roleName == Roles.Admin && !granted)
            return Result.Failure("Administrator permissions cannot be revoked.");

        var row = await _db.RolePermissions.FirstOrDefaultAsync(r => r.RoleName == roleName && r.Permission == permission, ct);
        if (row is null)
        {
            row = new RolePermission { RoleName = roleName, Permission = permission, IsGranted = granted };
            _db.RolePermissions.Add(row);
        }
        else
        {
            row.IsGranted = granted;
        }

        await _identity.SetRolePermissionAsync(roleName, permission, granted);
        await _audit.LogAsync("Permission changed", $"{roleName} / {permission}", granted ? "granted" : "revoked", ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
