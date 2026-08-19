namespace KIPL.AssetManagement.Domain.Common;

/// <summary>Base for all persisted aggregate roots / entities.</summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
}

/// <summary>Adds standard creation / modification auditing columns.</summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedUtc { get; set; }
    public string? ModifiedBy { get; set; }
}
