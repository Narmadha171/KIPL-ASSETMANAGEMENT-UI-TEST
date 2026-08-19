using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Inventory;
using KIPL.AssetManagement.Application.Returns;
using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KIPL.AssetManagement.Web.Pages.Me;

public class ReturnsModel : EmployeePageModel
{
    private readonly IReturnService _returns;
    private readonly IInventoryService _inventory;

    public ReturnsModel(IReturnService returns, IInventoryService inventory, ICurrentUserService currentUser)
        : base(currentUser)
    {
        _returns = returns;
        _inventory = inventory;
    }

    [BindProperty(SupportsGet = true)] public int? AssetId { get; set; }

    public IReadOnlyList<ReturnListItemDto> Returns { get; private set; } = Array.Empty<ReturnListItemDto>();
    public IReadOnlyList<AssetListItemDto> ReturnableAssets { get; private set; } = Array.Empty<AssetListItemDto>();

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (EmployeeId is not int me) return RedirectToPage("/Account/AccessDenied");

        await LoadAsync(me, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostRaiseAsync(
        int assetId,
        FulfilmentMode mode,
        string? reason,
        string? dropOffLocation,
        string? courierService,
        IFormFile? photo,
        CancellationToken ct)
    {
        if (EmployeeId is not int me) return Forbid();

        await using var stream = photo?.OpenReadStream();
        var command = new RaiseReturnCommand
        {
            EmployeeId = me,
            AssetId = assetId,
            Mode = mode,
            Reason = reason,
            DropOffLocation = dropOffLocation,
            CourierService = courierService,
            Photo = stream is null || photo is null
                ? null
                : new PhotoUpload(stream, photo.FileName, photo.ContentType, photo.Length)
        };

        var result = await _returns.RaiseAsync(command, ct);
        Finish(result, "Return raised — IT will arrange collection.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostHandoverAsync(int returnId, string? tracking, CancellationToken ct)
    {
        if (EmployeeId is not int) return Forbid();

        Finish(await _returns.MarkHandedOverAsync(returnId, tracking, ct), "Thanks — we have marked it as handed over.");
        return RedirectToPage();
    }

    private async Task LoadAsync(int employeeId, CancellationToken ct)
    {
        Returns = await _returns.GetForEmployeeAsync(employeeId, ct);

        var open = Returns.Where(r => r.Stage != ReturnStage.Closed).Select(r => r.AssetTag).ToHashSet();
        var assets = await _inventory.GetForEmployeeAsync(employeeId, ct);

        // Do not offer an asset that already has a return in flight.
        ReturnableAssets = assets.Where(a => !open.Contains(a.Tag)).ToList();
    }
}
