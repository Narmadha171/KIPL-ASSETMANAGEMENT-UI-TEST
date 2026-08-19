using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Application.Returns;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KIPL.AssetManagement.Web.Pages.Ops;

public class ReturnTrackingModel : PageModel
{
    private readonly IReturnService _returns;
    private readonly ICurrentUserService _currentUser;

    public ReturnTrackingModel(IReturnService returns, ICurrentUserService currentUser)
    {
        _returns = returns;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public ReturnStage? Stage { get; set; }

    public IReadOnlyList<ReturnListItemDto> Returns { get; private set; } = Array.Empty<ReturnListItemDto>();
    public ReturnCountsDto Counts { get; private set; } = null!;

    public bool CanInspect => _currentUser.HasPermission(Permissions.InspectReturns);

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    private async Task LoadAsync(CancellationToken ct)
    {
        Returns = await _returns.GetAllAsync(Stage, ct);
        Counts = await _returns.GetCountsAsync(ct);
    }

    public async Task<IActionResult> OnPostReceiveAsync(int returnId, CancellationToken ct)
    {
        if (!CanInspect) return Forbid();

        Finish(await _returns.MarkReceivedAsync(returnId, ct), "Marked as received — ready for inspection.");
        return RedirectToPage(new { Stage });
    }

    public async Task<IActionResult> OnPostInspectAsync(
        int returnId, AssetCondition condition, string? notes, IFormFile? photo, CancellationToken ct)
    {
        if (!CanInspect) return Forbid();
        if (_currentUser.EmployeeId is not int me) return Forbid();

        await using var stream = photo?.OpenReadStream();
        var command = new InspectReturnCommand
        {
            ReturnId = returnId,
            InspectorEmployeeId = me,
            Condition = condition,
            Notes = notes,
            Photo = stream is null || photo is null
                ? null
                : new PhotoUpload(stream, photo.FileName, photo.ContentType, photo.Length)
        };

        Finish(await _returns.InspectAsync(command, ct), "Inspection recorded and the asset routed accordingly.");
        return RedirectToPage(new { Stage });
    }

    private void Finish(Result result, string success)
    {
        if (result.Succeeded) TempData["Success"] = success;
        else TempData["Error"] = result.Error;
    }
}
