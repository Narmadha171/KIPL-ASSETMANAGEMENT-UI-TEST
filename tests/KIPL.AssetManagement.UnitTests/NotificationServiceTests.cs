using FluentAssertions;
using KIPL.AssetManagement.Application.Notifications;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Domain.Enums;
using Xunit;

namespace KIPL.AssetManagement.UnitTests;

public class NotificationServiceTests : IDisposable
{
    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _service = new NotificationService(_db);

        _db.Employees.AddRange(
            new Employee { Id = 1, FullName = "Priya Shankar", Email = "p@kipl.com", RoleName = Roles.Employee, ManagerId = 2 },
            new Employee { Id = 2, FullName = "Michael Chen", Email = "m@kipl.com", RoleName = Roles.ReportingManager },
            new Employee { Id = 3, FullName = "Amanda Lee", Email = "a@kipl.com", RoleName = Roles.HR },
            new Employee { Id = 4, FullName = "Vikram Singh", Email = "v@kipl.com", RoleName = Roles.ITAssetManager }
        );

        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetNotifications_ReturnsDispatchedRequests_ForEmployee()
    {
        _db.AssetRequests.Add(new AssetRequest
        {
            Id = 10,
            RequesterId = 1,
            ItemName = "MacBook Pro",
            Category = AssetCategory.Laptop,
            Status = RequestStatus.Dispatched,
            Mode = FulfilmentMode.Office,
            OfficePickupLocation = "Desk 4A"
        });
        _db.SaveChanges();

        var currentUser = new FakeCurrentUserService { EmployeeId = 1 };
        var notifs = await _service.GetNotificationsAsync(currentUser);

        notifs.Should().Contain(n => n.Title == "Asset Ready For Receipt" && n.TargetUrl == "/Me/Requests");
    }

    [Fact]
    public async Task GetNotifications_ReturnsPendingApprovals_ForManager()
    {
        _db.AssetRequests.Add(new AssetRequest
        {
            Id = 11,
            RequesterId = 1,
            ItemName = "Dell 27 Inch Monitor",
            Category = AssetCategory.Monitor,
            Status = RequestStatus.Pending
        });
        _db.SaveChanges();

        var currentUser = new FakeCurrentUserService { EmployeeId = 2 };
        var notifs = await _service.GetNotificationsAsync(currentUser);

        notifs.Should().Contain(n => n.Title == "Team Request Awaiting Approval" && n.TargetUrl == "/Team/Approvals");
    }

    [Fact]
    public async Task GetNotifications_ReturnsUnclaimedRequests_ForOperations()
    {
        _db.AssetRequests.Add(new AssetRequest
        {
            Id = 12,
            RequesterId = 1,
            ItemName = "Mechanical Keyboard",
            Category = AssetCategory.Keyboard,
            Status = RequestStatus.Unclaimed
        });
        _db.SaveChanges();

        var currentUser = new FakeCurrentUserService { EmployeeId = 4 };
        var notifs = await _service.GetNotificationsAsync(currentUser);

        notifs.Should().Contain(n => n.Title == "Unclaimed Request In Queue" && n.TargetUrl == "/Ops/ExecApprovals");
    }
}
