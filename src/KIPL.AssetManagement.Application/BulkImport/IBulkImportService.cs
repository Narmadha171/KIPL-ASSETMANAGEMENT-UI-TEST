namespace KIPL.AssetManagement.Application.BulkImport;

public interface IBulkImportService
{
    /// <summary>Expected CSV header, also served as the downloadable template.</summary>
    string TemplateHeader { get; }
    byte[] GetTemplate();

    /// <summary>Parses and imports a CSV stream. Valid rows are saved; invalid rows are reported.</summary>
    Task<BulkImportResult> ImportAsync(Stream csvStream, bool dryRun, CancellationToken ct = default);
}
