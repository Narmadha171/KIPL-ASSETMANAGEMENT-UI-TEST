using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using KIPL.AssetManagement.Domain.Enums;

namespace KIPL.AssetManagement.Web.Pages.Ops;

/// <summary>Superseded by the "deliveryChallan" modal, auto-opened on the approvals pages — kept as a redirect for old links.</summary>
[Authorize(Policy = Permissions.FulfilRequests)]
public class ChallanModel : PageModel
{
    public IActionResult OnGet(int requestId) => RedirectToPage("/Ops/ExecApprovals");
}
