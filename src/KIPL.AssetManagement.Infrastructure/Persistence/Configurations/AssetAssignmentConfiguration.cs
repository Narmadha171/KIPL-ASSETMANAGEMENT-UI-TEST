using KIPL.AssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KIPL.AssetManagement.Infrastructure.Persistence.Configurations;

public class AssetAssignmentConfiguration : IEntityTypeConfiguration<AssetAssignment>
{
    public void Configure(EntityTypeBuilder<AssetAssignment> builder)
    {
        builder.ToTable("AssetAssignments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AssignedBy).HasMaxLength(160);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasOne(x => x.Asset).WithMany(a => a.Assignments)
            .HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Employee).WithMany()
            .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.AssetId, x.ReturnedUtc });
        builder.Ignore(x => x.IsActive);
    }
}
