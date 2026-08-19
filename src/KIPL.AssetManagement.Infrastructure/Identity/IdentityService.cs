using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KIPL.AssetManagement.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<ApplicationRole> _roles;

    public IdentityService(UserManager<ApplicationUser> users, RoleManager<ApplicationRole> roles)
    {
        _users = users;
        _roles = roles;
    }

    public async Task<Result<string>> CreateUserAsync(string email, string fullName, string roleName, string password)
    {
        var existing = await _users.FindByEmailAsync(email);
        if (existing is not null) return Result<string>.Failure("A login already exists for that email address.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true
        };

        if (string.IsNullOrWhiteSpace(password)) password = "Kipl@12345";

        var created = await _users.CreateAsync(user, password);
        if (!created.Succeeded)
            return Result<string>.Failure(string.Join(" ", created.Errors.Select(e => e.Description)));

        if (!await _roles.RoleExistsAsync(roleName))
            await _roles.CreateAsync(new ApplicationRole(roleName));

        await _users.AddToRoleAsync(user, roleName);
        return Result<string>.Success(user.Id);
    }

    public async Task SyncUserAsync(string identityUserId, string email, string fullName, string roleName, bool enabled)
    {
        var user = await _users.FindByIdAsync(identityUserId);
        if (user is null) return;

        user.Email = email;
        user.UserName = email;
        user.FullName = fullName;
        await _users.UpdateAsync(user);

        var current = await _users.GetRolesAsync(user);
        if (!current.Contains(roleName))
        {
            if (current.Count > 0) await _users.RemoveFromRolesAsync(user, current);
            if (!await _roles.RoleExistsAsync(roleName)) await _roles.CreateAsync(new ApplicationRole(roleName));
            await _users.AddToRoleAsync(user, roleName);
        }

        await SetLockoutAsync(identityUserId, !enabled);
    }

    public async Task SetLockoutAsync(string identityUserId, bool locked)
    {
        var user = await _users.FindByIdAsync(identityUserId);
        if (user is null) return;

        await _users.SetLockoutEnabledAsync(user, true);
        await _users.SetLockoutEndDateAsync(user, locked ? DateTimeOffset.MaxValue : null);
    }

    public async Task SetRolePermissionAsync(string roleName, string permission, bool granted)
    {
        var role = await _roles.FindByNameAsync(roleName);
        if (role is null) return;

        var claims = await _roles.GetClaimsAsync(role);
        var existing = claims.FirstOrDefault(c => c.Type == Permissions.ClaimType && c.Value == permission);

        if (granted && existing is null)
            await _roles.AddClaimAsync(role, new System.Security.Claims.Claim(Permissions.ClaimType, permission));
        else if (!granted && existing is not null)
            await _roles.RemoveClaimAsync(role, existing);
    }

    public async Task<IReadOnlyList<string>> GetAllRoleNamesAsync()
        => await _roles.Roles
            .Select(r => r.Name!)
            .Where(n => n != null && n != "")
            .OrderBy(n => n)
            .ToListAsync();

    public async Task<Result> CreateRoleAsync(string roleName)
    {
        roleName = roleName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(roleName)) return Result.Failure("Role name is required.");
        if (roleName.Length > 80) return Result.Failure("Role name is too long.");

        if (await _roles.RoleExistsAsync(roleName))
            return Result.Failure("A role with that name already exists.");

        var created = await _roles.CreateAsync(new ApplicationRole(roleName));
        if (!created.Succeeded)
            return Result.Failure(string.Join(" ", created.Errors.Select(e => e.Description)));

        return Result.Success();
    }
}
