namespace KIPL.AssetManagement.Application.Common.Interfaces;

public record StoredFile(string RelativePath, string OriginalFileName, string ContentType, long SizeBytes);

/// <summary>
/// Saves uploaded files somewhere durable. Implemented over the local disk;
/// swapping in blob storage means replacing only this.
/// </summary>
public interface IFileStorage
{
    /// <summary>Persists a stream and returns the details to record against the entity.</summary>
    Task<StoredFile> SaveAsync(
        Stream content, string originalFileName, string contentType, string folder, CancellationToken ct = default);

    Task DeleteAsync(string relativePath, CancellationToken ct = default);

    /// <summary>A URL the browser can fetch the stored file from.</summary>
    string GetPublicUrl(string relativePath);
}
