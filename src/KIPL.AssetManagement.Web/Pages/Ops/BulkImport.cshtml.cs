using KIPL.AssetManagement.Application.BulkImport;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KIPL.AssetManagement.Web.Pages.Ops;

[Authorize(Policy = Permissions.BulkImport)]
public class BulkImportModel : PageModel
{
    private const long MaxUploadBytes = 5 * 1024 * 1024;

    private readonly IBulkImportService _import;

    public BulkImportModel(IBulkImportService import) => _import = import;

    [BindProperty] public IFormFile? Upload { get; set; }

    /// <summary>When true the file is parsed and reported on, but nothing is written.</summary>
    [BindProperty] public bool DryRun { get; set; } = true;

    public BulkImportResult? Result { get; private set; }
    public string TemplateHeader => _import.TemplateHeader;

    public void OnGet() { }

    public IActionResult OnGetTemplate()
        => File(_import.GetTemplate(), "text/csv", "kipl-asset-import-template.csv");

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (Upload is null || Upload.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Choose a CSV file to import.");
            return Page();
        }

        if (Upload.Length > MaxUploadBytes)
        {
            ModelState.AddModelError(string.Empty, "That file is larger than the 5 MB limit.");
            return Page();
        }

        if (!Upload.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Only .csv files can be imported.");
            return Page();
        }

        await using var stream = Upload.OpenReadStream();
        Result = await _import.ImportAsync(stream, DryRun, ct);

        if (!DryRun && Result.Imported > 0)
            TempData["Success"] = $"{Result.Imported} asset(s) imported.";

        return Page();
    }
}
