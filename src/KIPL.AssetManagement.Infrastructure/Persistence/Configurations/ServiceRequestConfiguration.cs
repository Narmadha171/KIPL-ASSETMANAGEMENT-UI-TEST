using KIPL.AssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KIPL.AssetManagement.Infrastructure.Persistence.Configurations;

public class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("ServiceRequests");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Reference).HasMaxLength(40).IsRequired();
        builder.HasIndex(s => s.Reference).IsUnique();

        builder.Property(s => s.IssueType).HasMaxLength(80).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(2000).IsRequired();
        builder.Property(s => s.Resolution).HasMaxLength(2000);
        builder.Property(s => s.CreatedBy).HasMaxLength(160);
        builder.Property(s => s.ModifiedBy).HasMaxLength(160);

        builder.Property(s => s.Urgency).HasConversion<int>();
        builder.Property(s => s.Status).HasConversion<int>();

        builder.HasOne(s => s.Asset).WithMany()
            .HasForeignKey(s => s.AssetId).OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(s => s.ReportedBy).WithMany()
            .HasForeignKey(s => s.ReportedById).OnDelete(DeleteBehavior.NoAction);
    }
}
