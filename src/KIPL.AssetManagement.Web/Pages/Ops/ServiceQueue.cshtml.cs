using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Application.ServiceRequests;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KIPL.AssetManagement.Web.Pages.Ops;

[Authorize(Policy = Permissions.ManageInventory)]
public class ServiceQueueModel : PageModel
{
    private readonly IServiceRequestService _service;

    public ServiceQueueModel(IServiceRequestService service) => _service = service;

    [BindProperty(SupportsGet = true)] public ServiceRequestStatus? Status { get; set; }

    public IReadOnlyList<ServiceRequestDto> Items { get; private set; } = Array.Empty<ServiceRequestDto>();
    public int OpenCount { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Items = await _service.GetAllAsync(Status, ct);
        OpenCount = await _service.GetOpenCountAsync(ct);
    }

    public async Task<IActionResult> OnPostAcceptAsync(int id, CancellationToken ct)
    {
        Finish(await _service.AcceptAsync(id, ct), "Accepted — the asset is now under service.");
        return RedirectToPage(new { Status });
    }

    public async Task<IActionResult> OnPostDismissAsync(int id, string reason, CancellationToken ct)
    {
        Finish(await _service.DismissAsync(id, reason, ct), "Report closed.");
        return RedirectToPage(new { Status });
    }

    public async Task<IActionResult> OnPostResolveAsync(int id, string resolution, CancellationToken ct)
    {
        Finish(await _service.ResolveAsync(id, resolution, ct), "Resolved — asset returned to stock.");
        return RedirectToPage(new { Status });
    }

    private void Finish(Result result, string success)
    {
        if (result.Succeeded) TempData["Success"] = success;
        else TempData["Error"] = result.Error;
    }
}
