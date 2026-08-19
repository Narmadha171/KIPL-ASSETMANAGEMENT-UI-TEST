using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Infrastructure.Identity;
using KIPL.AssetManagement.Infrastructure.Persistence;
using KIPL.AssetManagement.Infrastructure.Persistence.Seed;
using KIPL.AssetManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KIPL.AssetManagement.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers persistence and the infrastructure implementations of the
    /// Application contracts.
    /// </summary>
    /// <remarks>
    /// Identity itself is wired up in the Web project rather than here. The
    /// <c>AddIdentity</c> extension lives in the ASP.NET Core shared framework,
    /// and pulling that into this class library would make Infrastructure depend
    /// on the web stack. The stores and managers used below come from
    /// Microsoft.Extensions.Identity.Core, which is framework-agnostic.
    /// </remarks>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connection, sql =>
            {
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
            }));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ITagGenerator, TagGenerator>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<IDateTime, DateTimeService>();
        services.AddScoped<ApplicationDbSeeder>();

        return services;
    }
}
