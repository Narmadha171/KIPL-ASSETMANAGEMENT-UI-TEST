using FluentAssertions;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Domain.Enums;
using KIPL.AssetManagement.Domain.Exceptions;
using Xunit;

namespace KIPL.AssetManagement.UnitTests;

public class AssetRequestTests
{
    private static AssetRequest NewRequest(RequestStatus status = RequestStatus.Pending)
        => new() { Id = 1, RequesterId = 5, ItemName = "Monitor", Reason = "Second screen", Status = status };

    private static Employee NewApprover() => new() { Id = 9, FullName = "Michael Chen", Email = "m@kipl.com" };

    [Fact]
    public void Approve_MovesRequestToUnclaimed()
    {
        var request = NewRequest();

        request.Approve(NewApprover());

        request.Status.Should().Be(RequestStatus.Unclaimed);
        request.ApprovedById.Should().Be(9);
        request.ApprovedUtc.Should().NotBeNull();
    }

    [Fact]
    public void Approve_Throws_WhenNotPending()
    {
        var request = NewRequest(RequestStatus.Completed);

        var act = () => request.Approve(NewApprover());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reject_RecordsTheReason()
    {
        var request = NewRequest();

        request.Reject(NewApprover(), "Budget freeze");

        request.Status.Should().Be(RequestStatus.Rejected);
        request.RejectionReason.Should().Be("Budget freeze");
    }

    [Fact]
    public void Claim_Throws_WhenRequestStillNeedsApproval()
    {
        var request = NewRequest();

        var act = () => request.Claim(NewApprover());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void FulfilFromStock_ByCourier_StoresTrackingNumber()
    {
        var request = NewRequest(RequestStatus.Claimed);
        var asset = new Asset { Id = 3, Tag = "MON-0001", Brand = "LG" };

        request.FulfilFromStock(asset, FulfilmentMode.Courier, "1Z999");

        request.Status.Should().Be(RequestStatus.Dispatched);
        request.CourierTrackingNumber.Should().Be("1Z999");
        request.OfficePickupLocation.Should().BeNull();
        request.FulfilledWithAssetId.Should().Be(3);
    }

    [Fact]
    public void FulfilFromStock_ForOfficeCollection_StoresPickupPoint()
    {
        var request = NewRequest(RequestStatus.Claimed);
        var asset = new Asset { Id = 3, Tag = "MON-0001", Brand = "LG" };

        request.FulfilFromStock(asset, FulfilmentMode.Office, "IT desk");

        request.OfficePickupLocation.Should().Be("IT desk");
        request.CourierTrackingNumber.Should().BeNull();
    }

    [Fact]
    public void ConfirmReceipt_Throws_UnlessDispatched()
    {
        var request = NewRequest(RequestStatus.Claimed);

        var act = () => request.ConfirmReceipt();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ConfirmReceipt_ClosesTheRequest()
    {
        var request = NewRequest(RequestStatus.Dispatched);

        request.ConfirmReceipt();

        request.Status.Should().Be(RequestStatus.Completed);
        request.CompletedUtc.Should().NotBeNull();
        request.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void OnlineOrder_RequiresVerificationBeforeFulfilment()
    {
        var request = NewRequest(RequestStatus.Claimed);

        request.PlaceOnlineOrder("ORD-1", "Dell", "Dell", "U2723QE");
        request.Status.Should().Be(RequestStatus.OnlineOrdered);
        request.Source.Should().Be(RequestSource.Online);

        request.VerifyOnlineDelivery();
        request.Status.Should().Be(RequestStatus.Claimed);
        request.OnlineOrderVerified.Should().BeTrue();
    }
}
