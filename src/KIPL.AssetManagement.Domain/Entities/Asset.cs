using KIPL.AssetManagement.Domain.Common;
using KIPL.AssetManagement.Domain.Enums;
using KIPL.AssetManagement.Domain.Exceptions;

namespace KIPL.AssetManagement.Domain.Entities;

/// <summary>A single physical asset, uniquely identified by its printed tag.</summary>
public class Asset : AuditableEntity
{
    public string Tag { get; set; } = string.Empty;
    public AssetCategory Category { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }

    public AssetCondition Condition { get; set; } = AssetCondition.New;
    public AssetStatus Status { get; set; } = AssetStatus.InStock;

    public int? AssignedToEmployeeId { get; set; }
    public Employee? AssignedToEmployee { get; set; }

    /// <summary>Free-text note used for Lost / Retired assets, e.g. "Last seen: John Doe".</summary>
    public string? StatusNote { get; set; }

    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseCost { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }

    public ICollection<AssetAssignment> Assignments { get; set; } = new List<AssetAssignment>();

    /// <summary>An asset can only be handed out when it is free stock and physically usable.</summary>
    public bool IsAssignable => Status == AssetStatus.InStock && Condition != AssetCondition.Retired;

    public void AssignTo(Employee employee, string? actor = null)
    {
        if (!IsAssignable)
            throw new DomainException($"Asset {Tag} is not assignable while its status is {Status}.");

        AssignedToEmployeeId = employee.Id;
        AssignedToEmployee = employee;
        Status = AssetStatus.Assigned;
        StatusNote = null;
        Touch(actor);
    }

    public void MarkInTransit(string? actor = null)
    {
        if (Status is AssetStatus.Retired or AssetStatus.Lost)
            throw new DomainException($"Asset {Tag} cannot be dispatched from status {Status}.");
        Status = AssetStatus.InTransit;
        Touch(actor);
    }

    public void ReturnToStock(string? actor = null)
    {
        AssignedToEmployeeId = null;
        AssignedToEmployee = null;
        Status = AssetStatus.InStock;
        StatusNote = null;
        Touch(actor);
    }

    public void SendToService(string? actor = null)
    {
        Status = AssetStatus.UnderService;
        Touch(actor);
    }

    public void Retire(string? reason, string? actor = null)
    {
        Status = AssetStatus.Retired;
        Condition = AssetCondition.Retired;
        AssignedToEmployeeId = null;
        AssignedToEmployee = null;
        StatusNote = reason ?? "Decommissioned";
        Touch(actor);
    }

    public void ReportLost(string? lastSeenWith, string? actor = null)
    {
        Status = AssetStatus.Lost;
        StatusNote = string.IsNullOrWhiteSpace(lastSeenWith) ? "Lost" : $"Last seen: {lastSeenWith}";
        AssignedToEmployeeId = null;
        AssignedToEmployee = null;
        Touch(actor);
    }

    private void Touch(string? actor)
    {
        ModifiedUtc = DateTime.UtcNow;
        ModifiedBy = actor;
    }
}
