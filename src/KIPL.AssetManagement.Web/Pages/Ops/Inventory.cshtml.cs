using System.Text.Json;
using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Application.Inventory;
using KIPL.AssetManagement.Application.Requests;
using KIPL.AssetManagement.Application.Users;
using KIPL.AssetManagement.Web.Pages.Shared;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KIPL.AssetManagement.Web.Pages.Ops;

public class InventoryModel : PageModel
{
    private readonly IInventoryService _inventory;
    private readonly IUserService _users;
    private readonly ICurrentUserService _currentUser;
    private readonly IPhotoService _photos;

    public InventoryModel(
        IInventoryService inventory,
        IUserService users,
        ICurrentUserService currentUser,
        IPhotoService photos)
    {
        _inventory = inventory;
        _users = users;
        _currentUser = currentUser;
        _photos = photos;
    }

    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public AssetCategory? Category { get; set; }
    [BindProperty(SupportsGet = true)] public AssetStatus? Status { get; set; }
    [BindProperty(SupportsGet = true)] public AssetCondition? Condition { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public PaginatedList<AssetListItemDto> Assets { get; private set; } = null!;
    public IReadOnlyDictionary<int, AssetDetailDto> Details { get; private set; } = new Dictionary<int, AssetDetailDto>();
    public InventoryCountsDto Counts { get; private set; } = null!;
    public IReadOnlyList<EmployeeOptionDto> Employees { get; private set; } = Array.Empty<EmployeeOptionDto>();

    [BindProperty] public AssetFormInput Input { get; set; } = new();

    /// <summary>Set when a dialog post failed validation, so the view reopens it.</summary>
    public string? ReopenModal { get; private set; }

    /// <summary>Set right after a direct assign, so the delivery challan modal auto-opens.</summary>
    public ChallanDto? Challan { get; private set; }

    public bool CanManage => _currentUser.HasPermission(Permissions.ManageInventory);
    public bool CanAssign => _currentUser.HasPermission(Permissions.AssignAsset);
    public bool CanRetire => _currentUser.HasPermission(Permissions.RetireAsset);

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);

        if (TempData["ChallanJson"] is string json && !string.IsNullOrWhiteSpace(json))
        {
            Challan = JsonSerializer.Deserialize<ChallanDto>(json);
        }
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Assets = await _inventory.SearchAsync(new AssetFilter
        {
            Search = Search,
            Category = Category,
            Status = Status,
            Condition = Condition,
            PageNumber = PageNumber,
            PageSize = 12
        }, ct);

        Details = await _inventory.GetDetailsAsync(Assets.Items.Select(a => a.Id), ct);
        Counts = await _inventory.GetCountsAsync(ct);
        Employees = await _users.GetEmployeeOptionsAsync(ct);
    }

    public async Task<IActionResult> OnPostAssignAsync(
        int assetId, int employeeId, string? notes,
        FulfilmentMode mode, string? courierService, string? trackingNumber, string? pickupPoint,
        IFormFile? photo, CancellationToken ct)
    {
        if (!CanAssign) return Forbid();

        var result = await _inventory.AssignAsync(assetId, employeeId, notes, mode, courierService, trackingNumber, pickupPoint, ct);
        if (result.Succeeded && photo is not null && photo.Length > 0)
        {
            await using var stream = photo.OpenReadStream();
            var upload = new PhotoUpload(stream, photo.FileName, photo.ContentType, photo.Length);
            var photoResult = await _photos.AttachAsync(upload, PhotoKind.Assignment, assetId, saveChanges: true, ct: ct);
            if (!photoResult.Succeeded) TempData["Error"] = photoResult.Error;
        }

        if (result.Succeeded)
        {
            TempData["Success"] = "Asset assigned — delivery challan generated.";
            TempData["ChallanJson"] = JsonSerializer.Serialize(result.Value);
            TempData["OpenModal"] = "deliveryChallan";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToPage(new { Search, Category, Status, Condition, PageNumber });
    }

    public async Task<IActionResult> OnPostUnassignAsync(int assetId, CancellationToken ct)
    {
        if (!CanAssign) return Forbid();

        var result = await _inventory.UnassignAsync(assetId, ct);
        SetMessage(result, "Asset returned to stock.");
        return RedirectToPage(new { Search, Category, Status, Condition, PageNumber });
    }

    public async Task<IActionResult> OnPostRetireAsync(int assetId, string? reason, CancellationToken ct)
    {
        if (!CanRetire) return Forbid();

        var result = await _inventory.RetireAsync(assetId, reason, ct);
        SetMessage(result, "Asset retired.");
        return RedirectToPage(new { Search, Category, Status, Condition, PageNumber });
    }

    public async Task<IActionResult> OnPostReportLostAsync(int assetId, string? lastSeenWith, CancellationToken ct)
    {
        if (!CanRetire) return Forbid();

        var result = await _inventory.ReportLostAsync(assetId, lastSeenWith, ct);
        SetMessage(result, "Asset reported lost.");
        return RedirectToPage(new { Search, Category, Status, Condition, PageNumber });
    }

    public async Task<IActionResult> OnPostServiceAsync(int assetId, CancellationToken ct)
    {
        if (!CanManage) return Forbid();

        var result = await _inventory.SendToServiceAsync(assetId, ct);
        SetMessage(result, "Asset sent to service.");
        return RedirectToPage(new { Search, Category, Status, Condition, PageNumber });
    }

    public async Task<IActionResult> OnPostAddAssetAsync(CancellationToken ct)
    {
        if (!CanManage) return Forbid();

        var command = new CreateAssetCommand
        {
            Tag = Input.Tag,
            Category = Input.Category,
            Brand = Input.Brand,
            Model = Input.Model,
            SerialNumber = Input.SerialNumber,
            Condition = Input.Condition,
            PurchaseDate = Input.PurchaseDate,
            PurchaseCost = Input.PurchaseCost,
            WarrantyExpiry = Input.WarrantyExpiry,
            Location = Input.Location,
            Notes = Input.Notes
        };

        var result = await _inventory.CreateAsync(command, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            ReopenModal = "addAsset";
            await LoadAsync(ct);
            return Page();
        }

        TempData["Success"] = "Asset added to inventory.";
        return RedirectToPage(new { Search, Category, Status, Condition, PageNumber });
    }

    public async Task<IActionResult> OnPostSaveAssetAsync(CancellationToken ct)
    {
        if (!CanManage) return Forbid();

        var command = new UpdateAssetCommand
        {
            Id = Input.Id,
            Tag = Input.Tag,
            Category = Input.Category,
            Brand = Input.Brand,
            Model = Input.Model,
            SerialNumber = Input.SerialNumber,
            Condition = Input.Condition,
            PurchaseDate = Input.PurchaseDate,
            PurchaseCost = Input.PurchaseCost,
            WarrantyExpiry = Input.WarrantyExpiry,
            Location = Input.Location,
            Notes = Input.Notes
        };

        var result = await _inventory.UpdateAsync(command, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            ReopenModal = $"editAsset-{Input.Id}";
            await LoadAsync(ct);
            return Page();
        }

        TempData["Success"] = "Asset updated.";
        return RedirectToPage(new { Search, Category, Status, Condition, PageNumber });
    }

    private void SetMessage(Result result, string success)
    {
        if (result.Succeeded) TempData["Success"] = success;
        else TempData["Error"] = result.Error;
    }
}
