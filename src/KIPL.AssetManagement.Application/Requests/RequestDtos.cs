using KIPL.AssetManagement.Domain.Enums;

namespace KIPL.AssetManagement.Application.Requests;

public record RequestListItemDto(
    int Id,
    string RequesterName,
    string DepartmentName,
    string ItemName,
    AssetCategory Category,
    string Reason,
    RequestStatus Status,
    RequestSource Source,
    FulfilmentMode? Mode,
    DateTime RaisedUtc,
    string? AssetTag,
    string? OrderId,
    string? TrackingNumber,
    string? PickupLocation,
    string? ApprovedByName,
    string? RejectionReason);

public class RequestFilter
{
    public RequestStatus? Status { get; set; }
    public int? RequesterId { get; set; }
    public int? ManagerId { get; set; }
    public string? Search { get; set; }
    public bool OnlyOpen { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class RaiseRequestCommand
{
    public int RequesterId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public AssetCategory Category { get; set; } = AssetCategory.Other;
    public string Reason { get; set; } = string.Empty;
}

public class FulfilRequestCommand
{
    public int RequestId { get; set; }

    /// <summary>
    /// One or more assets. The prototype lets an operator tick several items,
    /// which then ship together on a single delivery challan.
    /// </summary>
    public List<int> AssetIds { get; set; } = new();

    public FulfilmentMode Mode { get; set; } = FulfilmentMode.Office;
    public string? CourierService { get; set; }
    public string? TrackingNumber { get; set; }
    public string? PickupPoint { get; set; }
    public string? PickupLocation { get; set; }

    public Common.Models.PhotoUpload? Photo { get; set; }
}

/// <summary>Everything the delivery challan needs, gathered after a successful dispatch.</summary>
public record ChallanDto(
    string ChallanNumber,
    DateTime IssuedUtc,
    string IssuedByName,
    string IssuedByRole,
    string EmployeeName,
    string EmployeeMeta,
    FulfilmentMode Mode,
    string? CourierService,
    string? TrackingNumber,
    string? PickupPoint,
    IReadOnlyList<ChallanLineDto> Items);

public record ChallanLineDto(string Brand, string Category, string Tag, string Condition);

public class VerifyOnlineOrderCommand
{
    public int RequestId { get; set; }
    public string? SerialNumber { get; set; }
    public string? AssetTag { get; set; }
    /// <summary>Operator ticked "the tag on the box does not match" — routes to an exception path.</summary>
    public bool TagMismatch { get; set; }
    public string? MismatchNote { get; set; }
    public Common.Models.PhotoUpload? Photo { get; set; }
}

public class ConfirmReceiptCommand
{
    public int RequestId { get; set; }
    public int EmployeeId { get; set; }
    public Common.Models.PhotoUpload? Photo { get; set; }
}

public class OnlineOrderCommand
{
    public int RequestId { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string? Model { get; set; }
}

public record RequestCountsDto(int Pending, int Unclaimed, int Claimed, int OnlineOrders, int Dispatched, int CompletedThisMonth);
