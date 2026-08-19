using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KIPL.AssetManagement.Application.Common;

public class PhotoService : IPhotoService
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly ICurrentUserService _currentUser;

    public PhotoService(IApplicationDbContext db, IFileStorage storage, ICurrentUserService currentUser)
    {
        _db = db;
        _storage = storage;
        _currentUser = currentUser;
    }

    public async Task<Result> AttachAsync(
        PhotoUpload? upload,
        PhotoKind kind,
        int? assetId = null,
        int? requestId = null,
        int? returnId = null,
        bool saveChanges = false,
        CancellationToken ct = default)
    {
        // Most steps treat the photo as optional, so "nothing uploaded" is fine.
        if (upload is null || upload.Length == 0) return Result.Success();

        if (!upload.IsImage)
            return Result.Failure("Only JPEG, PNG, WebP or HEIC images can be attached.");

        if (!upload.IsWithinSizeLimit)
            return Result.Failure($"That image is larger than the {PhotoUpload.MaxBytes / (1024 * 1024)} MB limit.");

        var stored = await _storage.SaveAsync(
            upload.Content, upload.FileName, upload.ContentType, kind.ToString().ToLowerInvariant(), ct);

        _db.AssetPhotos.Add(new AssetPhoto
        {
            Kind = kind,
            StoredPath = stored.RelativePath,
            OriginalFileName = stored.OriginalFileName,
            ContentType = stored.ContentType,
            SizeBytes = stored.SizeBytes,
            CapturedUtc = DateTime.UtcNow,
            CapturedBy = _currentUser.UserName,
            AssetId = assetId,
            AssetRequestId = requestId,
            AssetReturnId = returnId
        });

        if (saveChanges) await _db.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<IReadOnlyList<PhotoDto>> GetForAssetAsync(int assetId, CancellationToken ct = default)
        => Project(await _db.AssetPhotos.AsNoTracking()
            .Where(p => p.AssetId == assetId)
            .OrderByDescending(p => p.CapturedUtc).ToListAsync(ct));

    public async Task<IReadOnlyList<PhotoDto>> GetForReturnAsync(int returnId, CancellationToken ct = default)
        => Project(await _db.AssetPhotos.AsNoTracking()
            .Where(p => p.AssetReturnId == returnId)
            .OrderByDescending(p => p.CapturedUtc).ToListAsync(ct));

    private List<PhotoDto> Project(List<AssetPhoto> photos)
        => photos.Select(p => new PhotoDto(
            p.Id, p.Kind, _storage.GetPublicUrl(p.StoredPath),
            p.OriginalFileName, p.CapturedUtc, p.CapturedBy)).ToList();
}
