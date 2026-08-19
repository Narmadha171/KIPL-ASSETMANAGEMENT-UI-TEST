using KIPL.AssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KIPL.AssetManagement.Infrastructure.Persistence.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Tag).HasMaxLength(40).IsRequired();
        builder.HasIndex(a => a.Tag).IsUnique();

        builder.Property(a => a.Brand).HasMaxLength(160).IsRequired();
        builder.Property(a => a.Model).HasMaxLength(160);
        builder.Property(a => a.SerialNumber).HasMaxLength(120);
        builder.Property(a => a.StatusNote).HasMaxLength(240);
        builder.Property(a => a.Location).HasMaxLength(160);
        builder.Property(a => a.Notes).HasMaxLength(1000);
        builder.Property(a => a.PurchaseCost).HasColumnType("decimal(18,2)");
        builder.Property(a => a.CreatedBy).HasMaxLength(160);
        builder.Property(a => a.ModifiedBy).HasMaxLength(160);

        builder.Property(a => a.Category).HasConversion<int>();
        builder.Property(a => a.Condition).HasConversion<int>();
        builder.Property(a => a.Status).HasConversion<int>();

        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.Category);

        builder.HasOne(a => a.AssignedToEmployee)
            .WithMany(e => e.AssignedAssets)
            .HasForeignKey(a => a.AssignedToEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(a => a.IsAssignable);
    }
}
