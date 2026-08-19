using System.Security.Claims;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Domain.Enums;
using KIPL.AssetManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KIPL.AssetManagement.Infrastructure.Persistence.Seed;

/// <summary>
/// Applies migrations and populates the database with the reference data from the
/// UI prototype so the application is immediately explorable. Idempotent — each
/// section is skipped when its table already has rows.
/// </summary>
public class ApplicationDbSeeder
{
    private const string DefaultPassword = "Kipl@12345";

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<ApplicationRole> _roles;
    private readonly ILogger<ApplicationDbSeeder> _logger;

    public ApplicationDbSeeder(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles,
        ILogger<ApplicationDbSeeder> logger)
    {
        _db = db;
        _users = users;
        _roles = roles;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await _db.Database.MigrateAsync(ct);

        await SeedRolesAndPermissionsAsync();
        await SeedDepartmentsAsync(ct);
        await SeedPeopleAsync(ct);
        await SeedAssetsAsync(ct);
        await SeedRequestsAsync(ct);
        await SeedReturnsAsync(ct);
        await SeedServiceRequestsAsync(ct);
        await SeedAuditAsync(ct);

        _logger.LogInformation("Database seeding complete.");
    }

    // ------------------------------------------------------------------ roles
    private async Task SeedRolesAndPermissionsAsync()
    {
        foreach (var roleName in Roles.All)
        {
            if (!await _roles.RoleExistsAsync(roleName))
                await _roles.CreateAsync(new ApplicationRole(roleName));
        }

        if (!await _db.RolePermissions.AnyAsync())
        {
            foreach (var (roleName, permissions) in Permissions.ForRole)
            {
                foreach (var permission in Permissions.All)
                {
                    _db.RolePermissions.Add(new RolePermission
                    {
                        RoleName = roleName,
                        Permission = permission,
                        IsGranted = permissions.Contains(permission)
                    });
                }
            }

            await _db.SaveChangesAsync();
        }

        // Mirror the table into Identity role claims, which the policies read.
        var grants = await _db.RolePermissions.AsNoTracking().Where(r => r.IsGranted).ToListAsync();
        foreach (var roleName in Roles.All)
        {
            var role = await _roles.FindByNameAsync(roleName);
            if (role is null) continue;

            var existing = (await _roles.GetClaimsAsync(role))
                .Where(c => c.Type == Permissions.ClaimType)
                .Select(c => c.Value)
                .ToHashSet();

            foreach (var grant in grants.Where(g => g.RoleName == roleName))
            {
                if (!existing.Contains(grant.Permission))
                    await _roles.AddClaimAsync(role, new Claim(Permissions.ClaimType, grant.Permission));
            }
        }
    }

