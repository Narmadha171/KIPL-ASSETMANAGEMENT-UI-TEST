using System.Globalization;
using System.Text;
using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KIPL.AssetManagement.Application.BulkImport;

public class BulkImportService : IBulkImportService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ITagGenerator _tags;

    public BulkImportService(IApplicationDbContext db, IAuditService audit, ITagGenerator tags)
    {
        _db = db;
        _audit = audit;
        _tags = tags;
    }

    public string TemplateHeader => "Tag,Category,Brand,Model,SerialNumber,Condition,PurchaseDate,PurchaseCost,WarrantyExpiry,Location,Notes";

    public byte[] GetTemplate()
    {
        var sb = new StringBuilder();
        sb.AppendLine(TemplateHeader);
        sb.AppendLine("LAP-EXAMPLE1,Laptop,Dell Latitude 5440,5440,SN-123456,New,2026-01-15,1250.00,2029-01-15,Head office,Sample row — delete before importing");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<BulkImportResult> ImportAsync(Stream csvStream, bool dryRun, CancellationToken ct = default)
    {
        var rows = new List<ImportRowResult>();
        var toAdd = new List<Asset>();

        var existingTags = (await _db.Assets.AsNoTracking().Select(a => a.Tag).ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        var headerLine = await reader.ReadLineAsync(ct);
        if (headerLine is null)
            return new BulkImportResult(0, 0, 0, rows);

        var headers = SplitCsvLine(headerLine).Select(h => h.Trim()).ToList();
        int Index(string name) => headers.FindIndex(h => string.Equals(h, name, StringComparison.OrdinalIgnoreCase));

        var iTag = Index("Tag");
        var iCategory = Index("Category");
        var iBrand = Index("Brand");
        var iModel = Index("Model");
        var iSerial = Index("SerialNumber");
        var iCondition = Index("Condition");
        var iPurchase = Index("PurchaseDate");
        var iCost = Index("PurchaseCost");
        var iWarranty = Index("WarrantyExpiry");
        var iLocation = Index("Location");
        var iNotes = Index("Notes");

        if (iBrand < 0)
            return new BulkImportResult(0, 0, 1, new[] { new ImportRowResult(1, false, null, "CSV is missing the required 'Brand' column.") });

        var rowNumber = 1;
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            rowNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cells = SplitCsvLine(line);
            string? Cell(int i) => i >= 0 && i < cells.Count ? cells[i].Trim() : null;

            var brand = Cell(iBrand);
            if (string.IsNullOrWhiteSpace(brand))
            {
                rows.Add(new ImportRowResult(rowNumber, false, null, "Brand is required."));
                continue;
            }

            var categoryText = Cell(iCategory);
            if (!TryParseEnum<AssetCategory>(categoryText, out var category))
            {
                rows.Add(new ImportRowResult(rowNumber, false, null, $"Unknown category '{categoryText}'."));
                continue;
            }

            var conditionText = Cell(iCondition);
            if (!string.IsNullOrWhiteSpace(conditionText) && !TryParseEnum<AssetCondition>(conditionText, out _))
            {
                rows.Add(new ImportRowResult(rowNumber, false, null, $"Unknown condition '{conditionText}'."));
                continue;
            }
            TryParseEnum<AssetCondition>(conditionText, out var condition);

            var tag = Cell(iTag);
            tag = string.IsNullOrWhiteSpace(tag) ? _tags.NewAssetTag(category) : tag.ToUpperInvariant();

            if (!existingTags.Add(tag))
            {
                rows.Add(new ImportRowResult(rowNumber, false, tag, $"Duplicate asset tag '{tag}'."));
                continue;
            }

            var asset = new Asset
            {
                Tag = tag,
                Category = category,
                Brand = brand,
                Model = Cell(iModel),
                SerialNumber = Cell(iSerial),
                Condition = condition,
                Status = AssetStatus.InStock,
                PurchaseDate = ParseDate(Cell(iPurchase)),
                PurchaseCost = ParseDecimal(Cell(iCost)),
                WarrantyExpiry = ParseDate(Cell(iWarranty)),
                Location = Cell(iLocation),
                Notes = Cell(iNotes)
            };

            toAdd.Add(asset);
            rows.Add(new ImportRowResult(rowNumber, true, tag, null));
        }

        var imported = rows.Count(r => r.Succeeded);
        var skipped = rows.Count(r => !r.Succeeded);

        if (!dryRun && toAdd.Count > 0)
        {
            _db.Assets.AddRange(toAdd);
            await _audit.LogAsync("Bulk import", $"{toAdd.Count} assets", $"{skipped} row(s) skipped", ct);
            await _db.SaveChangesAsync(ct);
        }

        return new BulkImportResult(rows.Count, imported, skipped, rows);
    }

    private static bool TryParseEnum<TEnum>(string? text, out TEnum value) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = default;
            return true;
        }

        var normalised = text.Replace(" ", string.Empty);
        return Enum.TryParse(normalised, ignoreCase: true, out value);
    }

    private static DateTime? ParseDate(string? text)
        => DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var d) ? d : null;

    private static decimal? ParseDecimal(string? text)
        => decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;

    /// <summary>Minimal RFC-4180 style splitter — handles quoted cells containing commas.</summary>
    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                    else inQuotes = false;
                }
                else current.Append(c);
            }
            else if (c == '"') inQuotes = true;
            else if (c == ',') { result.Add(current.ToString()); current.Clear(); }
            else current.Append(c);
        }

        result.Add(current.ToString());
        return result;
    }
}
