using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Domain.Common;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KIPL.AssetManagement.Infrastructure.Persistence;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>, IApplicationDbContext
{
    private readonly ICurrentUserService? _currentUser;

    /// <summary>
    /// The current-user service is optional so the design-time factory (and tests)
    /// can build a context without the web request pipeline.
    /// </summary>
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService? currentUser = null)
        : base(options) => _currentUser = currentUser;

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
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Keep the Identity tables readable rather than AspNetXxx everywhere.
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var actor = _currentUser?.UserName ?? "system";
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedUtc = now;
                    entry.Entity.CreatedBy = actor;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedUtc = now;
                    entry.Entity.ModifiedBy = actor;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    Task<int> IApplicationDbContext.SaveChangesAsync(CancellationToken cancellationToken)
        => SaveChangesAsync(cancellationToken);
}
