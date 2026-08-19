using System.Globalization;
using System.Text;
using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KIPL.AssetManagement.Application.Audit;

public class AuditQueryService : IAuditQueryService
{
    private readonly IApplicationDbContext _db;

    public AuditQueryService(IApplicationDbContext db) => _db = db;

    private IQueryable<AuditEntry> Filtered(AuditFilter filter)
    {
        var query = _db.AuditEntries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(a => a.UserName.Contains(term) || a.Entity.Contains(term) || a.Action.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(filter.Action)) query = query.Where(a => a.Action == filter.Action);
        if (!string.IsNullOrWhiteSpace(filter.RoleName)) query = query.Where(a => a.RoleName == filter.RoleName);
        if (filter.FromUtc.HasValue) query = query.Where(a => a.TimestampUtc >= filter.FromUtc);
        if (filter.ToUtc.HasValue) query = query.Where(a => a.TimestampUtc <= filter.ToUtc);

        return query.OrderByDescending(a => a.TimestampUtc);
    }

    public async Task<PaginatedList<AuditEntryDto>> SearchAsync(AuditFilter filter, CancellationToken ct = default)
    {
        var projected = Filtered(filter)
            .Select(a => new AuditEntryDto(a.Id, a.TimestampUtc, a.UserName, a.RoleName, a.Action, a.Entity, a.Detail));
        return await PaginatedList<AuditEntryDto>.CreateAsync(projected, filter.PageNumber, filter.PageSize, ct);
    }

    public async Task<IReadOnlyList<AuditEntryDto>> GetRecentAsync(int take, CancellationToken ct = default)
        => await _db.AuditEntries.AsNoTracking()
            .OrderByDescending(a => a.TimestampUtc).Take(take)
            .Select(a => new AuditEntryDto(a.Id, a.TimestampUtc, a.UserName, a.RoleName, a.Action, a.Entity, a.Detail))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> GetDistinctActionsAsync(CancellationToken ct = default)
        => await _db.AuditEntries.AsNoTracking().Select(a => a.Action).Distinct().OrderBy(a => a).ToListAsync(ct);

    public async Task<byte[]> ExportCsvAsync(AuditFilter filter, CancellationToken ct = default)
    {
        var rows = await Filtered(filter).Take(10_000).ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Timestamp (UTC),User,Role,Action,Entity,Detail");
        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(',',
                Csv(r.TimestampUtc.ToString("u", CultureInfo.InvariantCulture)),
                Csv(r.UserName), Csv(r.RoleName), Csv(r.Action), Csv(r.Entity), Csv(r.Detail ?? "")));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string Csv(string value)
        => value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
