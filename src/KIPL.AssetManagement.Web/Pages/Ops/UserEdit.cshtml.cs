using KIPL.AssetManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KIPL.AssetManagement.Web.Pages.Ops;

/// <summary>Superseded by the "addUser"/"editUser" modals on the Users page — kept as a redirect for old links.</summary>
[Authorize(Policy = Permissions.ManageUsers)]
public class UserEditModel : PageModel
{
    public IActionResult OnGet(int? id) => RedirectToPage("/Ops/Users");
}
