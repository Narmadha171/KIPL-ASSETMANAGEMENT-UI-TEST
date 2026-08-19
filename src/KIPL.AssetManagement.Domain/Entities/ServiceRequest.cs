using KIPL.AssetManagement.Domain.Common;
using KIPL.AssetManagement.Domain.Enums;

namespace KIPL.AssetManagement.Domain.Entities;

/// <summary>A fault reported by an employee against an asset they hold.</summary>
public class ServiceRequest : AuditableEntity
{
    public string Reference { get; set; } = string.Empty;   // e.g. SR-1041

    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    public int ReportedById { get; set; }
    public Employee ReportedBy { get; set; } = null!;

    public string IssueType { get; set; } = string.Empty;    // Battery, Screen, Keyboard, ...
    public string Description { get; set; } = string.Empty;
    public ServiceUrgency Urgency { get; set; } = ServiceUrgency.Minor;
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Open;

    public DateTime RaisedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedUtc { get; set; }
    public string? Resolution { get; set; }
}
