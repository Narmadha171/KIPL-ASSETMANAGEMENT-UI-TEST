using System.Security.Claims;
using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Domain.Enums;

namespace KIPL.AssetManagement.Web.Infrastructure;

public class CurrentUserService : ICurrentUserService
{
    public const string EmployeeIdClaim = "employee_id";

    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;

    public string? IdentityUserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserName => User?.FindFirstValue("full_name") ?? User?.Identity?.Name;

    public string? Email => User?.FindFirstValue(ClaimTypes.Email) ?? User?.Identity?.Name;

    public string RoleName => User?.FindFirstValue(ClaimTypes.Role) ?? "System";

    public int? EmployeeId
        => int.TryParse(User?.FindFirstValue(EmployeeIdClaim), out var id) ? id : null;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public bool HasPermission(string permission)
    {
        if (User is null) return false;
        if (User.IsInRole(Roles.Admin)) return true;
        return User.HasClaim(Permissions.ClaimType, permission);
    }
}
