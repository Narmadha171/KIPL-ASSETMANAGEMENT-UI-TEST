using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Infrastructure.Persistence;

namespace KIPL.AssetManagement.Infrastructure.Services;

/// <summary>
/// Appends to the audit trail using the ambient user. The entry is added to the
/// change tracker but not saved — the calling service saves it in the same
/// transaction as the change it describes, so the trail can never drift.
/// </summary>
public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AuditService(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task LogAsync(string action, string entity, string? detail = null, CancellationToken ct = default)
    {
        _db.AuditEntries.Add(new AuditEntry
        {
            TimestampUtc = DateTime.UtcNow,
            UserName = _currentUser.UserName ?? "system",
            RoleName = _currentUser.RoleName,
            Action = action,
            Entity = entity,
            Detail = detail
        });

        return Task.CompletedTask;
    }
}
