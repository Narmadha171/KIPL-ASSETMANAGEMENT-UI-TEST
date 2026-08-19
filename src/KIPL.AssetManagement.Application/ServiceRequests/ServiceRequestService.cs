using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KIPL.AssetManagement.Application.ServiceRequests;

public class ServiceRequestService : IServiceRequestService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ITagGenerator _tags;

    public ServiceRequestService(IApplicationDbContext db, IAuditService audit, ITagGenerator tags)
    {
        _db = db;
        _audit = audit;
        _tags = tags;
    }

    private static ServiceRequestDto Map(ServiceRequest s) => new(
        s.Id, s.Reference, s.Asset.Brand, s.Asset.Tag, s.ReportedBy.FullName,
        s.IssueType, s.Description, s.Urgency, s.Status, s.RaisedUtc, s.ResolvedUtc, s.Resolution);

    private IQueryable<ServiceRequest> BaseQuery() => _db.ServiceRequests
        .AsNoTracking().Include(s => s.Asset).Include(s => s.ReportedBy);

    public async Task<IReadOnlyList<ServiceRequestDto>> GetAllAsync(ServiceRequestStatus? status, CancellationToken ct = default)
    {
        var query = BaseQuery();
        if (status.HasValue) query = query.Where(s => s.Status == status);

        var rows = await query.OrderByDescending(s => s.Urgency).ThenByDescending(s => s.RaisedUtc).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ServiceRequestDto>> GetForEmployeeAsync(int employeeId, CancellationToken ct = default)
        => (await BaseQuery().Where(s => s.ReportedById == employeeId)
                .OrderByDescending(s => s.RaisedUtc).ToListAsync(ct))
            .Select(Map).ToList();

    public async Task<int> GetOpenCountAsync(CancellationToken ct = default)
        => await _db.ServiceRequests.CountAsync(
            s => s.Status == ServiceRequestStatus.Open || s.Status == ServiceRequestStatus.InProgress, ct);

    public async Task<Result<int>> ReportAsync(ReportIssueCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Description))
            return Result<int>.Failure("Please describe what is going wrong.");

        var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == command.AssetId, ct);
        if (asset is null) return Result<int>.Failure("Asset not found.");

        var entity = new ServiceRequest
        {
            Reference = _tags.NewServiceReference(),
            AssetId = asset.Id,
            ReportedById = command.ReportedById,
            IssueType = string.IsNullOrWhiteSpace(command.IssueType) ? "General" : command.IssueType.Trim(),
            Description = command.Description.Trim(),
            Urgency = command.Urgency,
            Status = ServiceRequestStatus.Open,
            RaisedUtc = DateTime.UtcNow
        };

        _db.ServiceRequests.Add(entity);
        await _audit.LogAsync("Reported issue", asset.Tag, entity.Reference, ct);
        await _db.SaveChangesAsync(ct);
        return Result<int>.Success(entity.Id);
    }

    public async Task<Result> AcceptAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.ServiceRequests.Include(s => s.Asset).FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entity is null) return Result.Failure("Service request not found.");
        if (entity.Status != ServiceRequestStatus.Open)
            return Result.Failure("Only an open report can be accepted.");

        entity.Status = ServiceRequestStatus.InProgress;
        entity.Asset.SendToService();

        await _audit.LogAsync("Accepted service request", entity.Reference, entity.Asset.Tag, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DismissAsync(int id, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure("Tell the reporter why this is being closed.");

        var entity = await _db.ServiceRequests.Include(s => s.Asset).FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entity is null) return Result.Failure("Service request not found.");

        entity.Status = ServiceRequestStatus.Closed;
        entity.Resolution = reason.Trim();
        entity.ResolvedUtc = DateTime.UtcNow;

        await _audit.LogAsync("Dismissed service request", entity.Reference, reason.Trim(), ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ResolveAsync(int id, string resolution, CancellationToken ct = default)
    {
        var entity = await _db.ServiceRequests.Include(s => s.Asset).FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entity is null) return Result.Failure("Service request not found.");

        entity.Status = ServiceRequestStatus.Resolved;
        entity.Resolution = resolution?.Trim();
        entity.ResolvedUtc = DateTime.UtcNow;

        // A repaired asset goes back into circulation.
        if (entity.Asset.Status == AssetStatus.UnderService) entity.Asset.ReturnToStock();

        await _audit.LogAsync("Resolved service request", entity.Reference, ct: ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
