using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KIPL.AssetManagement.Web.Pages.Ops;

/// <summary>Superseded by the "assetDetail" modal on the Inventory page — kept as a redirect for old links.</summary>
public class AssetDetailsModel : PageModel
{
    public IActionResult OnGet(int id) => RedirectToPage("/Ops/Inventory");
}
