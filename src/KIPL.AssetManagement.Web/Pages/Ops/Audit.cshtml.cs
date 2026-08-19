using KIPL.AssetManagement.Application.Audit;
using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KIPL.AssetManagement.Web.Pages.Ops;

[Authorize(Policy = Permissions.ViewAudit)]
public class AuditModel : PageModel
{
    private readonly IAuditQueryService _audit;

    public AuditModel(IAuditQueryService audit) => _audit = audit;

    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public string? ActionName { get; set; }
    [BindProperty(SupportsGet = true)] public string? RoleName { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? FromUtc { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? ToUtc { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public PaginatedList<AuditEntryDto> Entries { get; private set; } = null!;
    public IReadOnlyList<string> Actions { get; private set; } = Array.Empty<string>();

    private AuditFilter Filter => new()
    {
        Search = Search,
        Action = ActionName,
        RoleName = RoleName,
        FromUtc = FromUtc,
        ToUtc = ToUtc,
        PageNumber = PageNumber,
        PageSize = 25
    };

    public async Task OnGetAsync(CancellationToken ct)
    {
        Entries = await _audit.SearchAsync(Filter, ct);
        Actions = await _audit.GetDistinctActionsAsync(ct);
    }

    public async Task<IActionResult> OnGetExportAsync(CancellationToken ct)
    {
        var csv = await _audit.ExportCsvAsync(Filter, ct);
        return File(csv, "text/csv", $"kipl-audit-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv");
    }
}
