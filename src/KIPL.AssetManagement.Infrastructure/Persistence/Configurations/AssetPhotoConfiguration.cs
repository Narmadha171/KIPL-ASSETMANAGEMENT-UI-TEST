using KIPL.AssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KIPL.AssetManagement.Infrastructure.Persistence.Configurations;

public class AssetPhotoConfiguration : IEntityTypeConfiguration<AssetPhoto>
{
    public void Configure(EntityTypeBuilder<AssetPhoto> builder)
    {
        builder.ToTable("AssetPhotos");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.StoredPath).HasMaxLength(400).IsRequired();
        builder.Property(p => p.OriginalFileName).HasMaxLength(260).IsRequired();
        builder.Property(p => p.ContentType).HasMaxLength(120).IsRequired();
        builder.Property(p => p.CapturedBy).HasMaxLength(160);
        builder.Property(p => p.Kind).HasConversion<int>();

        builder.HasIndex(p => p.Kind);

        builder.HasOne(p => p.Asset).WithMany()
            .HasForeignKey(p => p.AssetId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.AssetRequest).WithMany()
            .HasForeignKey(p => p.AssetRequestId).OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(p => p.AssetReturn).WithMany()
            .HasForeignKey(p => p.AssetReturnId).OnDelete(DeleteBehavior.NoAction);
    }
}
