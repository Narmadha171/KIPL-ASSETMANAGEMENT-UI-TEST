using FluentAssertions;
using KIPL.AssetManagement.Application.Common;
using KIPL.AssetManagement.Application.Requests;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Domain.Enums;
using Xunit;

namespace KIPL.AssetManagement.UnitTests;

public class AssetRequestServiceTests : IDisposable
{
    private const int EmployeeId = 1;
    private const int ManagerId = 2;
    private const int ExecutiveId = 3;

    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly AssetRequestService _service;
    private readonly FakeCurrentUserService _currentUser = new();

    public AssetRequestServiceTests()
    {
        var photos = new PhotoService(_db, new FakeFileStorage(), _currentUser);
        _service = new AssetRequestService(_db, new FakeAuditService(_db), photos, _currentUser);

        _db.Departments.Add(new Department { Id = 1, Name = "Engineering" });
        _db.Employees.AddRange(
            new Employee { Id = ManagerId, FullName = "Michael Chen", Email = "m@kipl.com", DepartmentId = 1, RoleName = Roles.ReportingManager },
            new Employee { Id = EmployeeId, FullName = "Priya Shankar", Email = "p@kipl.com", DepartmentId = 1, ManagerId = ManagerId },
            new Employee { Id = ExecutiveId, FullName = "Revanth K", Email = "r@kipl.com", DepartmentId = 1 });

        _db.Assets.Add(new Asset { Id = 10, Tag = "MON-0001", Brand = "LG", Category = AssetCategory.Monitor, Status = AssetStatus.InStock });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    private async Task<int> RaiseAsync()
    {
        var result = await _service.RaiseAsync(new RaiseRequestCommand
        {
            RequesterId = EmployeeId,
            ItemName = "Monitor 27\"",
            Category = AssetCategory.Monitor,
            Reason = "Second screen for reviews."
        });

        result.Succeeded.Should().BeTrue();
        return result.Value;
    }

    [Fact]
    public async Task RaiseAsync_Fails_WithoutReason()
    {
        var result = await _service.RaiseAsync(new RaiseRequestCommand { RequesterId = EmployeeId, ItemName = "Mouse" });

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task FullHappyPath_FromRaiseToReceipt()
    {
        var id = await RaiseAsync();

        (await _service.ApproveAsync(id, ManagerId)).Succeeded.Should().BeTrue();
        (await _service.ClaimAsync(id, ExecutiveId)).Succeeded.Should().BeTrue();

        var fulfil = await _service.FulfilAsync(new FulfilRequestCommand
        {
            RequestId = id,
            AssetIds = { 10 },
            Mode = FulfilmentMode.Courier,
            TrackingNumber = "1Z999"
        });
        fulfil.Succeeded.Should().BeTrue();
        fulfil.Value!.Items.Should().ContainSingle();
        fulfil.Value.ChallanNumber.Should().StartWith("KIPL/DC/");

        // The asset should be locked out of the assignable pool while it travels.
        _db.Assets.Single(a => a.Id == 10).Status.Should().Be(AssetStatus.InTransit);

        (await _service.ConfirmReceiptAsync(
            new ConfirmReceiptCommand { RequestId = id, EmployeeId = EmployeeId })).Succeeded.Should().BeTrue();

        var request = _db.AssetRequests.Single(r => r.Id == id);
        request.Status.Should().Be(RequestStatus.Completed);

        var asset = _db.Assets.Single(a => a.Id == 10);
        asset.Status.Should().Be(AssetStatus.Assigned);
        asset.AssignedToEmployeeId.Should().Be(EmployeeId);
    }

    [Fact]
    public async Task ConfirmReceipt_Rejected_ForSomebodyElsesRequest()
    {
        var id = await RaiseAsync();
        await _service.ApproveAsync(id, ManagerId);
        await _service.FulfilAsync(new FulfilRequestCommand
        {
            RequestId = id, AssetIds = { 10 }, Mode = FulfilmentMode.Office
        });

        var result = await _service.ConfirmReceiptAsync(
            new ConfirmReceiptCommand { RequestId = id, EmployeeId = ExecutiveId });

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("your own");
    }

    [Fact]
    public async Task FulfilAsync_Fails_WhenAssetIsNotInStock()
    {
        var id = await RaiseAsync();
        await _service.ApproveAsync(id, ManagerId);

        _db.Assets.Single(a => a.Id == 10).Status = AssetStatus.UnderService;
        await _db.SaveChangesAsync();

        var result = await _service.FulfilAsync(new FulfilRequestCommand
        {
            RequestId = id, AssetIds = { 10 }, Mode = FulfilmentMode.Office
        });

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("Not available");
    }

    [Fact]
    public async Task RejectAsync_RequiresAReason()
    {
        var id = await RaiseAsync();

        var result = await _service.RejectAsync(id, ManagerId, "   ");

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task GetPendingForManagerAsync_OnlyReturnsDirectReports()
    {
        await RaiseAsync();

        var mine = await _service.GetPendingForManagerAsync(ManagerId);
        var theirs = await _service.GetPendingForManagerAsync(ExecutiveId);

        mine.Should().ContainSingle();
        theirs.Should().BeEmpty();
    }

    [Fact]
    public async Task CancelAsync_OnlyAllowedByRequesterWhilePending()
    {
        var id = await RaiseAsync();

        (await _service.CancelAsync(id, ExecutiveId)).Succeeded.Should().BeFalse();
        (await _service.CancelAsync(id, EmployeeId)).Succeeded.Should().BeTrue();

        _db.AssetRequests.Single(r => r.Id == id).Status.Should().Be(RequestStatus.Cancelled);
    }

    [Fact]
    public async Task EscalateToHrAsync_MovesRequestToAwaitingHr_NotStraightToUnclaimed()
    {
        var id = await RaiseAsync();

        var result = await _service.EscalateToHrAsync(id, ManagerId);

        result.Succeeded.Should().BeTrue();
        _db.AssetRequests.Single(r => r.Id == id).Status.Should().Be(RequestStatus.PendingHrApproval);
    }

    [Fact]
    public async Task GetPendingForManagerAsync_NoLongerShowsARequest_OnceEscalated()
    {
        var id = await RaiseAsync();
        await _service.EscalateToHrAsync(id, ManagerId);

        var mine = await _service.GetPendingForManagerAsync(ManagerId);

        mine.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPendingAsync_OnlyShowsRequests_EscalatedByAManager()
    {
        var id = await RaiseAsync();

        // Still awaiting the manager — HR shouldn't see it yet.
        (await _service.GetPendingAsync(null)).Should().BeEmpty();

        await _service.EscalateToHrAsync(id, ManagerId);

        var hrQueue = await _service.GetPendingAsync(null);
        hrQueue.Should().ContainSingle(r => r.Id == id);
    }

    [Fact]
    public async Task ApproveAsync_FinalisesARequest_AfterManagerEscalation()
    {
        var id = await RaiseAsync();
        await _service.EscalateToHrAsync(id, ManagerId);

        var result = await _service.ApproveAsync(id, ExecutiveId);

        result.Succeeded.Should().BeTrue();
        _db.AssetRequests.Single(r => r.Id == id).Status.Should().Be(RequestStatus.Unclaimed);
    }

    [Fact]
    public async Task FulfilAsync_ImplicitlyApproves_AStillPendingRequest()
    {
        var id = await RaiseAsync();
        await _service.EscalateToHrAsync(id, ManagerId);
        _currentUser.EmployeeId = ExecutiveId;

        // No separate ApproveAsync call — HR fulfils directly from the escalated card.
        var fulfil = await _service.FulfilAsync(new FulfilRequestCommand
        {
            RequestId = id, AssetIds = { 10 }, Mode = FulfilmentMode.Office
        });

        fulfil.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetPendingAsync_PicksUpARequest_WhoseManagerIsNotAReportingManager()
    {
        // The "manager" on file is really an Admin/HR/IT record (or a Reporting Manager
        // raising a request for themselves) — nobody ever queries a Team Approvals queue
        // for a role other than Reporting Manager, so without this fallback the request
        // would be orphaned as Pending forever.
        _db.Employees.Single(e => e.Id == ManagerId).RoleName = Roles.ITAssetManager;
        await _db.SaveChangesAsync();

        var id = await RaiseAsync();

        var hrQueue = await _service.GetPendingAsync(null);

        hrQueue.Should().ContainSingle(r => r.Id == id);
    }

    [Fact]
    public async Task GetCountsAsync_ReflectsQueueState()
    {
        var first = await RaiseAsync();
        await RaiseAsync();
        await _service.ApproveAsync(first, ManagerId);

        var counts = await _service.GetCountsAsync();

        counts.Pending.Should().Be(1);
        counts.Unclaimed.Should().Be(1);
    }
}
