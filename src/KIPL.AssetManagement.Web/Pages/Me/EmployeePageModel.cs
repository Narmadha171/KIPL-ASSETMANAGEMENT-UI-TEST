using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Common.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KIPL.AssetManagement.Web.Pages.Me;

/// <summary>
/// Shared plumbing for the self-service screens: resolves the signed-in user's
/// Employee id and standardises the success/error banner handling.
/// </summary>
public abstract class EmployeePageModel : PageModel
{
    protected EmployeePageModel(ICurrentUserService currentUser) => CurrentUser = currentUser;

    protected ICurrentUserService CurrentUser { get; }

    /// <summary>The Employee row backing the signed-in login, or null if unlinked.</summary>
    protected int? EmployeeId => CurrentUser.EmployeeId;

    protected void Finish(Result result, string success)
    {
        if (result.Succeeded) TempData["Success"] = success;
        else TempData["Error"] = result.Error;
    }
}
