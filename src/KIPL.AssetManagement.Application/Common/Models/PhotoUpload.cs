namespace KIPL.AssetManagement.Application.Common.Models;

/// <summary>
/// A file handed down from the web layer without dragging IFormFile into
/// Application. The stream is owned by the caller.
/// </summary>
public class PhotoUpload
{
    public PhotoUpload(Stream content, string fileName, string contentType, long length)
    {
        Content = content;
        FileName = fileName;
        ContentType = contentType;
        Length = length;
    }

    public Stream Content { get; }
    public string FileName { get; }
    public string ContentType { get; }
    public long Length { get; }

    public const long MaxBytes = 8 * 1024 * 1024;

    private static readonly string[] Allowed = { "image/jpeg", "image/png", "image/webp", "image/heic" };

    public bool IsImage => Allowed.Contains(ContentType, StringComparer.OrdinalIgnoreCase);
    public bool IsWithinSizeLimit => Length > 0 && Length <= MaxBytes;
}
