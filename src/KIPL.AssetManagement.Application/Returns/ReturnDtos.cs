using KIPL.AssetManagement.Domain.Enums;

namespace KIPL.AssetManagement.Application.Returns;

public record ReturnListItemDto(
    int Id,
    string Reference,
    string EmployeeName,
    string AssetName,
    string AssetTag,
    AssetCategory Category,
    FulfilmentMode Mode,
    ReturnStage Stage,
    string? TrackingNumber,
    string? DropOffLocation,
    DateTime RaisedUtc,
    DateTime? HandoverUtc,
    DateTime? ExpectedUtc,
    AssetCondition? InspectedCondition,
    string? InspectionNotes);

public class RaiseReturnCommand
{
    public int EmployeeId { get; set; }
    public int AssetId { get; set; }
    public FulfilmentMode Mode { get; set; } = FulfilmentMode.Office;
    public string? Reason { get; set; }
    public string? DropOffLocation { get; set; }
    public string? CourierService { get; set; }
    public DateTime? ExpectedUtc { get; set; }
    public Common.Models.PhotoUpload? Photo { get; set; }
}

public class InspectReturnCommand
{
    public int ReturnId { get; set; }
    public int InspectorEmployeeId { get; set; }
    public AssetCondition Condition { get; set; } = AssetCondition.Good;
    public string? Notes { get; set; }
    public Common.Models.PhotoUpload? Photo { get; set; }
}

public record ReturnCountsDto(int AwaitingPickup, int InTransit, int AwaitingInspection, int ClosedThisMonth);
