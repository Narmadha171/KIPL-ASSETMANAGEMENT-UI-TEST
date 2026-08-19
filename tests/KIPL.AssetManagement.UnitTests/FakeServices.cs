using KIPL.AssetManagement.Application.Common;
using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Domain.Enums;

namespace KIPL.AssetManagement.UnitTests;

public class FakeAuditService : IAuditService
{
    private readonly TestDbContext _db;

    public FakeAuditService(TestDbContext db) => _db = db;

    public List<(string Action, string Entity)> Entries { get; } = new();

    public Task LogAsync(string action, string entity, string? detail = null, CancellationToken ct = default)
    {
        Entries.Add((action, entity));
        _db.AuditEntries.Add(new AuditEntry
        {
            Action = action,
            Entity = entity,
            Detail = detail,
            UserName = "test",
            RoleName = Roles.Admin
        });
        return Task.CompletedTask;
    }
}

public class FakeTagGenerator : ITagGenerator
{
    private int _counter;

    public string NewAssetTag(AssetCategory category) => $"TST-{++_counter:D4}";
    public string NewReturnReference() => $"RET-{++_counter:D4}";
    public string NewServiceReference() => $"SR-{++_counter:D4}";
}

public class FakeIdentityService : IIdentityService
{
    public Task<Result<string>> CreateUserAsync(string email, string fullName, string roleName, string password)
        => Task.FromResult(Result<string>.Success(Guid.NewGuid().ToString()));

    public Task SyncUserAsync(string identityUserId, string email, string fullName, string roleName, bool enabled)
        => Task.CompletedTask;

    public Task SetLockoutAsync(string identityUserId, bool locked) => Task.CompletedTask;

    public Task SetRolePermissionAsync(string roleName, string permission, bool granted) => Task.CompletedTask;

    public Task<IReadOnlyList<string>> GetAllRoleNamesAsync()
        => Task.FromResult<IReadOnlyList<string>>(Domain.Enums.Roles.All);

    public Task<Result> CreateRoleAsync(string roleName) => Task.FromResult(Result.Success());
}

public class FakeFileStorage : IFileStorage
{
    public List<string> Saved { get; } = new();

    public Task<StoredFile> SaveAsync(
        Stream content, string originalFileName, string contentType, string folder, CancellationToken ct = default)
    {
        var relative = $"{folder}/{Guid.NewGuid():N}{Path.GetExtension(originalFileName)}";
        Saved.Add(relative);
        return Task.FromResult(new StoredFile(relative, originalFileName, contentType, content.Length));
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        Saved.Remove(relativePath);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string relativePath) => "/uploads/" + relativePath;
}

public class FakeCurrentUserService : ICurrentUserService
{
    public string? IdentityUserId => "test-user";
    public string? UserName => "Test User";
    public string? Email => "test@kipl.com";
    public string RoleName => Roles.Admin;
    public int? EmployeeId { get; set; }
    public bool IsAuthenticated => true;
    public bool HasPermission(string permission) => true;
}
