using KIPL.AssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KIPL.AssetManagement.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FullName).HasMaxLength(160).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(200).IsRequired();
        builder.HasIndex(e => e.Email).IsUnique();

        builder.Property(e => e.RoleName).HasMaxLength(60).IsRequired();
        builder.Property(e => e.IdentityUserId).HasMaxLength(450);
        builder.Property(e => e.Status).HasConversion<int>();
        builder.Property(e => e.CreatedBy).HasMaxLength(160);
        builder.Property(e => e.ModifiedBy).HasMaxLength(160);

        builder.HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Manager)
            .WithMany(e => e.DirectReports)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Ignore(e => e.Initials);
    }
}
