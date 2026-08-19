using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KIPL.AssetManagement.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add ...` build a context without starting the web host.
/// Override the connection string with the KIPL_CONNECTION environment variable.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("KIPL_CONNECTION")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=KIPL_AssetManagement;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connection, sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .Options;

        return new ApplicationDbContext(options);
    }
}
