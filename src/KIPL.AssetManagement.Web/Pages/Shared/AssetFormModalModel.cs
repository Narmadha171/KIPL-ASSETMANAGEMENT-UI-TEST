using KIPL.AssetManagement.Application.Inventory;
using KIPL.AssetManagement.Domain.Enums;

namespace KIPL.AssetManagement.Web.Pages.Shared;

/// <summary>Backs both the add-asset and edit-asset dialogs.</summary>
public class AssetFormModalModel
{
    public AssetFormModalModel(AssetFormInput input, bool isEdit, bool openOnLoad = false)
    {
        Input = input;
        IsEdit = isEdit;
        OpenOnLoad = openOnLoad;
    }

    public AssetFormInput Input { get; }
    public bool IsEdit { get; }

    /// <summary>True after a failed post, so the user sees the validation message.</summary>
    public bool OpenOnLoad { get; }
}

/// <summary>Backs the per-row edit-asset dialog rendered once per inventory row.</summary>
public class EditAssetModalModel
{
    public EditAssetModalModel(AssetDetailDto asset, bool openOnLoad)
    {
        Asset = asset;
        OpenOnLoad = openOnLoad;
    }

    public AssetDetailDto Asset { get; }
    public bool OpenOnLoad { get; }
}

public class AssetFormInput
{
    public int Id { get; set; }
    public string? Tag { get; set; }
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
}
