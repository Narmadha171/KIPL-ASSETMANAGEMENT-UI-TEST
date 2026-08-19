using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Inventory;
using KIPL.AssetManagement.Application.Users;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KIPL.AssetManagement.Web.Pages.Team;

[Authorize(Policy = Permissions.ViewTeam)]
public class AssetsModel : PageModel
{
    private readonly IUserService _users;
    private readonly IInventoryService _inventory;
    private readonly ICurrentUserService _currentUser;

    public AssetsModel(IUserService users, IInventoryService inventory, ICurrentUserService currentUser)
    {
        _users = users;
        _inventory = inventory;
        _currentUser = currentUser;
    }

    public record TeamMemberAssets(EmployeeOptionDto Member, IReadOnlyList<AssetListItemDto> Assets);

    public IReadOnlyList<TeamMemberAssets> Team { get; private set; } = Array.Empty<TeamMemberAssets>();
    public int TotalAssets => Team.Sum(t => t.Assets.Count);

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (_currentUser.EmployeeId is not int me) return RedirectToPage("/Account/AccessDenied");

        var reports = await _users.GetDirectReportsAsync(me, ct);

        var team = new List<TeamMemberAssets>();
        foreach (var member in reports)
            team.Add(new TeamMemberAssets(member, await _inventory.GetForEmployeeAsync(member.Id, ct)));

        Team = team;
        return Page();
    }
}
