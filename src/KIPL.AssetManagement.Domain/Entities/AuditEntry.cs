using KIPL.AssetManagement.Domain.Common;

namespace KIPL.AssetManagement.Domain.Entities;

/// <summary>Append-only trail of every state-changing action in the system.</summary>
public class AuditEntry : BaseEntity
{
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string UserName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? IpAddress { get; set; }
}
