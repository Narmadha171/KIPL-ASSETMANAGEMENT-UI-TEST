using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Domain.Enums;
using KIPL.AssetManagement.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace KIPL.AssetManagement.Application.Returns;

public class ReturnService : IReturnService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ITagGenerator _tags;
    private readonly IPhotoService _photos;

    public ReturnService(IApplicationDbContext db, IAuditService audit, ITagGenerator tags, IPhotoService photos)
    {
        _db = db;
        _audit = audit;
        _tags = tags;
        _photos = photos;
    }

    private IQueryable<AssetReturn> BaseQuery() => _db.AssetReturns
        .AsNoTracking()
        .Include(r => r.Employee)
        .Include(r => r.Asset);

    private static ReturnListItemDto Map(AssetReturn r) => new(
        r.Id, r.Reference, r.Employee.FullName, r.Asset.Brand, r.Asset.Tag, r.Asset.Category,
        r.Mode, r.Stage, r.CourierTrackingNumber, r.DropOffLocation,
        r.RaisedUtc, r.HandoverUtc, r.ExpectedUtc, r.InspectedCondition, r.InspectionNotes);

    public async Task<IReadOnlyList<ReturnListItemDto>> GetAllAsync(ReturnStage? stage, CancellationToken ct = default)
    {
        var query = BaseQuery();
        if (stage.HasValue) query = query.Where(r => r.Stage == stage);
        var rows = await query.OrderBy(r => r.Stage).ThenByDescending(r => r.RaisedUtc).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ReturnListItemDto>> GetForEmployeeAsync(int employeeId, CancellationToken ct = default)
        => (await BaseQuery().Where(r => r.EmployeeId == employeeId)
                .OrderByDescending(r => r.RaisedUtc).ToListAsync(ct))
            .Select(Map).ToList();

    public async Task<ReturnListItemDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var entity = await BaseQuery().FirstOrDefaultAsync(r => r.Id == id, ct);
        return entity is null ? null : Map(entity);
    }

    public async Task<ReturnCountsDto> GetCountsAsync(CancellationToken ct = default)
    {
        var grouped = await _db.AssetReturns.AsNoTracking()
            .GroupBy(r => r.Stage).Select(g => new { Stage = g.Key, Count = g.Count() }).ToListAsync(ct);

        int At(ReturnStage s) => grouped.FirstOrDefault(x => x.Stage == s)?.Count ?? 0;

        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var closed = await _db.AssetReturns.AsNoTracking()
            .CountAsync(r => r.Stage == ReturnStage.Closed && r.ReceivedUtc != null && r.ReceivedUtc >= monthStart, ct);

        return new ReturnCountsDto(At(ReturnStage.AwaitingPickup), At(ReturnStage.InTransit), At(ReturnStage.ReceivedForInspection), closed);
    }

    public async Task<Result<int>> RaiseAsync(RaiseReturnCommand command, CancellationToken ct = default)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == command.EmployeeId, ct);
        if (employee is null) return Result<int>.Failure("Employee not found.");

        var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == command.AssetId, ct);
        if (asset is null) return Result<int>.Failure("Asset not found.");
        if (asset.AssignedToEmployeeId != employee.Id)
            return Result<int>.Failure("You can only return an asset currently assigned to you.");

        var alreadyOpen = await _db.AssetReturns
            .AnyAsync(r => r.AssetId == asset.Id && r.Stage != ReturnStage.Closed, ct);
        if (alreadyOpen) return Result<int>.Failure($"A return is already in progress for {asset.Tag}.");

        var entity = new AssetReturn
        {
            Reference = _tags.NewReturnReference(),
            EmployeeId = employee.Id,
            AssetId = asset.Id,
            Mode = command.Mode,
            Stage = ReturnStage.AwaitingPickup,
            Reason = command.Reason?.Trim(),
            DropOffLocation = command.Mode == FulfilmentMode.Office
                ? (command.DropOffLocation ?? "IT desk, 2nd floor")
                : command.CourierService,
            ExpectedUtc = command.ExpectedUtc,
            RaisedUtc = DateTime.UtcNow
        };

        _db.AssetReturns.Add(entity);
        await _db.SaveChangesAsync(ct);

        var photoResult = await _photos.AttachAsync(
            command.Photo, PhotoKind.ReturnHandover, asset.Id, returnId: entity.Id, ct: ct);
        if (!photoResult.Succeeded) return Result<int>.Failure(photoResult.Error!);

        await _audit.LogAsync("Raised return", asset.Tag, entity.Reference, ct);
        await _db.SaveChangesAsync(ct);
        return Result<int>.Success(entity.Id);
    }

    public async Task<Result> MarkHandedOverAsync(int returnId, string? tracking, CancellationToken ct = default)
    {
        var entity = await _db.AssetReturns.Include(r => r.Asset).FirstOrDefaultAsync(r => r.Id == returnId, ct);
        if (entity is null) return Result.Failure("Return not found.");

        try { entity.MarkHandedOver(tracking); }
        catch (DomainException ex) { return Result.Failure(ex.Message); }

        entity.Asset.MarkInTransit();
        await _audit.LogAsync("Return handed over", entity.Asset.Tag, entity.Reference, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> MarkReceivedAsync(int returnId, CancellationToken ct = default)
    {
        var entity = await _db.AssetReturns.Include(r => r.Asset).FirstOrDefaultAsync(r => r.Id == returnId, ct);
        if (entity is null) return Result.Failure("Return not found.");

        entity.MarkReceived();
        await _audit.LogAsync("Return received", entity.Asset.Tag, entity.Reference, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> InspectAsync(InspectReturnCommand command, CancellationToken ct = default)
    {
        var entity = await _db.AssetReturns
            .Include(r => r.Asset).ThenInclude(a => a.Assignments)
            .FirstOrDefaultAsync(r => r.Id == command.ReturnId, ct);
        if (entity is null) return Result.Failure("Return not found.");

        var inspector = await _db.Employees.FirstOrDefaultAsync(e => e.Id == command.InspectorEmployeeId, ct);
        if (inspector is null) return Result.Failure("Inspector not found.");

        foreach (var open in entity.Asset.Assignments.Where(x => x.ReturnedUtc == null))
            open.ReturnedUtc = DateTime.UtcNow;

        try { entity.Inspect(inspector, command.Condition, command.Notes?.Trim()); }
        catch (DomainException ex) { return Result.Failure(ex.Message); }

        var photoResult = await _photos.AttachAsync(
            command.Photo, PhotoKind.ReturnInspection, entity.AssetId, returnId: entity.Id, ct: ct);
        if (!photoResult.Succeeded) return Result.Failure(photoResult.Error!);

        await _audit.LogAsync("Inspected return", entity.Asset.Tag, $"{entity.Reference} — {command.Condition}", ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
