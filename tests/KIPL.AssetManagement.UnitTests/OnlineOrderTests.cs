using FluentAssertions;
using KIPL.AssetManagement.Application.Common;
using KIPL.AssetManagement.Application.Requests;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Domain.Enums;
using Xunit;

namespace KIPL.AssetManagement.UnitTests;

/// <summary>
/// The online-order route: ops logs an order, HR checks the delivery in, and the
/// item only becomes inventory once verified.
/// </summary>
public class OnlineOrderTests : IDisposable
{
    private const int EmployeeId = 1;
    private const int ManagerId = 2;

    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly AssetRequestService _service;

    public OnlineOrderTests()
    {
        var currentUser = new FakeCurrentUserService();
        var photos = new PhotoService(_db, new FakeFileStorage(), currentUser);
        _service = new AssetRequestService(_db, new FakeAuditService(_db), photos, currentUser);

        _db.Departments.Add(new Department { Id = 1, Name = "Engineering" });
        _db.Employees.AddRange(
            new Employee { Id = ManagerId, FullName = "Michael Chen", Email = "m@kipl.com", DepartmentId = 1 },
            new Employee { Id = EmployeeId, FullName = "Sarah Jenkins", Email = "s@kipl.com", DepartmentId = 1, ManagerId = ManagerId });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    private async Task<int> ApprovedRequestAsync()
    {
        var raised = await _service.RaiseAsync(new RaiseRequestCommand
        {
            RequesterId = EmployeeId,
            ItemName = "Monitor 27\"",
            Category = AssetCategory.Monitor,
            Reason = "Dual screen setup."
        });

        await _service.ApproveAsync(raised.Value, ManagerId);
        return raised.Value;
    }

    private async Task<int> OrderedRequestAsync()
    {
        var id = await ApprovedRequestAsync();
        await _service.PlaceOnlineOrderAsync(new OnlineOrderCommand
        {
            RequestId = id, OrderId = "ORD-1", Vendor = "Dell", Brand = "Dell", Model = "U2723QE"
        });
        return id;
    }

    [Fact]
    public async Task PlacingAnOrder_MarksTheRequestAsOnlineSourced()
    {
        var id = await OrderedRequestAsync();

        var request = _db.AssetRequests.Single(r => r.Id == id);
        request.Status.Should().Be(RequestStatus.OnlineOrdered);
        request.Source.Should().Be(RequestSource.Online);
        request.OrderId.Should().Be("ORD-1");
    }

    [Fact]
    public async Task AwaitingVerification_ListsUnverifiedOrdersOnly()
    {
        await OrderedRequestAsync();

        var waiting = await _service.GetAwaitingVerificationAsync();

        waiting.Should().ContainSingle();
    }

    [Fact]
    public async Task Verification_RequiresASerialNumber()
    {
        var id = await OrderedRequestAsync();

        var result = await _service.VerifyOnlineDeliveryAsync(new VerifyOnlineOrderCommand { RequestId = id });

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("serial number");
    }

    [Fact]
    public async Task DeclaredMismatch_MustBeExplained()
    {
        var id = await OrderedRequestAsync();

        var result = await _service.VerifyOnlineDeliveryAsync(new VerifyOnlineOrderCommand
        {
            RequestId = id, SerialNumber = "SN-1", TagMismatch = true
        });

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("mismatch");
    }

    [Fact]
    public async Task Verification_CreatesTheAssetInStock()
    {
        var id = await OrderedRequestAsync();

        var result = await _service.VerifyOnlineDeliveryAsync(new VerifyOnlineOrderCommand
        {
            RequestId = id, SerialNumber = "SN-12345", AssetTag = "MON-9001"
        });

        result.Succeeded.Should().BeTrue();

        var asset = _db.Assets.Single(a => a.Tag == "MON-9001");
        asset.Status.Should().Be(AssetStatus.InStock);
        asset.SerialNumber.Should().Be("SN-12345");
        asset.Brand.Should().Be("Dell");

        _db.AssetRequests.Single(r => r.Id == id).OnlineOrderVerified.Should().BeTrue();
    }

    [Fact]
    public async Task Verification_GeneratesATagWhenNoneSupplied()
    {
        var id = await OrderedRequestAsync();

        await _service.VerifyOnlineDeliveryAsync(new VerifyOnlineOrderCommand
        {
            RequestId = id, SerialNumber = "SN-777"
        });

        _db.Assets.Should().ContainSingle(a => a.SerialNumber == "SN-777" && a.Tag.Length > 0);
    }

    [Fact]
    public async Task MismatchNote_IsRecordedOnTheAsset()
    {
        var id = await OrderedRequestAsync();

        await _service.VerifyOnlineDeliveryAsync(new VerifyOnlineOrderCommand
        {
            RequestId = id,
            SerialNumber = "SN-9",
            TagMismatch = true,
            MismatchNote = "Wrong model shipped."
        });

        _db.Assets.Single().Notes.Should().Contain("Wrong model shipped.");
    }

    [Fact]
    public async Task SelfApproval_IsBlocked()
    {
        var raised = await _service.RaiseAsync(new RaiseRequestCommand
        {
            RequesterId = ManagerId,
            ItemName = "Dock",
            Category = AssetCategory.DockingStation,
            Reason = "Desk setup."
        });

        var result = await _service.ApproveAsync(raised.Value, ManagerId);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("your own");
    }
}
