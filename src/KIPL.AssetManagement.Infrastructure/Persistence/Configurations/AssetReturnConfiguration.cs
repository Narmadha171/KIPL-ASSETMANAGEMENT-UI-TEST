using KIPL.AssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KIPL.AssetManagement.Infrastructure.Persistence.Configurations;

public class AssetReturnConfiguration : IEntityTypeConfiguration<AssetReturn>
{
    public void Configure(EntityTypeBuilder<AssetReturn> builder)
    {
        builder.ToTable("AssetReturns");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Reference).HasMaxLength(40).IsRequired();
        builder.HasIndex(r => r.Reference).IsUnique();

        builder.Property(r => r.Reason).HasMaxLength(1000);
        builder.Property(r => r.CourierTrackingNumber).HasMaxLength(80);
        builder.Property(r => r.DropOffLocation).HasMaxLength(160);
        builder.Property(r => r.InspectionNotes).HasMaxLength(1000);
        builder.Property(r => r.CreatedBy).HasMaxLength(160);
        builder.Property(r => r.ModifiedBy).HasMaxLength(160);

        builder.Property(r => r.Mode).HasConversion<int>();
        builder.Property(r => r.Stage).HasConversion<int>();
        builder.Property(r => r.InspectedCondition).HasConversion<int?>();

        builder.HasIndex(r => r.Stage);

        builder.HasOne(r => r.Employee).WithMany()
            .HasForeignKey(r => r.EmployeeId).OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.Asset).WithMany()
            .HasForeignKey(r => r.AssetId).OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.InspectedBy).WithMany()
            .HasForeignKey(r => r.InspectedById).OnDelete(DeleteBehavior.NoAction);

        builder.Ignore(r => r.IsClosed);
    }
}
