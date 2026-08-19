using KIPL.AssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KIPL.AssetManagement.Infrastructure.Persistence.Configurations;

public class AssetRequestConfiguration : IEntityTypeConfiguration<AssetRequest>
{
    public void Configure(EntityTypeBuilder<AssetRequest> builder)
    {
        builder.ToTable("AssetRequests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ItemName).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(r => r.RejectionReason).HasMaxLength(1000);
        builder.Property(r => r.OrderId).HasMaxLength(60);
        builder.Property(r => r.OrderBrand).HasMaxLength(160);
        builder.Property(r => r.OrderModel).HasMaxLength(160);
        builder.Property(r => r.OrderVendor).HasMaxLength(160);
        builder.Property(r => r.CourierTrackingNumber).HasMaxLength(80);
        builder.Property(r => r.OfficePickupLocation).HasMaxLength(160);
        builder.Property(r => r.CreatedBy).HasMaxLength(160);
        builder.Property(r => r.ModifiedBy).HasMaxLength(160);

        builder.Property(r => r.Status).HasConversion<int>();
        builder.Property(r => r.Source).HasConversion<int>();
        builder.Property(r => r.Mode).HasConversion<int?>();
        builder.Property(r => r.Category).HasConversion<int>();

        builder.HasIndex(r => r.Status);

        builder.HasOne(r => r.Requester).WithMany(e => e.Requests)
            .HasForeignKey(r => r.RequesterId).OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.ApprovedBy).WithMany()
            .HasForeignKey(r => r.ApprovedById).OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.ClaimedBy).WithMany()
            .HasForeignKey(r => r.ClaimedById).OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.FulfilledWithAsset).WithMany()
            .HasForeignKey(r => r.FulfilledWithAssetId).OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(r => r.IsOpen);
    }
}
