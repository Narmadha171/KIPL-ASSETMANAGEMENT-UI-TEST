using KIPL.AssetManagement.Application.Users;
using KIPL.AssetManagement.Domain.Enums;
using KIPL.AssetManagement.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KIPL.AssetManagement.Web.Pages.Ops;

[Authorize(Policy = Permissions.ManageRoles)]
public class RolesModel : PageModel
{
    private readonly IUserService _users;

    public RolesModel(IUserService users) => _users = users;

    [BindProperty(SupportsGet = true)] public string? SelectedRole { get; set; }

    [BindProperty] public string? NewRoleName { get; set; }

    /// <summary>Set when the create-role post fails validation, so the view reopens the dialog.</summary>
    public bool ReopenCreateRole { get; private set; }

    public IReadOnlyList<RolePermissionMatrixDto> Matrix { get; private set; } = Array.Empty<RolePermissionMatrixDto>();

    public async Task OnGetAsync(CancellationToken ct)
        => Matrix = await _users.GetRoleMatrixAsync(ct);

    public async Task<IActionResult> OnPostCreateRoleAsync(CancellationToken ct)
    {
        var result = await _users.CreateRoleAsync(NewRoleName ?? string.Empty, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            ReopenCreateRole = true;
            Matrix = await _users.GetRoleMatrixAsync(ct);
            return Page();
        }

        TempData["Success"] = $"\"{NewRoleName!.Trim()}\" role created.";
        return RedirectToPage(new { SelectedRole = NewRoleName!.Trim() });
    }

    public async Task<IActionResult> OnPostToggleAsync(
        string roleName, string permission, bool granted, string? selectedRole, CancellationToken ct)
    {
        var result = await _users.SetPermissionAsync(roleName, permission, granted, ct);

        if (result.Succeeded)
        {
            var label = DisplayHelpers.PermissionLabel(permission);
            TempData["Success"] = $"\"{label}\" {(granted ? "granted to" : "revoked from")} {roleName}.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToPage(new { SelectedRole = selectedRole ?? roleName });
    }
}
