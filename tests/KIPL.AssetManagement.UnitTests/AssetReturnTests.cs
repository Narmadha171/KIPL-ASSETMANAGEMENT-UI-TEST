using FluentAssertions;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Domain.Enums;
using Xunit;

namespace KIPL.AssetManagement.UnitTests;

public class AssetReturnTests
{
    private static (AssetReturn Return, Asset Asset) Build(
        FulfilmentMode mode = FulfilmentMode.Office,
        ReturnStage stage = ReturnStage.AwaitingPickup)
    {
        var asset = new Asset
        {
            Id = 1,
            Tag = "LAP-0001",
            Brand = "Dell",
            Status = AssetStatus.Assigned,
            AssignedToEmployeeId = 5
        };

        var ret = new AssetReturn
        {
            Id = 1,
            Reference = "RET-0001",
            EmployeeId = 5,
            AssetId = asset.Id,
            Asset = asset,
            Mode = mode,
            Stage = stage
        };

        return (ret, asset);
    }

    private static Employee NewInspector() => new() { Id = 2, FullName = "Revanth K", Email = "r@kipl.com" };

    [Fact]
    public void CourierHandover_MovesToInTransit()
    {
        var (ret, _) = Build(FulfilmentMode.Courier);

        ret.MarkHandedOver("1Z999");

        ret.Stage.Should().Be(ReturnStage.InTransit);
        ret.CourierTrackingNumber.Should().Be("1Z999");
    }

    [Fact]
    public void OfficeHandover_GoesStraightToInspection()
    {
        var (ret, _) = Build();

        ret.MarkHandedOver(null);

        ret.Stage.Should().Be(ReturnStage.ReceivedForInspection);
    }

    [Fact]
    public void InspectingAsGood_ReturnsAssetToStock()
    {
        var (ret, asset) = Build(stage: ReturnStage.ReceivedForInspection);

        ret.Inspect(NewInspector(), AssetCondition.Good, "No damage");

        ret.Stage.Should().Be(ReturnStage.Closed);
        asset.Status.Should().Be(AssetStatus.InStock);
        asset.Condition.Should().Be(AssetCondition.Good);
        asset.AssignedToEmployeeId.Should().BeNull();
    }

    [Fact]
    public void InspectingAsDamaged_SendsAssetToService()
    {
        var (ret, asset) = Build(stage: ReturnStage.ReceivedForInspection);

        ret.Inspect(NewInspector(), AssetCondition.Damaged, "Cracked screen");

        asset.Status.Should().Be(AssetStatus.UnderService);
    }

    [Fact]
    public void InspectingAsBeyondRepair_RetiresAsset()
    {
        var (ret, asset) = Build(stage: ReturnStage.ReceivedForInspection);

        ret.Inspect(NewInspector(), AssetCondition.Retired, "Water damage");

        asset.Status.Should().Be(AssetStatus.Retired);
        asset.IsAssignable.Should().BeFalse();
    }
}
