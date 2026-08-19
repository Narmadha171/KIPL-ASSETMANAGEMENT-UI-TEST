using FluentAssertions;
using KIPL.AssetManagement.Application.Common;
using KIPL.AssetManagement.Application.Returns;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Domain.Enums;
using Xunit;

namespace KIPL.AssetManagement.UnitTests;

public class ReturnServiceTests : IDisposable
{
    private const int Holder = 1;
    private const int Inspector = 2;

    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly ReturnService _service;

    public ReturnServiceTests()
    {
        var currentUser = new FakeCurrentUserService();
        var photos = new PhotoService(_db, new FakeFileStorage(), currentUser);
        _service = new ReturnService(_db, new FakeAuditService(_db), new FakeTagGenerator(), photos);

        _db.Employees.AddRange(
            new Employee { Id = Holder, FullName = "Priya Shankar", Email = "p@kipl.com" },
            new Employee { Id = Inspector, FullName = "Revanth K", Email = "r@kipl.com" });

        _db.Assets.AddRange(
            new Asset { Id = 1, Tag = "LAP-0001", Brand = "Dell", Status = AssetStatus.Assigned, AssignedToEmployeeId = Holder },
            new Asset { Id = 2, Tag = "MON-0001", Brand = "LG", Status = AssetStatus.InStock });

        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task RaiseAsync_Fails_WhenAssetIsNotHeldByThatEmployee()
    {
        var result = await _service.RaiseAsync(new RaiseReturnCommand { EmployeeId = Holder, AssetId = 2 });

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("assigned to you");
    }

    [Fact]
    public async Task RaiseAsync_Fails_WhenAReturnIsAlreadyInFlight()
    {
        await _service.RaiseAsync(new RaiseReturnCommand { EmployeeId = Holder, AssetId = 1 });

        var second = await _service.RaiseAsync(new RaiseReturnCommand { EmployeeId = Holder, AssetId = 1 });

        second.Succeeded.Should().BeFalse();
        second.Error.Should().Contain("already in progress");
    }

    [Fact]
    public async Task OfficeReturn_DefaultsTheDropOffPoint()
    {
        var result = await _service.RaiseAsync(new RaiseReturnCommand
        {
            EmployeeId = Holder,
            AssetId = 1,
            Mode = FulfilmentMode.Office
        });

        var entity = _db.AssetReturns.Single(r => r.Id == result.Value);
        entity.DropOffLocation.Should().Be("IT desk, 2nd floor");
        entity.Reference.Should().StartWith("RET-");
    }

    [Fact]
    public async Task InspectAsync_ClosesReturn_AndFreesTheAsset()
    {
        var raised = await _service.RaiseAsync(new RaiseReturnCommand { EmployeeId = Holder, AssetId = 1 });
        await _service.MarkReceivedAsync(raised.Value);

        var result = await _service.InspectAsync(new InspectReturnCommand
        {
            ReturnId = raised.Value,
            InspectorEmployeeId = Inspector,
            Condition = AssetCondition.Good,
            Notes = "Light scuffing"
        });

        result.Succeeded.Should().BeTrue();

        var asset = _db.Assets.Single(a => a.Id == 1);
        asset.Status.Should().Be(AssetStatus.InStock);
        asset.AssignedToEmployeeId.Should().BeNull();

        _db.AssetReturns.Single(r => r.Id == raised.Value).Stage.Should().Be(ReturnStage.Closed);
    }

    [Fact]
    public async Task GetCountsAsync_GroupsByStage()
    {
        await _service.RaiseAsync(new RaiseReturnCommand { EmployeeId = Holder, AssetId = 1 });

        var counts = await _service.GetCountsAsync();

        counts.AwaitingPickup.Should().Be(1);
    }
}
