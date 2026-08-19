using System.Text;
using FluentAssertions;
using KIPL.AssetManagement.Application.Common;
using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Domain.Enums;
using Xunit;

namespace KIPL.AssetManagement.UnitTests;

public class PhotoServiceTests : IDisposable
{
    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly FakeFileStorage _storage = new();
    private readonly PhotoService _service;

    public PhotoServiceTests()
    {
        _service = new PhotoService(_db, _storage, new FakeCurrentUserService());
        _db.Assets.Add(new Asset { Id = 1, Tag = "LAP-0001", Brand = "Dell" });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    private static PhotoUpload Upload(string contentType = "image/jpeg", int bytes = 1024)
        => new(new MemoryStream(new byte[bytes]), "photo.jpg", contentType, bytes);

    [Fact]
    public async Task NoUpload_IsASuccessfulNoOp()
    {
        var result = await _service.AttachAsync(null, PhotoKind.Assignment, assetId: 1, saveChanges: true);

        result.Succeeded.Should().BeTrue();
        _db.AssetPhotos.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidImage_IsStoredAndRecorded()
    {
        var result = await _service.AttachAsync(Upload(), PhotoKind.Assignment, assetId: 1, saveChanges: true);

        result.Succeeded.Should().BeTrue();
        _storage.Saved.Should().ContainSingle();

        var photo = _db.AssetPhotos.Single();
        photo.Kind.Should().Be(PhotoKind.Assignment);
        photo.AssetId.Should().Be(1);
        photo.CapturedBy.Should().Be("Test User");
    }

    [Fact]
    public async Task NonImage_IsRejected()
    {
        var result = await _service.AttachAsync(
            Upload("application/pdf"), PhotoKind.Assignment, assetId: 1, saveChanges: true);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("JPEG");
        _db.AssetPhotos.Should().BeEmpty();
    }

    [Fact]
    public async Task OversizedImage_IsRejected()
    {
        var upload = new PhotoUpload(
            new MemoryStream(), "big.jpg", "image/jpeg", PhotoUpload.MaxBytes + 1);

        var result = await _service.AttachAsync(upload, PhotoKind.Dispatch, assetId: 1, saveChanges: true);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("larger than");
    }

    [Fact]
    public async Task WithoutSaveChanges_NothingIsPersistedYet()
    {
        // Services attach inside their own transaction and save once at the end.
        await _service.AttachAsync(Upload(), PhotoKind.Dispatch, assetId: 1);

        _db.AssetPhotos.Should().BeEmpty();

        await _db.SaveChangesAsync();
        _db.AssetPhotos.Should().ContainSingle();
    }

    [Fact]
    public async Task GetForAsset_ReturnsAPublicUrl()
    {
        await _service.AttachAsync(Upload(), PhotoKind.Assignment, assetId: 1, saveChanges: true);

        var photos = await _service.GetForAssetAsync(1);

        photos.Should().ContainSingle();
        photos[0].Url.Should().StartWith("/uploads/");
    }
}
