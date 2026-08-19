using KIPL.AssetManagement.Application.Common.Models;

namespace KIPL.AssetManagement.Application.Common.Interfaces;

/// <summary>
/// Wraps ASP.NET Core Identity so the Application layer can create and adjust
/// login accounts without referencing Identity types directly.
/// </summary>
public interface IIdentityService
{
    Task<Result<string>> CreateUserAsync(string email, string fullName, string roleName, string password);
    Task SyncUserAsync(string identityUserId, string email, string fullName, string roleName, bool enabled);
    Task SetLockoutAsync(string identityUserId, bool locked);
    Task SetRolePermissionAsync(string roleName, string permission, bool granted);

    /// <summary>All roles known to Identity, built-in and custom, alphabetised.</summary>
    Task<IReadOnlyList<string>> GetAllRoleNamesAsync();

    /// <summary>Creates a new, empty (no permissions granted) custom role.</summary>
    Task<Result> CreateRoleAsync(string roleName);
}
