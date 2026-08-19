using KIPL.AssetManagement.Domain.Common;

namespace KIPL.AssetManagement.Domain.Entities;

/// <summary>Historical record of who held an asset and when — the custody chain.</summary>
public class AssetAssignment : BaseEntity
{
    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateTime AssignedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReturnedUtc { get; set; }
    public string? AssignedBy { get; set; }
    public string? Notes { get; set; }

    public bool IsActive => ReturnedUtc is null;
}
