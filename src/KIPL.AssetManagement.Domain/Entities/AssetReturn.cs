using KIPL.AssetManagement.Domain.Common;
using KIPL.AssetManagement.Domain.Enums;
using KIPL.AssetManagement.Domain.Exceptions;

namespace KIPL.AssetManagement.Domain.Entities;

/// <summary>An employee handing an asset back to IT.</summary>
public class AssetReturn : AuditableEntity
{
    public string Reference { get; set; } = string.Empty;   // e.g. RET-2045

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    public FulfilmentMode Mode { get; set; } = FulfilmentMode.Office;
    public ReturnStage Stage { get; set; } = ReturnStage.AwaitingPickup;

    public string? Reason { get; set; }
    public string? CourierTrackingNumber { get; set; }
    public string? DropOffLocation { get; set; }

    public DateTime RaisedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? HandoverUtc { get; set; }
    public DateTime? ExpectedUtc { get; set; }
    public DateTime? ReceivedUtc { get; set; }

    public AssetCondition? InspectedCondition { get; set; }
    public string? InspectionNotes { get; set; }
    public int? InspectedById { get; set; }
    public Employee? InspectedBy { get; set; }

    public bool IsClosed => Stage == ReturnStage.Closed;

    public void MarkHandedOver(string? tracking)
    {
        if (Stage != ReturnStage.AwaitingPickup)
            throw new DomainException("Return is no longer awaiting pickup.");
        Stage = Mode == FulfilmentMode.Courier ? ReturnStage.InTransit : ReturnStage.ReceivedForInspection;
        HandoverUtc = DateTime.UtcNow;
        if (Mode == FulfilmentMode.Courier) CourierTrackingNumber = tracking;
    }

    public void MarkReceived()
    {
        Stage = ReturnStage.ReceivedForInspection;
        ReceivedUtc = DateTime.UtcNow;
    }

    /// <summary>Inspection closes the return and decides where the asset lands.</summary>
    public void Inspect(Employee inspector, AssetCondition condition, string? notes)
    {
        if (Stage is ReturnStage.Closed)
            throw new DomainException("This return is already closed.");

        InspectedById = inspector.Id;
        InspectedCondition = condition;
        InspectionNotes = notes;
        Stage = ReturnStage.Closed;
        ReceivedUtc ??= DateTime.UtcNow;

        if (condition == AssetCondition.Damaged) Asset.SendToService();
        else if (condition == AssetCondition.Retired) Asset.Retire("Retired on return inspection");
        else
        {
            Asset.Condition = condition;
            Asset.ReturnToStock();
        }
    }
}
