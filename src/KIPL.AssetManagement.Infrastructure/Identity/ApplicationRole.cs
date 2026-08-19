using Microsoft.AspNetCore.Identity;

namespace KIPL.AssetManagement.Infrastructure.Identity;

public class ApplicationRole : IdentityRole
{
    public ApplicationRole() { }
    public ApplicationRole(string name) : base(name) { }
    public string? Description { get; set; }
}
