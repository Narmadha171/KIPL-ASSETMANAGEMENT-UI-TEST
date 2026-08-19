using Microsoft.AspNetCore.Identity;

namespace KIPL.AssetManagement.Infrastructure.Identity;

/// <summary>Identity login account. The business record lives in Domain.Entities.Employee.</summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public int? EmployeeId { get; set; }
    public DateTime? LastLoginUtc { get; set; }
}
