using FluentAssertions;
using KIPL.AssetManagement.Application.Inventory;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Domain.Enums;
using Xunit;

namespace KIPL.AssetManagement.UnitTests;

public class InventoryServiceTests : IDisposable
{
    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly InventoryService _service;

    public InventoryServiceTests()
    {
        _service = new InventoryService(_db, new FakeAuditService(_db), new FakeTagGenerator(), new FakeCurrentUserService());

        _db.Employees.Add(new Employee { Id = 1, FullName = "Priya Shankar", Email = "p@kipl.com" });
        _db.Assets.AddRange(
            new Asset { Id = 1, Tag = "LAP-0001", Brand = "Dell", Category = AssetCategory.Laptop, Status = AssetStatus.InStock },
            new Asset { Id = 2, Tag = "MON-0001", Brand = "LG", Category = AssetCategory.Monitor, Status = AssetStatus.Assigned, AssignedToEmployeeId = 1 },
            new Asset { Id = 3, Tag = "KEY-0001", Brand = "Keychron", Category = AssetCategory.Keyboard, Status = AssetStatus.Retired, Condition = AssetCondition.Retired });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateAsync_GeneratesTag_WhenNoneSupplied()
    {
        var result = await _service.CreateAsync(new CreateAssetCommand
        {
            Brand = "HP EliteBook",
            Category = AssetCategory.Laptop
        });

        result.Succeeded.Should().BeTrue();

        var created = _db.Assets.Single(a => a.Id == result.Value);
        created.Tag.Should().StartWith("TST-");
        created.Status.Should().Be(AssetStatus.InStock);
    }

    [Fact]
    public async Task CreateAsync_Fails_OnDuplicateTag()
    {
        var result = await _service.CreateAsync(new CreateAssetCommand { Brand = "Dell", Tag = "LAP-0001" });

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("already in use");
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenBrandMissing()
    {
        var result = await _service.CreateAsync(new CreateAssetCommand { Brand = "  " });

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AssignAsync_LinksAssetAndWritesCustodyRecord()
    {
        var result = await _service.AssignAsync(1, 1, "Onboarding", FulfilmentMode.Office, null, null, null);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        _db.Assets.Single(a => a.Id == 1).Status.Should().Be(AssetStatus.Assigned);
        _db.AssetAssignments.Should().ContainSingle(x => x.AssetId == 1 && x.ReturnedUtc == null);
    }

    [Fact]
    public async Task AssignAsync_Fails_WhenAssetAlreadyAssigned()
    {
        var result = await _service.AssignAsync(2, 1, null, FulfilmentMode.Office, null, null, null);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("not assignable");
    }

    [Fact]
    public async Task GetAssignableAsync_ExcludesAssignedAndRetired()
    {
        var assignable = await _service.GetAssignableAsync(null);

        assignable.Should().ContainSingle();
        assignable[0].Tag.Should().Be("LAP-0001");
    }

    [Fact]
    public async Task GetCountsAsync_GroupsByStatus()
    {
        var counts = await _service.GetCountsAsync();

        counts.Total.Should().Be(3);
        counts.InStock.Should().Be(1);
        counts.Assigned.Should().Be(1);
        counts.Retired.Should().Be(1);
    }

    [Fact]
    public async Task UnassignAsync_ClosesTheOpenCustodyRecord()
    {
        await _service.AssignAsync(1, 1, null, FulfilmentMode.Office, null, null, null);

        var result = await _service.UnassignAsync(1);

        result.Succeeded.Should().BeTrue();
        _db.AssetAssignments.Single(x => x.AssetId == 1).ReturnedUtc.Should().NotBeNull();
        _db.Assets.Single(a => a.Id == 1).Status.Should().Be(AssetStatus.InStock);
    }

    [Fact]
    public async Task SearchAsync_FiltersByStatus()
    {
        var page = await _service.SearchAsync(new AssetFilter { Status = AssetStatus.Retired });

        page.TotalCount.Should().Be(1);
        page.Items[0].Tag.Should().Be("KEY-0001");
    }

    [Fact]
    public async Task SearchAsync_MatchesOnTagAndBrand()
    {
        var byTag = await _service.SearchAsync(new AssetFilter { Search = "MON-0001" });
        var byBrand = await _service.SearchAsync(new AssetFilter { Search = "Keychron" });

        byTag.TotalCount.Should().Be(1);
        byBrand.TotalCount.Should().Be(1);
    }
}
