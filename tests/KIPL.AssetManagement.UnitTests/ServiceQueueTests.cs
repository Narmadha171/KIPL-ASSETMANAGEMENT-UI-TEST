using FluentAssertions;
using KIPL.AssetManagement.Application.ServiceRequests;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Domain.Enums;
using Xunit;

namespace KIPL.AssetManagement.UnitTests;

public class ServiceQueueTests : IDisposable
{
    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly ServiceRequestService _service;

    public ServiceQueueTests()
    {
        _service = new ServiceRequestService(_db, new FakeAuditService(_db), new FakeTagGenerator());

        _db.Employees.Add(new Employee { Id = 1, FullName = "Sarah Jenkins", Email = "s@kipl.com" });
        _db.Assets.Add(new Asset
        {
            Id = 1, Tag = "LAP-0001", Brand = "Dell",
            Status = AssetStatus.Assigned, AssignedToEmployeeId = 1
        });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    private async Task<int> ReportAsync()
    {
        var result = await _service.ReportAsync(new ReportIssueCommand
        {
            AssetId = 1,
            ReportedById = 1,
            IssueType = "Battery",
            Description = "Drains in two hours.",
            Urgency = ServiceUrgency.AffectsMyWork
        });

        result.Succeeded.Should().BeTrue();
        return result.Value;
    }

    [Fact]
    public async Task Report_RequiresADescription()
    {
        var result = await _service.ReportAsync(new ReportIssueCommand { AssetId = 1, ReportedById = 1 });

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Accept_MovesAssetUnderService()
    {
        var id = await ReportAsync();

        (await _service.AcceptAsync(id)).Succeeded.Should().BeTrue();

        _db.ServiceRequests.Single(s => s.Id == id).Status.Should().Be(ServiceRequestStatus.InProgress);
        _db.Assets.Single(a => a.Id == 1).Status.Should().Be(AssetStatus.UnderService);
    }

    [Fact]
    public async Task Accept_Fails_WhenAlreadyInProgress()
    {
        var id = await ReportAsync();
        await _service.AcceptAsync(id);

        var second = await _service.AcceptAsync(id);

        second.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Dismiss_RequiresAReason()
    {
        var id = await ReportAsync();

        var result = await _service.DismissAsync(id, "  ");

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Dismiss_ClosesWithoutTouchingTheAsset()
    {
        var id = await ReportAsync();

        (await _service.DismissAsync(id, "Working as designed.")).Succeeded.Should().BeTrue();

        _db.ServiceRequests.Single(s => s.Id == id).Status.Should().Be(ServiceRequestStatus.Closed);
        _db.Assets.Single(a => a.Id == 1).Status.Should().Be(AssetStatus.Assigned);
    }

    [Fact]
    public async Task Resolve_ReturnsARepairedAssetToStock()
    {
        var id = await ReportAsync();
        await _service.AcceptAsync(id);

        (await _service.ResolveAsync(id, "Battery replaced.")).Succeeded.Should().BeTrue();

        _db.Assets.Single(a => a.Id == 1).Status.Should().Be(AssetStatus.InStock);
    }

    [Fact]
    public async Task OpenCount_ExcludesClosedReports()
    {
        var first = await ReportAsync();
        await ReportAsync();
        await _service.DismissAsync(first, "Duplicate.");

        (await _service.GetOpenCountAsync()).Should().Be(1);
    }
}
