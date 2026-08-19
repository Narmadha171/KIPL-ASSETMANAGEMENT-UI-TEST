using System.Text;
using FluentAssertions;
using KIPL.AssetManagement.Application.BulkImport;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Domain.Enums;
using Xunit;

namespace KIPL.AssetManagement.UnitTests;

public class BulkImportServiceTests : IDisposable
{
    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly BulkImportService _service;

    public BulkImportServiceTests()
    {
        _service = new BulkImportService(_db, new FakeAuditService(_db), new FakeTagGenerator());
        _db.Assets.Add(new Asset { Id = 1, Tag = "LAP-EXISTING", Brand = "Dell" });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    private Task<BulkImportResult> ImportAsync(string csv, bool dryRun = false)
        => _service.ImportAsync(new MemoryStream(Encoding.UTF8.GetBytes(csv)), dryRun);

    [Fact]
    public async Task ValidRows_AreImported()
    {
        var csv = """
                  Tag,Category,Brand,Model,SerialNumber,Condition,PurchaseDate,PurchaseCost,WarrantyExpiry,Location,Notes
                  LAP-1000,Laptop,Dell Latitude,5440,SN1,New,2026-01-15,1250.00,2029-01-15,HQ,
                  MON-1000,Monitor,LG UltraFine,27UL850,SN2,Good,,,,HQ,
                  """;

        var result = await ImportAsync(csv);

        result.Imported.Should().Be(2);
        result.Skipped.Should().Be(0);
        _db.Assets.Count().Should().Be(3);
        _db.Assets.Should().OnlyContain(a => a.Status == AssetStatus.InStock || a.Tag == "LAP-EXISTING");
    }

    [Fact]
    public async Task DryRun_ReportsWithoutWriting()
    {
        var csv = "Tag,Category,Brand\nLAP-2000,Laptop,HP EliteBook";

        var result = await ImportAsync(csv, dryRun: true);

        result.Imported.Should().Be(1);
        _db.Assets.Count().Should().Be(1);
    }

    [Fact]
    public async Task DuplicateTag_IsSkippedWithAnExplanation()
    {
        var csv = "Tag,Category,Brand\nLAP-EXISTING,Laptop,Dell";

        var result = await ImportAsync(csv);

        result.Skipped.Should().Be(1);
        result.Rows[0].Error.Should().Contain("Duplicate");
    }

    [Fact]
    public async Task MissingBrand_IsSkipped()
    {
        var csv = "Tag,Category,Brand\nLAP-3000,Laptop,";

        var result = await ImportAsync(csv);

        result.Skipped.Should().Be(1);
        result.Rows[0].Error.Should().Contain("Brand is required");
    }

    [Fact]
    public async Task UnknownCategory_IsSkipped()
    {
        var csv = "Tag,Category,Brand\nLAP-4000,Spaceship,Dell";

        var result = await ImportAsync(csv);

        result.Skipped.Should().Be(1);
        result.Rows[0].Error.Should().Contain("Unknown category");
    }

    [Fact]
    public async Task BlankTag_GetsAGeneratedOne()
    {
        var csv = "Tag,Category,Brand\n,Laptop,Lenovo ThinkPad";

        var result = await ImportAsync(csv);

        result.Imported.Should().Be(1);
        _db.Assets.Should().Contain(a => a.Tag.StartsWith("TST-"));
    }

    [Fact]
    public async Task QuotedFieldsContainingCommas_AreParsedCorrectly()
    {
        var csv = "Tag,Category,Brand,Notes\nLAP-5000,Laptop,\"Dell, Inc.\",\"Bought in bulk, 12 units\"";

        var result = await ImportAsync(csv);

        result.Imported.Should().Be(1);
        var asset = _db.Assets.Single(a => a.Tag == "LAP-5000");
        asset.Brand.Should().Be("Dell, Inc.");
        asset.Notes.Should().Be("Bought in bulk, 12 units");
    }

    [Fact]
    public async Task MissingBrandColumn_FailsFast()
    {
        var csv = "Tag,Category\nLAP-6000,Laptop";

        var result = await ImportAsync(csv);

        result.Imported.Should().Be(0);
        result.Rows[0].Error.Should().Contain("Brand");
    }

    [Fact]
    public async Task ValidAndInvalidRows_ArePartiallyImported()
    {
        var csv = """
                  Tag,Category,Brand
                  LAP-7000,Laptop,Dell
                  LAP-EXISTING,Laptop,Dell
                  MON-7000,Monitor,LG
                  """;

        var result = await ImportAsync(csv);

        result.Imported.Should().Be(2);
        result.Skipped.Should().Be(1);
        result.HasErrors.Should().BeTrue();
    }
}
