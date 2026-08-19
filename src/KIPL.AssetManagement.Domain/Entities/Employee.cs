using KIPL.AssetManagement.Domain.Common;
using KIPL.AssetManagement.Domain.Enums;

namespace KIPL.AssetManagement.Domain.Entities;

/// <summary>
/// The domain-side person record. Kept separate from the Identity user so the
/// Domain project stays free of any ASP.NET dependency; the two are linked by
/// <see cref="IdentityUserId"/>.
/// </summary>
public class Employee : AuditableEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? IdentityUserId { get; set; }

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    /// <summary>Primary role label, mirrored from Identity for fast display/filtering.</summary>
    public string RoleName { get; set; } = Roles.Employee;

    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime? LastLoginUtc { get; set; }

    /// <summary>Reporting manager, used to scope the "Team Approvals" and "Team Assets" pages.</summary>
    public int? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public ICollection<Employee> DirectReports { get; set; } = new List<Employee>();

    public ICollection<Asset> AssignedAssets { get; set; } = new List<Asset>();
    public ICollection<AssetRequest> Requests { get; set; } = new List<AssetRequest>();

    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FullName)) return "?";
            var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 1
                ? parts[0][..1].ToUpperInvariant()
                : (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
        }
    }
}
