namespace KIPL.AssetManagement.Domain.Enums;

public enum ReturnStage
{
    AwaitingPickup = 0,
    InTransit = 1,
    ReceivedForInspection = 2,
    Inspected = 3,
    Closed = 4
}
