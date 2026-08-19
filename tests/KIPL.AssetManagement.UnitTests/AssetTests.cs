using FluentAssertions;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Domain.Enums;
using KIPL.AssetManagement.Domain.Exceptions;
using Xunit;

namespace KIPL.AssetManagement.UnitTests;

public class AssetTests
{
    private static Asset NewAsset(AssetStatus status = AssetStatus.InStock, AssetCondition condition = AssetCondition.New)
        => new() { Id = 1, Tag = "LAP-0001", Brand = "Dell", Status = status, Condition = condition };

    private static Employee NewEmployee() => new() { Id = 7, FullName = "Priya Shankar", Email = "p@kipl.com" };

    [Fact]
    public void InStockAsset_IsAssignable()
        => NewAsset().IsAssignable.Should().BeTrue();

    [Theory]
    [InlineData(AssetStatus.Assigned)]
    [InlineData(AssetStatus.InTransit)]
    [InlineData(AssetStatus.UnderService)]
    [InlineData(AssetStatus.Retired)]
    [InlineData(AssetStatus.Lost)]
    public void AssetNotInStock_IsNotAssignable(AssetStatus status)
        => NewAsset(status).IsAssignable.Should().BeFalse();

    [Fact]
    public void RetiredCondition_BlocksAssignment_EvenWhenInStock()
        => NewAsset(AssetStatus.InStock, AssetCondition.Retired).IsAssignable.Should().BeFalse();

    [Fact]
    public void AssignTo_SetsHolderAndStatus()
    {
        var asset = NewAsset();
        var employee = NewEmployee();

        asset.AssignTo(employee);

        asset.Status.Should().Be(AssetStatus.Assigned);
        asset.AssignedToEmployeeId.Should().Be(employee.Id);
    }

    [Fact]
    public void AssignTo_Throws_WhenAssetAlreadyAssigned()
    {
        var asset = NewAsset(AssetStatus.Assigned);

        var act = () => asset.AssignTo(NewEmployee());

        act.Should().Throw<DomainException>().WithMessage("*not assignable*");
    }

    [Fact]
    public void Retire_ClearsHolderAndRecordsReason()
    {
        var asset = NewAsset(AssetStatus.Assigned);
        asset.AssignedToEmployeeId = 7;

        asset.Retire("End of life");

        asset.Status.Should().Be(AssetStatus.Retired);
        asset.Condition.Should().Be(AssetCondition.Retired);
        asset.AssignedToEmployeeId.Should().BeNull();
        asset.StatusNote.Should().Be("End of life");
    }

    [Fact]
    public void ReportLost_RecordsWhoHadItLast()
    {
        var asset = NewAsset(AssetStatus.Assigned);

        asset.ReportLost("John Doe");

        asset.Status.Should().Be(AssetStatus.Lost);
        asset.StatusNote.Should().Be("Last seen: John Doe");
    }

    [Fact]
    public void ReturnToStock_MakesAssetAvailableAgain()
    {
        var asset = NewAsset(AssetStatus.Assigned);
        asset.AssignedToEmployeeId = 7;

        asset.ReturnToStock();

        asset.Status.Should().Be(AssetStatus.InStock);
        asset.AssignedToEmployeeId.Should().BeNull();
        asset.IsAssignable.Should().BeTrue();
    }

    [Fact]
    public void MarkInTransit_Throws_ForRetiredAsset()
    {
        var asset = NewAsset(AssetStatus.Retired);

        var act = () => asset.MarkInTransit();

        act.Should().Throw<DomainException>();
    }
}