    // ------------------------------------------------------------ departments
    private async Task SeedDepartmentsAsync(CancellationToken ct)
    {
        if (await _db.Departments.AnyAsync(ct)) return;

        var names = new[]
        {
            "Operations", "Engineering", "Design", "Human Resources",
            "Sales", "Product", "IT Operations", "Finance"
        };

        _db.Departments.AddRange(names.Select(n => new Department { Name = n }));
        await _db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------- people
    private async Task SeedPeopleAsync(CancellationToken ct)
    {
        if (await _db.Employees.AnyAsync(ct)) return;

        var departments = await _db.Departments.ToDictionaryAsync(d => d.Name, d => d.Id, ct);

        // (FullName, Email, Role, Department, ManagerEmail)
        var people = new (string Name, string Email, string Role, string Dept, string? Manager)[]
        {
            ("Vikram Singh",  "vikram.singh@kipl.com",  Roles.ITAssetManager,   "IT Operations",    null),
            ("Revanth K",     "revanth.k@kipl.com",     Roles.ITAssetExecutive, "IT Operations",    "vikram.singh@kipl.com"),
            ("Amanda Lee",    "amanda.lee@kipl.com",    Roles.HR,               "Human Resources",  "vikram.singh@kipl.com"),
            ("Michael Chen",  "michael.chen@kipl.com",  Roles.ReportingManager, "Design",           "vikram.singh@kipl.com"),
            ("Priya Shankar", "priya.shankar@kipl.com", Roles.Employee,         "Engineering",      "michael.chen@kipl.com"),
            ("Sarah Jenkins", "sarah.jenkins@kipl.com", Roles.Employee,         "Engineering",      "michael.chen@kipl.com"),
            ("Tom Reddy",     "tom.reddy@kipl.com",     Roles.Employee,         "Design",           "michael.chen@kipl.com"),
            ("John Doe",      "john.doe@kipl.com",      Roles.Employee,         "Sales",            "michael.chen@kipl.com"),
            ("Ankit Kumar",   "ankit.kumar@kipl.com",   Roles.Employee,         "Product",          "michael.chen@kipl.com"),
            ("System Admin",  "admin@kipl.com",         Roles.Admin,            "Operations",       null),
        };

        // Pass 1 — create Identity logins and Employee rows.
        foreach (var p in people)
        {
            var user = new ApplicationUser
            {
                UserName = p.Email,
                Email = p.Email,
                FullName = p.Name,
                EmailConfirmed = true
            };

            var created = await _users.CreateAsync(user, DefaultPassword);
            if (!created.Succeeded)
            {
                _logger.LogWarning("Could not create login {Email}: {Errors}", p.Email,
                    string.Join("; ", created.Errors.Select(e => e.Description)));
                continue;
            }

            await _users.AddToRoleAsync(user, p.Role);

            _db.Employees.Add(new Employee
            {
                FullName = p.Name,
                Email = p.Email,
                RoleName = p.Role,
                DepartmentId = departments.TryGetValue(p.Dept, out var did) ? did : null,
                Status = p.Name == "Tom Reddy" ? UserStatus.Inactive : UserStatus.Active,
                IdentityUserId = user.Id,
                LastLoginUtc = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(5, 4000))
            });
        }

        await _db.SaveChangesAsync(ct);

        // Pass 2 — wire up the reporting lines now that every row has an Id.
        var employees = await _db.Employees.ToDictionaryAsync(e => e.Email, ct);
        foreach (var p in people.Where(x => x.Manager is not null))
        {
            if (employees.TryGetValue(p.Email, out var employee) &&
                employees.TryGetValue(p.Manager!, out var manager))
            {
                employee.ManagerId = manager.Id;
            }
        }

        // Pass 3 — link the Identity accounts back to their Employee rows.
        foreach (var employee in employees.Values)
        {
            if (employee.IdentityUserId is null) continue;
            var user = await _users.FindByIdAsync(employee.IdentityUserId);
            if (user is null) continue;
            user.EmployeeId = employee.Id;
            await _users.UpdateAsync(user);
        }

        await _db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------- assets
    private async Task SeedAssetsAsync(CancellationToken ct)
    {
        if (await _db.Assets.AnyAsync(ct)) return;

        var people = await _db.Employees.ToDictionaryAsync(e => e.FullName, e => e.Id, ct);
        int? Who(string? name) => name is not null && people.TryGetValue(name, out var id) ? id : null;

        // (Tag, Category, Brand, Condition, Status, AssignedTo, StatusNote)
        var rows = new (string Tag, AssetCategory Cat, string Brand, AssetCondition Cond, AssetStatus Status, string? Holder, string? Note)[]
        {
            ("LAP-7F3K9Q", AssetCategory.Laptop,         "Dell Latitude 5440",       AssetCondition.New,         AssetStatus.Assigned,     "Sarah Jenkins", null),
            ("MOU-2K19XZ", AssetCategory.Mouse,          "Logitech M331",            AssetCondition.Refurbished, AssetStatus.InTransit,    null, null),
            ("MON-8H4D2E", AssetCategory.Monitor,        "LG 27UL850",               AssetCondition.New,         AssetStatus.InStock,      null, null),
            ("KEY-3D6M8P", AssetCategory.Keyboard,       "Keychron K2 V2",           AssetCondition.Refurbished, AssetStatus.InStock,      null, null),
            ("LAP-9Q7T3B", AssetCategory.Laptop,         "HP EliteBook 840",         AssetCondition.Refurbished, AssetStatus.UnderService, null, null),
            ("HEA-5D8P2K", AssetCategory.Headset,        "Jabra Evolve 65",          AssetCondition.New,         AssetStatus.Assigned,     "Priya Shankar", null),
            ("DOC-3H9F5W", AssetCategory.DockingStation, "Belkin Thunderbolt 4",     AssetCondition.New,         AssetStatus.Assigned,     "Vikram Singh", null),
            ("SSD-6T4B2K", AssetCategory.Storage,        "Samsung T7 1TB",           AssetCondition.New,         AssetStatus.Assigned,     "Revanth K", null),
            ("KEY-9P2M4L", AssetCategory.Keyboard,       "Keychron K2",              AssetCondition.New,         AssetStatus.Assigned,     "Priya Shankar", null),
            ("TAB-1M6P3X", AssetCategory.Peripheral,     "Wacom Intuos Pro",         AssetCondition.New,         AssetStatus.Assigned,     "Tom Reddy", null),
            ("NET-2B7Q9C", AssetCategory.Networking,     "Netgear Nighthawk",        AssetCondition.Retired,     AssetStatus.Retired,      null, "Decommissioned"),
            ("LAP-4X8N2Q", AssetCategory.Laptop,         "MacBook Air M1",           AssetCondition.Good,        AssetStatus.Lost,         null, "Last seen: John Doe"),
            ("MOU-7712XX", AssetCategory.Mouse,          "Logi MX Master 3",         AssetCondition.Good,        AssetStatus.Lost,         null, "Last seen: Ankit Kumar"),
            ("LAP-RK4402", AssetCategory.Laptop,         "ThinkPad X1 Carbon",       AssetCondition.New,         AssetStatus.Assigned,     "Revanth K", null),
            ("KEY-RK9910", AssetCategory.Keyboard,       "Logitech MX Keys",         AssetCondition.Good,        AssetStatus.Assigned,     "Revanth K", null),
            ("LAP-VS1102", AssetCategory.Laptop,         "MacBook Pro 14\"",         AssetCondition.New,         AssetStatus.Assigned,     "Vikram Singh", null),
            ("LAP-MC7701", AssetCategory.Laptop,         "MacBook Pro 14\"",         AssetCondition.New,         AssetStatus.Assigned,     "Michael Chen", null),
            ("MOU-MC3301", AssetCategory.Mouse,          "MX Master 3S",             AssetCondition.Good,        AssetStatus.Assigned,     "Michael Chen", null),
            ("LAP-AL2201", AssetCategory.Laptop,         "MacBook Air 13\"",         AssetCondition.Good,        AssetStatus.Assigned,     "Amanda Lee", null),
            ("LMP-2C7Y1A", AssetCategory.Other,          "Desk Lamp",                AssetCondition.Good,        AssetStatus.Assigned,     "Amanda Lee", null),
            ("MOU-4X8K1P", AssetCategory.Mouse,          "MX Master 3S",             AssetCondition.New,         AssetStatus.InTransit,    null, null),
            ("MON-011X",   AssetCategory.Monitor,        "Dell U2723QE",             AssetCondition.New,         AssetStatus.InTransit,    null, null),
            ("HUB-8K2L9Q", AssetCategory.Peripheral,     "Anker USB-C Hub",          AssetCondition.Good,        AssetStatus.Assigned,     "Sarah Jenkins", null),
            ("LAP-OLD1",   AssetCategory.Laptop,         "MacBook Air M1 (2020)",    AssetCondition.Good,        AssetStatus.Assigned,     "Sarah Jenkins", null),
            ("LAP-5590Q",  AssetCategory.Laptop,         "Dell Latitude 5430",       AssetCondition.Good,        AssetStatus.Assigned,     "Tom Reddy", null),
            ("MON-4K7788", AssetCategory.Monitor,        "Dell U2723QE",             AssetCondition.New,         AssetStatus.InStock,      null, null),
            ("HEA-9911QQ", AssetCategory.Headset,        "Sony WH-1000XM5",          AssetCondition.New,         AssetStatus.InStock,      null, null),
            ("DOC-5521AB", AssetCategory.DockingStation, "CalDigit TS4",             AssetCondition.New,         AssetStatus.InStock,      null, null),
            ("CHR-3300ZZ", AssetCategory.Furniture,      "Herman Miller Aeron",      AssetCondition.Good,        AssetStatus.InStock,      null, null),
            ("LAP-2C8N4R", AssetCategory.Laptop,         "Dell Latitude 5420",       AssetCondition.Retired,     AssetStatus.Retired,      null, "End of life"),
        };

        var now = DateTime.UtcNow;
        foreach (var r in rows)
        {
            var holderId = Who(r.Holder);
            _db.Assets.Add(new Asset
            {
                Tag = r.Tag,
                Category = r.Cat,
                Brand = r.Brand,
                Condition = r.Cond,
                Status = r.Status,
                AssignedToEmployeeId = holderId,
                StatusNote = r.Note,
                PurchaseDate = now.AddDays(-Random.Shared.Next(60, 900)),
                PurchaseCost = Math.Round((decimal)Random.Shared.Next(40, 2200), 2),
                WarrantyExpiry = now.AddDays(Random.Shared.Next(30, 900)),
                Location = "Head office",
                CreatedUtc = now
            });
        }

        await _db.SaveChangesAsync(ct);

        // Give every assigned asset an open custody record.
        var assigned = await _db.Assets.Where(a => a.AssignedToEmployeeId != null).ToListAsync(ct);
        foreach (var asset in assigned)
        {
            _db.AssetAssignments.Add(new AssetAssignment
            {
                AssetId = asset.Id,
                EmployeeId = asset.AssignedToEmployeeId!.Value,
                AssignedUtc = now.AddDays(-Random.Shared.Next(10, 400)),
                AssignedBy = "Seed"
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    // -------------------------------------------------------------- requests
    private async Task SeedRequestsAsync(CancellationToken ct)
    {
        if (await _db.AssetRequests.AnyAsync(ct)) return;

        var people = await _db.Employees.ToDictionaryAsync(e => e.FullName, e => e.Id, ct);
        var assets = await _db.Assets.ToDictionaryAsync(a => a.Tag, a => a.Id, ct);
        var now = DateTime.UtcNow;

        int? Who(string name) => people.TryGetValue(name, out var id) ? id : null;
        int? Tagged(string? tag) => tag is not null && assets.TryGetValue(tag, out var id) ? id : null;

        var rows = new (string Emp, string Item, AssetCategory Cat, string Reason, RequestStatus Status, FulfilmentMode? Mode, int DaysAgo, string? Tag, string? OrderId)[]
        {
            ("Sarah Jenkins", "MacBook Pro 16\"",          AssetCategory.Laptop,   "Current laptop is 4 years old and struggling with build times.", RequestStatus.Pending,       null,                   6,  null, null),
            ("Priya Shankar", "Monitor 27\" 4K",           AssetCategory.Monitor,  "Second monitor for reviewing side-by-side layouts.",             RequestStatus.Dispatched,    FulfilmentMode.Courier, 9,  "MON-011X", "ORD-992384"),
            ("Tom Reddy",     "Ergonomic Chair",           AssetCategory.Furniture,"Doctor recommended ergonomic support.",                          RequestStatus.Unclaimed,     null,                   7,  null, null),
            ("Priya Shankar", "MX Master 3S Mouse",        AssetCategory.Mouse,    "Current mouse stopped tracking reliably.",                       RequestStatus.Dispatched,    FulfilmentMode.Courier, 8,  "MOU-4X8K1P", null),
            ("Priya Shankar", "Keychron K2 Keyboard",      AssetCategory.Keyboard, "Prefer a mechanical keyboard for daily typing.",                 RequestStatus.Completed,     FulfilmentMode.Office,  22, "KEY-9P2M4L", null),
            ("Priya Shankar", "Webcam",                    AssetCategory.Peripheral,"Need a better camera for client calls.",                        RequestStatus.Pending,       null,                   4,  null, null),
            ("Amanda Lee",    "Noise-cancelling Headset",  AssetCategory.Headset,  "Open-plan office is noisy during calls.",                        RequestStatus.Pending,       null,                   5,  null, null),
            ("Amanda Lee",    "Desk Lamp",                 AssetCategory.Other,    "Better lighting for video calls.",                               RequestStatus.Completed,     FulfilmentMode.Office,  27, "LMP-2C7Y1A", null),
            ("Revanth K",     "External SSD 1TB",          AssetCategory.Storage,  "Need portable storage for asset image backups.",                 RequestStatus.Dispatched,    FulfilmentMode.Courier, 10, "SSD-6T4B2K", null),
            ("Vikram Singh",  "Docking Station",           AssetCategory.DockingStation, "Switching between laptop and desk setup several times a day.", RequestStatus.Completed, FulfilmentMode.Office, 45, "DOC-3H9F5W", null),
            ("Michael Chen",  "Webcam",                    AssetCategory.Peripheral,"Client calls need better video quality.",                       RequestStatus.Pending,       null,                   3,  null, null),
            ("Sarah Jenkins", "USB-C Hub",                 AssetCategory.Peripheral,"Only one port on the current laptop.",                          RequestStatus.Completed,     FulfilmentMode.Office,  55, "HUB-8K2L9Q", null),
            ("Tom Reddy",     "Drawing Tablet",            AssetCategory.Peripheral,"Client mockups need pen input.",                                RequestStatus.Completed,     FulfilmentMode.Courier, 68, "TAB-1M6P3X", null),
            ("John Doe",      "Keyboard",                  AssetCategory.Keyboard, "My current laptop keyboard is failing and I need a replacement for client meetings.", RequestStatus.Pending, null, 2, null, null),
            ("Ankit Kumar",   "Developer Workstation",     AssetCategory.Laptop,   "New joiner onboarding — needs the standard developer workstation.", RequestStatus.Pending,     null,                   1,  null, null),
            ("Sarah Jenkins", "Dell U2723QE Monitor",      AssetCategory.Monitor,  "Approved for a dual-monitor setup, ordering from the vendor.",    RequestStatus.OnlineOrdered, null,                   3,  null, "ORD-104477"),
        };

        var approver = Who("Michael Chen");

        foreach (var r in rows)
        {
            var requesterId = Who(r.Emp);
            if (requesterId is null) continue;

            var raised = now.AddDays(-r.DaysAgo);
            var request = new AssetRequest
            {
                RequesterId = requesterId.Value,
                ItemName = r.Item,
                Category = r.Cat,
                Reason = r.Reason,
                Status = r.Status,
                Mode = r.Mode,
                RaisedUtc = raised,
                CreatedUtc = raised,
                FulfilledWithAssetId = Tagged(r.Tag),
                OrderId = r.OrderId,
                Source = r.OrderId is not null ? RequestSource.Online : RequestSource.Stock
            };

            if (r.Status is not (RequestStatus.Pending or RequestStatus.Cancelled))
            {
                request.ApprovedById = approver;
                request.ApprovedUtc = raised.AddHours(6);
            }

            if (r.Status == RequestStatus.Dispatched)
            {
                request.DispatchedUtc = raised.AddDays(1);
                if (r.Mode == FulfilmentMode.Courier)
                    request.CourierTrackingNumber = $"1Z999AA{Random.Shared.Next(10000000, 99999999)}";
                else
                    request.OfficePickupLocation = "IT desk, 2nd floor";
            }

            if (r.Status == RequestStatus.Completed)
            {
                request.DispatchedUtc = raised.AddDays(1);
                request.CompletedUtc = raised.AddDays(2);
                request.OfficePickupLocation = r.Mode == FulfilmentMode.Office ? "IT desk, 2nd floor" : null;
            }

            if (r.Status == RequestStatus.OnlineOrdered)
            {
                request.OrderVendor = "Dell Direct";
                request.OrderBrand = "Dell";
                request.OrderModel = "U2723QE";
            }

            _db.AssetRequests.Add(request);
        }

        await _db.SaveChangesAsync(ct);
    }

    // --------------------------------------------------------------- returns
    private async Task SeedReturnsAsync(CancellationToken ct)
    {
        if (await _db.AssetReturns.AnyAsync(ct)) return;

        var people = await _db.Employees.ToDictionaryAsync(e => e.FullName, e => e.Id, ct);
        var assets = await _db.Assets.ToDictionaryAsync(a => a.Tag, a => a.Id, ct);
        var now = DateTime.UtcNow;

        var rows = new (string Reference, string Emp, string Tag, FulfilmentMode Mode, ReturnStage Stage, string? Tracking, string? DropOff, int DaysAgo)[]
        {
            ("RET-2045", "John Doe",      "LAP-4X8N2Q", FulfilmentMode.Courier, ReturnStage.InTransit,             "1Z999AA10123456784", null,                  4),
            ("RET-2048", "Sarah Jenkins", "LAP-OLD1",   FulfilmentMode.Office,  ReturnStage.ReceivedForInspection, null,                 "IT desk, 2nd floor",  3),
            ("RET-2049", "Ankit Kumar",   "MOU-7712XX", FulfilmentMode.Courier, ReturnStage.AwaitingPickup,        null,                 null,                  2),
            ("RET-2051", "Tom Reddy",     "TAB-1M6P3X", FulfilmentMode.Office,  ReturnStage.AwaitingPickup,        null,                 "IT desk, 2nd floor",  1),
            ("RET-2052", "Amanda Lee",    "LMP-2C7Y1A", FulfilmentMode.Courier, ReturnStage.InTransit,             "1Z999AA10198765432", null,                  2),
        };

        foreach (var r in rows)
        {
            if (!people.TryGetValue(r.Emp, out var employeeId)) continue;
            if (!assets.TryGetValue(r.Tag, out var assetId)) continue;

            _db.AssetReturns.Add(new AssetReturn
            {
                Reference = r.Reference,
                EmployeeId = employeeId,
                AssetId = assetId,
                Mode = r.Mode,
                Stage = r.Stage,
                CourierTrackingNumber = r.Tracking,
                DropOffLocation = r.DropOff,
                RaisedUtc = now.AddDays(-r.DaysAgo),
                CreatedUtc = now.AddDays(-r.DaysAgo),
                HandoverUtc = r.Stage != ReturnStage.AwaitingPickup ? now.AddDays(-r.DaysAgo).AddHours(5) : null,
                ExpectedUtc = now.AddDays(2)
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------- service requests
    private async Task SeedServiceRequestsAsync(CancellationToken ct)
    {
        if (await _db.ServiceRequests.AnyAsync(ct)) return;

        var people = await _db.Employees.ToDictionaryAsync(e => e.FullName, e => e.Id, ct);
        var assets = await _db.Assets.ToDictionaryAsync(a => a.Tag, a => a.Id, ct);
        var now = DateTime.UtcNow;

        var rows = new (string Reference, string Tag, string Reporter, string Type, string Desc, ServiceUrgency Urgency, int DaysAgo)[]
        {
            ("SR-1041", "LAP-7F3K9Q", "Sarah Jenkins", "Battery", "Drains from 100% to 20% within two hours.",  ServiceUrgency.AffectsMyWork, 3),
            ("SR-1042", "MON-8H4D2E", "Tom Reddy",     "Screen",  "Flickering on the left third of the panel.", ServiceUrgency.CannotWork,    1),
        };

        foreach (var r in rows)
        {
            if (!people.TryGetValue(r.Reporter, out var reporterId)) continue;
            if (!assets.TryGetValue(r.Tag, out var assetId)) continue;

            _db.ServiceRequests.Add(new ServiceRequest
            {
                Reference = r.Reference,
                AssetId = assetId,
                ReportedById = reporterId,
                IssueType = r.Type,
                Description = r.Desc,
                Urgency = r.Urgency,
                Status = ServiceRequestStatus.Open,
                RaisedUtc = now.AddDays(-r.DaysAgo),
                CreatedUtc = now.AddDays(-r.DaysAgo)
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    // ----------------------------------------------------------------- audit
    private async Task SeedAuditAsync(CancellationToken ct)
    {
        if (await _db.AuditEntries.AnyAsync(ct)) return;

        var now = DateTime.UtcNow;
        var rows = new (int MinutesAgo, string User, string Role, string Action, string Entity)[]
        {
            (35,   "Vikram Singh", Roles.ITAssetManager,   "Retired asset",     "LAP-2C8N4R"),
            (120,  "Amanda Lee",   Roles.HR,               "Logged order",      "ORD-992384"),
            (1150, "Revanth K",    Roles.ITAssetExecutive, "Dispatched asset",  "MOU-4X8K1P"),
            (1300, "Revanth K",    Roles.ITAssetExecutive, "Asset edited",      "MOU-2K19XZ"),
            (1500, "Michael Chen", Roles.ReportingManager, "Approved request",  "Ergonomic Chair"),
            (2900, "Vikram Singh", Roles.Admin,            "Role changed",      "Priya Shankar"),
            (3400, "Revanth K",    Roles.ITAssetExecutive, "Assigned asset",    "HEA-5D8P2K"),
            (4200, "Amanda Lee",   Roles.HR,               "Approved request",  "Desk Lamp"),
        };

        foreach (var r in rows)
        {
            _db.AuditEntries.Add(new AuditEntry
            {
                TimestampUtc = now.AddMinutes(-r.MinutesAgo),
                UserName = r.User,
                RoleName = r.Role,
                Action = r.Action,
                Entity = r.Entity
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}
