using KIPL.AssetManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KIPL.AssetManagement.Infrastructure.Services;

/// <summary>
/// Writes uploads under a configured root (Storage:UploadRoot, defaulting to
/// wwwroot/uploads). Files are foldered by year/month so a single directory
/// never grows unbounded, and renamed to a GUID so a hostile filename cannot
/// escape the root or collide with an existing file.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _root;
    private readonly string _publicPrefix;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(IConfiguration configuration, ILogger<LocalFileStorage> logger)
    {
        _logger = logger;
        _root = configuration["Storage:UploadRoot"]
            ?? Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads");
        _publicPrefix = configuration["Storage:PublicPrefix"] ?? "/uploads";

        Directory.CreateDirectory(_root);
    }

    public async Task<StoredFile> SaveAsync(
        Stream content, string originalFileName, string contentType, string folder, CancellationToken ct = default)
    {
        var safeFolder = string.Concat(folder.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
        var datedFolder = Path.Combine(safeFolder, DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));

        var absoluteFolder = Path.Combine(_root, datedFolder);
        Directory.CreateDirectory(absoluteFolder);

        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(extension) || extension.Length > 10) extension = ".jpg";
        extension = string.Concat(extension.Where(c => char.IsLetterOrDigit(c) || c == '.'));

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(absoluteFolder, fileName);

        await using (var target = File.Create(absolutePath))
        {
            await content.CopyToAsync(target, ct);
        }

        var relative = Path.Combine(datedFolder, fileName).Replace('\\', '/');
        _logger.LogInformation("Stored upload {Relative} ({Bytes} bytes)", relative, new FileInfo(absolutePath).Length);

        return new StoredFile(
            relative,
            Path.GetFileName(originalFileName),
            contentType,
            new FileInfo(absolutePath).Length);
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        var absolutePath = ResolveWithinRoot(relativePath);
        if (absolutePath is not null && File.Exists(absolutePath)) File.Delete(absolutePath);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string relativePath)
        => $"{_publicPrefix}/{relativePath.Replace('\\', '/').TrimStart('/')}";

    /// <summary>Resolves a stored path and refuses anything that climbs outside the root.</summary>
    private string? ResolveWithinRoot(string relativePath)
    {
        var candidate = Path.GetFullPath(Path.Combine(_root, relativePath));
        var root = Path.GetFullPath(_root);

        return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? candidate : null;
    }
}
