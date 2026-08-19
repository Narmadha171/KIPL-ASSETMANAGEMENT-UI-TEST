using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Inventory;
using KIPL.AssetManagement.Application.ServiceRequests;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace KIPL.AssetManagement.Web.Pages.Me;

public class AssetsModel : EmployeePageModel
{
    private readonly IInventoryService _inventory;
    private readonly IServiceRequestService _service;

    public AssetsModel(IInventoryService inventory, IServiceRequestService service, ICurrentUserService currentUser)
        : base(currentUser)
    {
        _inventory = inventory;
        _service = service;
    }

    public IReadOnlyList<AssetListItemDto> Assets { get; private set; } = Array.Empty<AssetListItemDto>();
    public IReadOnlyList<ServiceRequestDto> MyIssues { get; private set; } = Array.Empty<ServiceRequestDto>();

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (EmployeeId is not int me) return RedirectToPage("/Account/AccessDenied");

        Assets = await _inventory.GetForEmployeeAsync(me, ct);
        MyIssues = await _service.GetForEmployeeAsync(me, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostReportAsync(
        int assetId, string issueType, string description, ServiceUrgency urgency, CancellationToken ct)
    {
        if (EmployeeId is not int me) return Forbid();

        var command = new ReportIssueCommand
        {
            AssetId = assetId,
            ReportedById = me,
            IssueType = issueType,
            Description = description,
            Urgency = urgency
        };

        var result = await _service.ReportAsync(command, ct);
        Finish(result, "Issue reported — IT will pick it up from the service queue.");
        return RedirectToPage();
    }
}
