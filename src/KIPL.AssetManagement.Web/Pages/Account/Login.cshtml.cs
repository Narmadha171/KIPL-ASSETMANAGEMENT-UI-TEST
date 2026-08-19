using System.ComponentModel.DataAnnotations;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using KIPL.AssetManagement.Infrastructure.Identity;
using KIPL.AssetManagement.Infrastructure.Persistence;
using KIPL.AssetManagement.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KIPL.AssetManagement.Web.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    public const string DemoPassword = "Kipl@12345";

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public IReadOnlyList<(string Role, string Email)> DemoAccounts { get; } = new[]
    {
        (Roles.Admin, "admin@kipl.com"),
        (Roles.ITAssetManager, "vikram.singh@kipl.com"),
        (Roles.ITAssetExecutive, "revanth.k@kipl.com"),
        (Roles.HR, "amanda.lee@kipl.com"),
        (Roles.ReportingManager, "michael.chen@kipl.com"),
        (Roles.Employee, "priya.shankar@kipl.com"),
    };

    public class InputModel
    {
        [Required, EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Keep me signed in")]
        public bool RememberMe { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        // Clear any partial cookie left over from a previous session.
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid) return Page();

        var result = await _signInManager.PasswordSignInAsync(
            Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "This account is locked. Contact your administrator.");
            return Page();
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "That email and password combination was not recognised.");
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is not null)
        {
            user.LastLoginUtc = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var employee = await _db.Employees.FirstOrDefaultAsync(e => e.IdentityUserId == user.Id);
            if (employee is not null)
            {
                employee.LastLoginUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        var roles = user is null ? Array.Empty<string>() : (await _userManager.GetRolesAsync(user)).ToArray();
        return RedirectToPage(Navigation.LandingPage(roles.FirstOrDefault() ?? Roles.Employee));
    }
}
