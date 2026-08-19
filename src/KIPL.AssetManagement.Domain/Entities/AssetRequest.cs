using KIPL.AssetManagement.Domain.Common;
using KIPL.AssetManagement.Domain.Enums;
using KIPL.AssetManagement.Domain.Exceptions;

namespace KIPL.AssetManagement.Domain.Entities;

/// <summary>An employee's request for a new asset, from raise through to receipt.</summary>
public class AssetRequest : AuditableEntity
{
    public int RequesterId { get; set; }
    public Employee Requester { get; set; } = null!;

    public string ItemName { get; set; } = string.Empty;
    public AssetCategory Category { get; set; } = AssetCategory.Other;
    public string Reason { get; set; } = string.Empty;

    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public RequestSource Source { get; set; } = RequestSource.Stock;
    public FulfilmentMode? Mode { get; set; }

    public DateTime RaisedUtc { get; set; } = DateTime.UtcNow;

    public int? ApprovedById { get; set; }
    public Employee? ApprovedBy { get; set; }
    public DateTime? ApprovedUtc { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>Operations user who took ownership of fulfilling this request.</summary>
    public int? ClaimedById { get; set; }
    public Employee? ClaimedBy { get; set; }
    public DateTime? ClaimedUtc { get; set; }

    /// <summary>Set once a physical asset from inventory has been earmarked / dispatched.</summary>
    public int? FulfilledWithAssetId { get; set; }
    public Asset? FulfilledWithAsset { get; set; }

    public DateTime? DispatchedUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }

    // Online-order fields (HR verifies the delivery before it enters inventory).
    public string? OrderId { get; set; }
    public string? OrderBrand { get; set; }
    public string? OrderModel { get; set; }
    public string? OrderVendor { get; set; }
    public bool OnlineOrderVerified { get; set; }

    public string? CourierTrackingNumber { get; set; }
    public string? OfficePickupLocation { get; set; }

    public bool IsOpen => Status is not (RequestStatus.Completed or RequestStatus.Rejected or RequestStatus.Cancelled);

    /// <summary>A reporting manager's sign-off. Hands the request to HR for final approval
    /// rather than releasing it straight to IT.</summary>
    public void EscalateToHr(Employee manager)
    {
        if (Status != RequestStatus.Pending)
            throw new DomainException("Only a pending request can be sent to HR.");
        Status = RequestStatus.PendingHrApproval;
        ApprovedById = manager.Id;
        ApprovedUtc = DateTime.UtcNow;
    }

    public void Approve(Employee approver)
    {
        if (Status is not (RequestStatus.Pending or RequestStatus.PendingHrApproval))
            throw new DomainException("Only a pending request can be approved.");
        Status = RequestStatus.Unclaimed;
        ApprovedById = approver.Id;
        ApprovedUtc = DateTime.UtcNow;
    }

    public void Reject(Employee approver, string reason)
    {
        if (Status is RequestStatus.Completed or RequestStatus.Cancelled)
            throw new DomainException("A closed request cannot be rejected.");
        Status = RequestStatus.Rejected;
        ApprovedById = approver.Id;
        ApprovedUtc = DateTime.UtcNow;
        RejectionReason = reason;
    }

    public void Claim(Employee operationsUser)
    {
        if (Status != RequestStatus.Unclaimed)
            throw new DomainException("Only an unclaimed request can be claimed.");
        Status = RequestStatus.Claimed;
        ClaimedById = operationsUser.Id;
        ClaimedUtc = DateTime.UtcNow;
    }

    public void FulfilFromStock(Asset asset, FulfilmentMode mode, string? trackingOrLocation)
    {
        if (Status is not (RequestStatus.Claimed or RequestStatus.Unclaimed))
            throw new DomainException("Request must be approved before it can be fulfilled.");

        FulfilledWithAssetId = asset.Id;
        FulfilledWithAsset = asset;
        Mode = mode;
        Status = RequestStatus.Dispatched;
        DispatchedUtc = DateTime.UtcNow;

        if (mode == FulfilmentMode.Courier) CourierTrackingNumber = trackingOrLocation;
        else OfficePickupLocation = trackingOrLocation;
    }

    public void PlaceOnlineOrder(string orderId, string vendor, string brand, string? model)
    {
        if (Status is not (RequestStatus.Claimed or RequestStatus.Unclaimed))
            throw new DomainException("Request must be approved before an order is placed.");
        Source = RequestSource.Online;
        Status = RequestStatus.OnlineOrdered;
        OrderId = orderId;
        OrderVendor = vendor;
        OrderBrand = brand;
        OrderModel = model;
    }

    public void VerifyOnlineDelivery()
    {
        if (Status != RequestStatus.OnlineOrdered)
            throw new DomainException("Only an online-ordered request awaits verification.");
        OnlineOrderVerified = true;
        Status = RequestStatus.Claimed;
    }

    public void ConfirmReceipt()
    {
        if (Status != RequestStatus.Dispatched)
            throw new DomainException("Receipt can only be confirmed for a dispatched request.");
        Status = RequestStatus.Completed;
        CompletedUtc = DateTime.UtcNow;
    }
}
