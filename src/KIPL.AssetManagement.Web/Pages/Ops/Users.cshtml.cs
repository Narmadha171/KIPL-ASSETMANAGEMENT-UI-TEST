using System.ComponentModel.DataAnnotations;
using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Application.Users;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KIPL.AssetManagement.Web.Pages.Ops;

public class UsersModel : PageModel
{
    private readonly IUserService _users;

    public UsersModel(IUserService users) => _users = users;

    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public string? Role { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string? Sort { get; set; }
    [BindProperty(SupportsGet = true)] public bool Desc { get; set; }

    public PaginatedList<UserListItemDto> Users { get; private set; } = null!;
    public IReadOnlyDictionary<int, UpdateUserCommand> EditDetails { get; private set; } = new Dictionary<int, UpdateUserCommand>();
    public UserCountsDto Counts { get; private set; } = null!;

    [BindProperty] public NewUserInput NewUser { get; set; } = new();
    [BindProperty] public EditUserInput EditInput { get; set; } = new();

    /// <summary>Reopens the add-user dialog when its post failed validation.</summary>
    public bool ReopenAddUser { get; private set; }

    /// <summary>Set to "editUser-{id}" when an edit post failed validation, so the view reopens that row's dialog.</summary>
    public string? ReopenEditModal { get; private set; }

    public SelectList Departments { get; private set; } = new(Array.Empty<object>());
    public SelectList Managers { get; private set; } = new(Array.Empty<object>());
    public IReadOnlyList<string> AllRoles { get; private set; } = Roles.All;

    public class NewUserInput
    {
        [Required, StringLength(160), Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(200), Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required, Display(Name = "Role")]
        public string RoleName { get; set; } = Roles.Employee;

        [Display(Name = "Department")]
        public int? DepartmentId { get; set; }

        [Display(Name = "Reports to")]
        public int? ManagerId { get; set; }

        [DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
        [Display(Name = "Initial password")]
        public string? Password { get; set; }
    }

    public class EditUserInput
    {
        public int Id { get; set; }

        [Required, StringLength(160), Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(200), Display(Name = "Work email")]
        public string Email { get; set; } = string.Empty;

        [Required, Display(Name = "Role")]
        public string RoleName { get; set; } = Roles.Employee;

        [Display(Name = "Department")]
        public int? DepartmentId { get; set; }

        [Display(Name = "Reporting manager")]
        public int? ManagerId { get; set; }

        [Display(Name = "Status")]
        public UserStatus Status { get; set; } = UserStatus.Active;
    }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    private async Task LoadAsync(CancellationToken ct)
    {
        Users = await _users.SearchAsync(Search, Role, PageNumber, 12, ct);
        EditDetails = await _users.GetForEditManyAsync(Users.Items.Select(u => u.Id), ct);
        Counts = await _users.GetCountsAsync(ct);
        AllRoles = await _users.GetAllRoleNamesAsync(ct);

        if (!string.IsNullOrWhiteSpace(Sort)) Users = ApplySort(Users);

        var departments = await _users.GetDepartmentsAsync(ct);
        Departments = new SelectList(departments.Select(d => new { d.Id, d.Name }), "Id", "Name");

        var employees = await _users.GetEmployeeOptionsAsync(ct);
        Managers = new SelectList(employees.Select(e => new { e.Id, e.FullName }), "Id", "FullName");
    }

    public async Task<IActionResult> OnPostAddUserAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ReopenAddUser = true;
            await LoadAsync(ct);
            return Page();
        }

        var command = new CreateUserCommand
        {
            FullName = NewUser.FullName,
            Email = NewUser.Email,
            RoleName = NewUser.RoleName,
            DepartmentId = NewUser.DepartmentId,
            ManagerId = NewUser.ManagerId,
            Password = string.IsNullOrWhiteSpace(NewUser.Password) ? "Kipl@12345" : NewUser.Password
        };

        var result = await _users.CreateAsync(command, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            ReopenAddUser = true;
            await LoadAsync(ct);
            return Page();
        }

        TempData["Success"] = $"{NewUser.FullName} can now sign in.";
        return RedirectToPage(new { Search, Role, PageNumber });
    }

    public async Task<IActionResult> OnPostEditUserAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ReopenEditModal = $"editUser-{EditInput.Id}";
            await LoadAsync(ct);
            return Page();
        }

        var command = new UpdateUserCommand
        {
            Id = EditInput.Id,
            FullName = EditInput.FullName,
            Email = EditInput.Email,
            RoleName = EditInput.RoleName,
            DepartmentId = EditInput.DepartmentId,
            ManagerId = EditInput.ManagerId,
            Status = EditInput.Status
        };

        var result = await _users.UpdateAsync(command, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            ReopenEditModal = $"editUser-{EditInput.Id}";
            await LoadAsync(ct);
            return Page();
        }

        TempData["Success"] = $"{EditInput.FullName} updated.";
        return RedirectToPage(new { Search, Role, PageNumber });
    }

    /// <summary>Sorts the current page in memory — the page size keeps this cheap.</summary>
    private PaginatedList<UserListItemDto> ApplySort(PaginatedList<UserListItemDto> page)
    {
        Func<UserListItemDto, object?> key = Sort!.ToLowerInvariant() switch
        {
            "name" => u => u.FullName,
            "role" => u => u.RoleName,
            "department" => u => u.DepartmentName,
            "assets" => u => u.AssetCount,
            "lastlogin" => u => u.LastLoginUtc ?? DateTime.MinValue,
            "status" => u => u.Status,
            _ => u => u.FullName
        };

        var sorted = (Desc ? page.Items.OrderByDescending(key) : page.Items.OrderBy(key)).ToList();
        return new PaginatedList<UserListItemDto>(sorted, page.TotalCount, page.PageNumber, page.PageSize);
    }

    public string SortLink(string column)
    {
        var flip = string.Equals(Sort, column, StringComparison.OrdinalIgnoreCase) && !Desc;
        return $"?Search={Search}&Role={Role}&PageNumber={PageNumber}&Sort={column}&Desc={flip.ToString().ToLowerInvariant()}";
    }

    public string SortArrow(string column)
        => string.Equals(Sort, column, StringComparison.OrdinalIgnoreCase) ? (Desc ? "▼" : "▲") : "";

    public async Task<IActionResult> OnPostDeactivateAsync(int id, CancellationToken ct)
    {
        var result = await _users.DeactivateAsync(id, ct);
        if (result.Succeeded) TempData["Success"] = "User deactivated.";
        else TempData["Error"] = result.Error;

        return RedirectToPage(new { Search, Role, PageNumber });
    }
}

/// <summary>Backs the per-row edit-user dialog rendered once per user row on the Users page.</summary>
public class EditUserModalModel
{
    public EditUserModalModel(UpdateUserCommand user, SelectList departments, SelectList managers, IReadOnlyList<string> roles, bool openOnLoad)
    {
        User = user;
        Departments = departments;
        Managers = managers;
        Roles = roles;
        OpenOnLoad = openOnLoad;
    }

    public UpdateUserCommand User { get; }
    public SelectList Departments { get; }
    public SelectList Managers { get; }
    public IReadOnlyList<string> Roles { get; }
    public bool OpenOnLoad { get; }
}
