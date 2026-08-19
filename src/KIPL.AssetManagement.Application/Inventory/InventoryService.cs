using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Application.Requests;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Domain.Enums;
using KIPL.AssetManagement.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace KIPL.AssetManagement.Application.Inventory;

public class InventoryService : IInventoryService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ITagGenerator _tags;
    private readonly ICurrentUserService _currentUser;

    public InventoryService(IApplicationDbContext db, IAuditService audit, ITagGenerator tags, ICurrentUserService currentUser)
    {
        _db = db;
        _audit = audit;
        _tags = tags;
        _currentUser = currentUser;
    }

    public async Task<PaginatedList<AssetListItemDto>> SearchAsync(AssetFilter filter, CancellationToken ct = default)
    {
        var query = _db.Assets.AsNoTracking().Include(a => a.AssignedToEmployee).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(a =>
                a.Tag.Contains(term) ||
                a.Brand.Contains(term) ||
                (a.Model != null && a.Model.Contains(term)) ||
                (a.SerialNumber != null && a.SerialNumber.Contains(term)) ||
                (a.AssignedToEmployee != null && a.AssignedToEmployee.FullName.Contains(term)));
        }

        if (filter.Category.HasValue) query = query.Where(a => a.Category == filter.Category);
        if (filter.Status.HasValue) query = query.Where(a => a.Status == filter.Status);
        if (filter.Condition.HasValue) query = query.Where(a => a.Condition == filter.Condition);

        var projected = query
            .OrderBy(a => a.Tag)
            .Select(a => new AssetListItemDto(
                a.Id,
                a.Tag,
                a.Category,
                a.Brand,
                a.Condition,
                a.Status,
                a.AssignedToEmployee != null ? a.AssignedToEmployee.FullName : (a.StatusNote ?? "—"),
                a.Status == AssetStatus.InStock && a.Condition != AssetCondition.Retired));

        return await PaginatedList<AssetListItemDto>.CreateAsync(projected, filter.PageNumber, filter.PageSize, ct);
    }

    public async Task<AssetDetailDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var asset = await _db.Assets.AsNoTracking()
            .Include(a => a.AssignedToEmployee)
            .Include(a => a.Assignments).ThenInclude(x => x.Employee)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (asset is null) return null;

        var history = asset.Assignments
            .OrderByDescending(x => x.AssignedUtc)
            .Select(x => new AssignmentHistoryDto(x.Employee.FullName, x.AssignedUtc, x.ReturnedUtc, x.AssignedBy))
            .ToList();

        return new AssetDetailDto(
            asset.Id, asset.Tag, asset.Category, asset.Brand, asset.Model, asset.SerialNumber,
            asset.Condition, asset.Status, asset.AssignedToEmployeeId,
            asset.AssignedToEmployee?.FullName ?? asset.StatusNote ?? "—",
            asset.PurchaseDate, asset.PurchaseCost, asset.WarrantyExpiry, asset.Location, asset.Notes,
            history);
    }

    public async Task<IReadOnlyDictionary<int, AssetDetailDto>> GetDetailsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        var assets = await _db.Assets.AsNoTracking()
            .Include(a => a.AssignedToEmployee)
            .Include(a => a.Assignments).ThenInclude(x => x.Employee)
            .Where(a => idList.Contains(a.Id))
            .ToListAsync(ct);

        var result = new Dictionary<int, AssetDetailDto>();
        foreach (var asset in assets)
        {
            var history = asset.Assignments
                .OrderByDescending(x => x.AssignedUtc)
                .Select(x => new AssignmentHistoryDto(x.Employee.FullName, x.AssignedUtc, x.ReturnedUtc, x.AssignedBy))
                .ToList();

            result[asset.Id] = new AssetDetailDto(
                asset.Id, asset.Tag, asset.Category, asset.Brand, asset.Model, asset.SerialNumber,
                asset.Condition, asset.Status, asset.AssignedToEmployeeId,
                asset.AssignedToEmployee?.FullName ?? asset.StatusNote ?? "—",
                asset.PurchaseDate, asset.PurchaseCost, asset.WarrantyExpiry, asset.Location, asset.Notes,
                history);
        }

        return result;
    }

    public async Task<InventoryCountsDto> GetCountsAsync(CancellationToken ct = default)
    {
        var grouped = await _db.Assets.AsNoTracking()
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int At(AssetStatus s) => grouped.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

        return new InventoryCountsDto(
            grouped.Sum(x => x.Count),
            At(AssetStatus.InStock),
            At(AssetStatus.Assigned),
            At(AssetStatus.InTransit),
            At(AssetStatus.UnderService),
            At(AssetStatus.Retired),
            At(AssetStatus.Lost));
    }

    public async Task<IReadOnlyList<AssetListItemDto>> GetAssignableAsync(AssetCategory? category, CancellationToken ct = default)
    {
        var query = _db.Assets.AsNoTracking()
            .Where(a => a.Status == AssetStatus.InStock && a.Condition != AssetCondition.Retired);

        if (category.HasValue) query = query.Where(a => a.Category == category);

        return await query
            .OrderBy(a => a.Tag)
            .Select(a => new AssetListItemDto(a.Id, a.Tag, a.Category, a.Brand, a.Condition, a.Status, "—", true))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AssetListItemDto>> GetForEmployeeAsync(int employeeId, CancellationToken ct = default)
        => await _db.Assets.AsNoTracking()
            .Where(a => a.AssignedToEmployeeId == employeeId)
            .OrderBy(a => a.Category)
            .Select(a => new AssetListItemDto(a.Id, a.Tag, a.Category, a.Brand, a.Condition, a.Status, "", false))
            .ToListAsync(ct);

    public async Task<Result<int>> CreateAsync(CreateAssetCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Brand))
            return Result<int>.Failure("Brand is required.");

        var tag = string.IsNullOrWhiteSpace(command.Tag) ? _tags.NewAssetTag(command.Category) : command.Tag.Trim().ToUpperInvariant();

        if (await _db.Assets.AnyAsync(a => a.Tag == tag, ct))
            return Result<int>.Failure($"Asset tag {tag} is already in use.");

        var asset = new Asset
        {
            Tag = tag,
            Category = command.Category,
            Brand = command.Brand.Trim(),
            Model = command.Model?.Trim(),
            SerialNumber = command.SerialNumber?.Trim(),
            Condition = command.Condition,
            Status = AssetStatus.InStock,
            PurchaseDate = command.PurchaseDate,
            PurchaseCost = command.PurchaseCost,
            WarrantyExpiry = command.WarrantyExpiry,
            Location = command.Location?.Trim(),
            Notes = command.Notes?.Trim()
        };

        _db.Assets.Add(asset);
        await _audit.LogAsync("Asset created", tag, ct: ct);
        await _db.SaveChangesAsync(ct);
        return Result<int>.Success(asset.Id);
    }

    public async Task<Result> UpdateAsync(UpdateAssetCommand command, CancellationToken ct = default)
    {
        var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == command.Id, ct);
        if (asset is null) return Result.Failure("Asset not found.");

        asset.Category = command.Category;
        asset.Brand = command.Brand.Trim();
        asset.Model = command.Model?.Trim();
        asset.SerialNumber = command.SerialNumber?.Trim();
        asset.Condition = command.Condition;
        asset.PurchaseDate = command.PurchaseDate;
        asset.PurchaseCost = command.PurchaseCost;
        asset.WarrantyExpiry = command.WarrantyExpiry;
        asset.Location = command.Location?.Trim();
        asset.Notes = command.Notes?.Trim();
        asset.ModifiedUtc = DateTime.UtcNow;

        await _audit.LogAsync("Asset edited", asset.Tag, ct: ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<ChallanDto>> AssignAsync(
        int assetId, int employeeId, string? notes,
        FulfilmentMode mode, string? courierService, string? trackingNumber, string? pickupPoint,
        CancellationToken ct = default)
    {
        var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == assetId, ct);
        if (asset is null) return Result<ChallanDto>.Failure("Asset not found.");

        var employee = await _db.Employees.AsNoTracking().Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee is null) return Result<ChallanDto>.Failure("Employee not found.");

        try
        {
            asset.AssignTo(employee);
        }
        catch (DomainException ex)
        {
            return Result<ChallanDto>.Failure(ex.Message);
        }

        _db.AssetAssignments.Add(new AssetAssignment
        {
            AssetId = asset.Id,
            EmployeeId = employee.Id,
            AssignedUtc = DateTime.UtcNow,
            Notes = notes
        });

        await _audit.LogAsync("Assigned asset", asset.Tag, $"to {employee.FullName}", ct);
        await _db.SaveChangesAsync(ct);

        var challan = new ChallanDto(
            $"KIPL/DC/{DateTime.UtcNow:yyyy}/A{asset.Id:D4}",
            DateTime.UtcNow,
            _currentUser.UserName ?? "IT Department",
            _currentUser.RoleName,
            employee.FullName,
            $"Department: {employee.Department?.Name ?? "—"} · {employee.Email}",
            mode,
            courierService,
            trackingNumber,
            mode == FulfilmentMode.Office ? (pickupPoint ?? "IT Desk, 2nd Floor, KIPL Office") : null,
            new List<ChallanLineDto> { new(asset.Brand, asset.Category.ToString(), asset.Tag, asset.Condition.ToString()) });

        return Result<ChallanDto>.Success(challan);
    }

    public async Task<Result> UnassignAsync(int assetId, CancellationToken ct = default)
    {
        var asset = await _db.Assets.Include(a => a.Assignments).FirstOrDefaultAsync(a => a.Id == assetId, ct);
        if (asset is null) return Result.Failure("Asset not found.");

        var open = asset.Assignments.FirstOrDefault(x => x.ReturnedUtc == null);
        if (open is not null) open.ReturnedUtc = DateTime.UtcNow;

        asset.ReturnToStock();
        await _audit.LogAsync("Asset returned to stock", asset.Tag, ct: ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RetireAsync(int assetId, string? reason, CancellationToken ct = default)
    {
        var asset = await _db.Assets.Include(a => a.Assignments).FirstOrDefaultAsync(a => a.Id == assetId, ct);
        if (asset is null) return Result.Failure("Asset not found.");

        foreach (var open in asset.Assignments.Where(x => x.ReturnedUtc == null))
            open.ReturnedUtc = DateTime.UtcNow;

        asset.Retire(reason);
        await _audit.LogAsync("Retired asset", asset.Tag, reason, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ReportLostAsync(int assetId, string? lastSeenWith, CancellationToken ct = default)
    {
        var asset = await _db.Assets.Include(a => a.AssignedToEmployee).FirstOrDefaultAsync(a => a.Id == assetId, ct);
        if (asset is null) return Result.Failure("Asset not found.");

        var holder = lastSeenWith ?? asset.AssignedToEmployee?.FullName;
        asset.ReportLost(holder);
        await _audit.LogAsync("Reported lost", asset.Tag, holder, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> SendToServiceAsync(int assetId, CancellationToken ct = default)
    {
        var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == assetId, ct);
        if (asset is null) return Result.Failure("Asset not found.");

        asset.SendToService();
        await _audit.LogAsync("Sent to service", asset.Tag, ct: ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
