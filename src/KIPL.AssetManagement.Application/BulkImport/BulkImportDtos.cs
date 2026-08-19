namespace KIPL.AssetManagement.Application.BulkImport;

public record ImportRowResult(int RowNumber, bool Succeeded, string? Tag, string? Error);

public record BulkImportResult(
    int TotalRows, int Imported, int Skipped, IReadOnlyList<ImportRowResult> Rows)
{
    public bool HasErrors => Skipped > 0;
}
