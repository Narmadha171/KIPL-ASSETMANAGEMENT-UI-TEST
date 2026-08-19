using KIPL.AssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KIPL.AssetManagement.Application.Common.Interfaces;

/// <summary>
/// The persistence contract the Application layer codes against. Infrastructure
/// supplies the EF Core implementation, keeping Application free of a provider
/// dependency and trivially fakeable in tests.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Department> Departments { get; }
    DbSet<Employee> Employees { get; }
    DbSet<Asset> Assets { get; }
    DbSet<AssetAssignment> AssetAssignments { get; }
    DbSet<AssetRequest> AssetRequests { get; }
    DbSet<AssetReturn> AssetReturns { get; }
    DbSet<ServiceRequest> ServiceRequests { get; }
    DbSet<AssetPhoto> AssetPhotos { get; }
    DbSet<AuditEntry> AuditEntries { get; }
    DbSet<RolePermission> RolePermissions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
