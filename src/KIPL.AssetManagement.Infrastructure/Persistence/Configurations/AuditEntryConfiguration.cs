using KIPL.AssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KIPL.AssetManagement.Infrastructure.Persistence.Configurations;

public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserName).HasMaxLength(160).IsRequired();
        builder.Property(a => a.RoleName).HasMaxLength(60).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(120).IsRequired();
        builder.Property(a => a.Entity).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Detail).HasMaxLength(1000);
        builder.Property(a => a.IpAddress).HasMaxLength(64);

        builder.HasIndex(a => a.TimestampUtc);
        builder.HasIndex(a => a.Action);
    }
}
