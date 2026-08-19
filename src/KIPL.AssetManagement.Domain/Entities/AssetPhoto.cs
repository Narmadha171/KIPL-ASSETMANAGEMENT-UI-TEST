using KIPL.AssetManagement.Domain.Common;
using KIPL.AssetManagement.Domain.Enums;

namespace KIPL.AssetManagement.Domain.Entities;

/// <summary>
/// A photograph captured at a point in an asset's lifecycle. Only the stored
/// path is held here; the bytes live on disk via IFileStorage.
/// </summary>
public class AssetPhoto : BaseEntity
{
    public PhotoKind Kind { get; set; }

    /// <summary>Relative path under the configured upload root, e.g. "2026/07/abc123.jpg".</summary>
    public string StoredPath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    public DateTime CapturedUtc { get; set; } = DateTime.UtcNow;
    public string? CapturedBy { get; set; }

    // Whichever part of the workflow the photo belongs to.
    public int? AssetId { get; set; }
    public Asset? Asset { get; set; }

    public int? AssetRequestId { get; set; }
    public AssetRequest? AssetRequest { get; set; }

    public int? AssetReturnId { get; set; }
    public AssetReturn? AssetReturn { get; set; }
}
