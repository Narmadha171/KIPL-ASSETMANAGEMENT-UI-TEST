using KIPL.AssetManagement.Application.Inventory;

namespace KIPL.AssetManagement.Web.Pages.Shared;

/// <summary>Data the shared approval dialogs need — currently the assignable stock list.</summary>
public class ApprovalModalsModel
{
    public ApprovalModalsModel(IReadOnlyList<AssetListItemDto> assignableAssets)
        => AssignableAssets = assignableAssets;

    public IReadOnlyList<AssetListItemDto> AssignableAssets { get; }
}
