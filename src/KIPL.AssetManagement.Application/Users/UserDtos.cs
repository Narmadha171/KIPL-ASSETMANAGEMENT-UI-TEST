using KIPL.AssetManagement.Domain.Enums;

namespace KIPL.AssetManagement.Application.Users;

public record UserListItemDto(
    int Id, string Initials, string FullName, string Email, string RoleName,
    string DepartmentName, DateTime? LastLoginUtc, UserStatus Status, int AssetCount);

public record EmployeeOptionDto(int Id, string FullName, string DepartmentName);

public record UserCountsDto(int Total, int Active, int Administrators, int Inactive);

public class CreateUserCommand
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = Roles.Employee;
    public int? DepartmentId { get; set; }
    public int? ManagerId { get; set; }
    public string Password { get; set; } = string.Empty;
}

public class UpdateUserCommand
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = Roles.Employee;
    public int? DepartmentId { get; set; }
    public int? ManagerId { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
}

public record RolePermissionMatrixDto(string RoleName, IReadOnlyDictionary<string, bool> Permissions, int UserCount);
