using KIPL.AssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KIPL.AssetManagement.Infrastructure.Persistence.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RoleName).HasMaxLength(60).IsRequired();
        builder.Property(r => r.Permission).HasMaxLength(80).IsRequired();

        builder.HasIndex(r => new { r.RoleName, r.Permission }).IsUnique();
    }
}
