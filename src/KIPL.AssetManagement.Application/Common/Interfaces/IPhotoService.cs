using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Domain.Enums;

namespace KIPL.AssetManagement.Application.Common.Interfaces;

/// <summary>Attaches captured photos to the workflow step they belong to.</summary>
public interface IPhotoService
{
    /// <summary>
    /// Validates and stores a photo. Returns failure on an oversized or
    /// non-image upload; a null upload is a no-op success, since most steps
    /// treat the photo as optional.
    /// </summary>
    /// <param name="saveChanges">
    /// Leave false when a calling service will save as part of its own
    /// transaction; set true when attaching a photo is the whole operation.
    /// </param>
    Task<Result> AttachAsync(
        PhotoUpload? upload,
        PhotoKind kind,
        int? assetId = null,
        int? requestId = null,
        int? returnId = null,
        bool saveChanges = false,
        CancellationToken ct = default);

    Task<IReadOnlyList<PhotoDto>> GetForAssetAsync(int assetId, CancellationToken ct = default);
    Task<IReadOnlyList<PhotoDto>> GetForReturnAsync(int returnId, CancellationToken ct = default);
}

public record PhotoDto(int Id, PhotoKind Kind, string Url, string OriginalFileName, DateTime CapturedUtc, string? CapturedBy);
