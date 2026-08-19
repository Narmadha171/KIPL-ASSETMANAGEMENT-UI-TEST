using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using KIPL.AssetManagement.Domain.Enums;

namespace KIPL.AssetManagement.Web.Pages.Ops;

/// <summary>Superseded by the "addAsset"/"editAsset" modals on the Inventory page — kept as a redirect for old links.</summary>
[Authorize(Policy = Permissions.ManageInventory)]
public class AssetEditModel : PageModel
{
    public IActionResult OnGet(int? id) => RedirectToPage("/Ops/Inventory");
}
