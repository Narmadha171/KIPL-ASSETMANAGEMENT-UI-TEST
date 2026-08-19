using KIPL.AssetManagement.Domain.Enums;

namespace KIPL.AssetManagement.Application.Common.Interfaces;

/// <summary>Produces the human-readable asset tags (e.g. LAP-7F3K9Q) and reference numbers.</summary>
public interface ITagGenerator
{
    string NewAssetTag(AssetCategory category);
    string NewReturnReference();
    string NewServiceReference();
}
