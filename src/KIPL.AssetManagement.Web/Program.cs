using KIPL.AssetManagement.Application.Common;
using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Domain.Enums;
using KIPL.AssetManagement.Infrastructure;
using KIPL.AssetManagement.Infrastructure.Identity;
using KIPL.AssetManagement.Infrastructure.Persistence;
using KIPL.AssetManagement.Infrastructure.Persistence.Seed;
using KIPL.AssetManagement.Web.Authorization;
using KIPL.AssetManagement.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Uploaded photos live under wwwroot so static file middleware can serve them.
builder.Configuration["Storage:UploadRoot"] ??=
    Path.Combine(builder.Environment.WebRootPath ?? "wwwroot", "uploads");

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// ---------------------------------------------------------------- identity
// Registered here rather than in Infrastructure: AddIdentity ships in the
// ASP.NET Core shared framework, so wiring it up in a class library would drag
// the web stack into the infrastructure layer.
builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireDigit = true;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Adds display name, Employee id and the role's permission claims to the cookie.
builder.Services
    .AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, AdditionalUserClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.Name = "kipl.assets";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// ----------------------------------------------------------- authorization
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OperationsShell", policy =>
        policy.RequireRole(Roles.Admin, Roles.ITAssetManager, Roles.ITAssetExecutive, Roles.HR));
});

builder.Services.AddRazorPages(options =>
{
    // Everything requires a login except the account pages.
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToFolder("/Account");

    options.Conventions.AuthorizeFolder("/Ops", "OperationsShell");
    options.Conventions.AuthorizePage("/Ops/Users", Permissions.ManageUsers);
    options.Conventions.AuthorizePage("/Ops/Roles", Permissions.ManageRoles);
    options.Conventions.AuthorizePage("/Ops/Audit", Permissions.ViewAudit);
    options.Conventions.AuthorizePage("/Ops/BulkImport", Permissions.BulkImport);
    options.Conventions.AuthorizePage("/Ops/ReturnTracking", Permissions.TrackReturns);
    options.Conventions.AuthorizePage("/Ops/ServiceQueue", Permissions.ManageInventory);
    options.Conventions.AuthorizePage("/Ops/Challan", Permissions.FulfilRequests);
    options.Conventions.AuthorizePage("/Ops/HrApprovals", Permissions.ApproveRequests);
    options.Conventions.AuthorizePage("/Team/Approvals", Permissions.ApproveTeamRequests);
    options.Conventions.AuthorizePage("/Team/Assets", Permissions.ViewTeam);
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

// Apply migrations and seed the reference data on startup.
if (app.Configuration.GetValue("SeedData:Enabled", true))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<ApplicationDbSeeder>();
    try
    {
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database migration/seed failed. Check the connection string in appsettings.json.");
    }
}

app.Run();

/// <summary>Exposed so an integration test host can reference the entry point.</summary>
public partial class Program { }
