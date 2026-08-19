using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KIPL.AssetManagement.UnitTests;

/// <summary>
/// An in-memory IApplicationDbContext for exercising the Application services
/// without SQL Server. Because Application depends on the interface rather than
/// the EF context, no Identity or provider plumbing is needed here.
/// </summary>
public class TestDbContext : DbContext, IApplicationDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetAssignment> AssetAssignments => Set<AssetAssignment>();
    public DbSet<AssetRequest> AssetRequests => Set<AssetRequest>();
    public DbSet<AssetReturn> AssetReturns => Set<AssetReturn>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<AssetPhoto> AssetPhotos => Set<AssetPhoto>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Computed helpers are not columns.
        builder.Entity<Asset>().Ignore(a => a.IsAssignable);
        builder.Entity<Employee>().Ignore(e => e.Initials);
        builder.Entity<AssetRequest>().Ignore(r => r.IsOpen);
        builder.Entity<AssetReturn>().Ignore(r => r.IsClosed);
        builder.Entity<AssetAssignment>().Ignore(a => a.IsActive);

        // Several entities point at Employee more than once, so the inverse
        // navigations must be stated explicitly rather than left to convention.
        builder.Entity<Employee>()
            .HasOne(e => e.Manager).WithMany(e => e.DirectReports).HasForeignKey(e => e.ManagerId);

        builder.Entity<Employee>()
            .HasOne(e => e.Department).WithMany(d => d.Employees).HasForeignKey(e => e.DepartmentId);

        builder.Entity<Asset>()
            .HasOne(a => a.AssignedToEmployee).WithMany(e => e.AssignedAssets)
            .HasForeignKey(a => a.AssignedToEmployeeId);

        builder.Entity<AssetAssignment>()
            .HasOne(x => x.Asset).WithMany(a => a.Assignments).HasForeignKey(x => x.AssetId);
        builder.Entity<AssetAssignment>()
            .HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId);

        builder.Entity<AssetRequest>()
            .HasOne(r => r.Requester).WithMany(e => e.Requests).HasForeignKey(r => r.RequesterId);
        builder.Entity<AssetRequest>()
            .HasOne(r => r.ApprovedBy).WithMany().HasForeignKey(r => r.ApprovedById);
        builder.Entity<AssetRequest>()
            .HasOne(r => r.ClaimedBy).WithMany().HasForeignKey(r => r.ClaimedById);
        builder.Entity<AssetRequest>()
            .HasOne(r => r.FulfilledWithAsset).WithMany().HasForeignKey(r => r.FulfilledWithAssetId);

        builder.Entity<AssetReturn>()
            .HasOne(r => r.Employee).WithMany().HasForeignKey(r => r.EmployeeId);
        builder.Entity<AssetReturn>()
            .HasOne(r => r.Asset).WithMany().HasForeignKey(r => r.AssetId);
        builder.Entity<AssetReturn>()
            .HasOne(r => r.InspectedBy).WithMany().HasForeignKey(r => r.InspectedById);

        builder.Entity<ServiceRequest>()
            .HasOne(s => s.Asset).WithMany().HasForeignKey(s => s.AssetId);
        builder.Entity<ServiceRequest>()
            .HasOne(s => s.ReportedBy).WithMany().HasForeignKey(s => s.ReportedById);

        builder.Entity<AssetPhoto>()
            .HasOne(p => p.Asset).WithMany().HasForeignKey(p => p.AssetId);
        builder.Entity<AssetPhoto>()
            .HasOne(p => p.AssetRequest).WithMany().HasForeignKey(p => p.AssetRequestId);
        builder.Entity<AssetPhoto>()
            .HasOne(p => p.AssetReturn).WithMany().HasForeignKey(p => p.AssetReturnId);

        base.OnModelCreating(builder);
    }

    public static TestDbContext Create()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new TestDbContext(options);
    }
}
