using KIPL.AssetManagement.Domain.Enums;

namespace KIPL.AssetManagement.Application.Inventory;

public record AssetListItemDto(
    int Id,
    string Tag,
    AssetCategory Category,
    string Brand,
    AssetCondition Condition,
    AssetStatus Status,
    string AssignedToDisplay,
    bool IsAssignable);

public record AssetDetailDto(
    int Id,
    string Tag,
    AssetCategory Category,
    string Brand,
    string? Model,
    string? SerialNumber,
    AssetCondition Condition,
    AssetStatus Status,
    int? AssignedToEmployeeId,
    string AssignedToDisplay,
    DateTime? PurchaseDate,
    decimal? PurchaseCost,
    DateTime? WarrantyExpiry,
    string? Location,
    string? Notes,
    IReadOnlyList<AssignmentHistoryDto> History);

public record AssignmentHistoryDto(string EmployeeName, DateTime AssignedUtc, DateTime? ReturnedUtc, string? AssignedBy);

public class AssetFilter
{
    public string? Search { get; set; }
    public AssetCategory? Category { get; set; }
    public AssetStatus? Status { get; set; }
    public AssetCondition? Condition { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public class CreateAssetCommand
{
    public AssetCategory Category { get; set; } = AssetCategory.Laptop;
    public string Brand { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public AssetCondition Condition { get; set; } = AssetCondition.New;
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseCost { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    /// <summary>Optional explicit tag; when blank one is generated.</summary>
    public string? Tag { get; set; }
}

public class UpdateAssetCommand : CreateAssetCommand
{
    public int Id { get; set; }
}

public record InventoryCountsDto(
    int Total, int InStock, int Assigned, int InTransit, int UnderService, int Retired, int Lost);
