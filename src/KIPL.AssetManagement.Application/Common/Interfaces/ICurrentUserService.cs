namespace KIPL.AssetManagement.Application.Common.Interfaces;

/// <summary>Ambient information about the signed-in user, supplied by the Web layer.</summary>
public interface ICurrentUserService
{
    string? IdentityUserId { get; }
    string? UserName { get; }
    string? Email { get; }
    string RoleName { get; }
    int? EmployeeId { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permission);
}
