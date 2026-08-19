using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KIPL.AssetManagement.Web.Pages;

/// <summary>Sends each role to its natural landing page.</summary>
public class IndexModel : PageModel
{
    private readonly ICurrentUserService _currentUser;

    public IndexModel(ICurrentUserService currentUser) => _currentUser = currentUser;

    public IActionResult OnGet() => RedirectToPage(Navigation.LandingPage(_currentUser.RoleName));
}
