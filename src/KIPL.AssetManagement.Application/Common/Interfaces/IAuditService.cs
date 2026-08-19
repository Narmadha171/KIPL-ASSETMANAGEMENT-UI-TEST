namespace KIPL.AssetManagement.Application.Common.Interfaces;

public interface IAuditService
{
    /// <summary>Appends an entry to the trail. Does not call SaveChanges.</summary>
    Task LogAsync(string action, string entity, string? detail = null, CancellationToken ct = default);
}
