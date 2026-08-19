using KIPL.AssetManagement.Domain.Common;

namespace KIPL.AssetManagement.Domain.Entities;

/// <summary>
/// Editable role-to-permission mapping backing the "Roles and permissions" screen.
/// Identity role claims are re-synced from this table whenever it changes.
/// </summary>
public class RolePermission : BaseEntity
{
    public string RoleName { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
}
