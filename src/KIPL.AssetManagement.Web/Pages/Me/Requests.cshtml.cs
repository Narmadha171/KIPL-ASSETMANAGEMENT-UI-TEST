using System.ComponentModel.DataAnnotations;
using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Application.Requests;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace KIPL.AssetManagement.Web.Pages.Me;

public class RequestsModel : EmployeePageModel
{
    private readonly IAssetRequestService _requests;

    public RequestsModel(IAssetRequestService requests, ICurrentUserService currentUser)
        : base(currentUser) => _requests = requests;

    [BindProperty] public InputModel Input { get; set; } = new();

    public IReadOnlyList<RequestListItemDto> Requests { get; private set; } = Array.Empty<RequestListItemDto>();

    public class InputModel
    {
        [Required, StringLength(200), Display(Name = "What do you need?")]
        public string ItemName { get; set; } = string.Empty;

        [Display(Name = "Category")]
        public AssetCategory Category { get; set; } = AssetCategory.Laptop;

        [Required, StringLength(1000), Display(Name = "Why do you need it?")]
        public string Reason { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (EmployeeId is not int me) return RedirectToPage("/Account/AccessDenied");

        await LoadAsync(me, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostRaiseAsync(CancellationToken ct)
    {
        if (EmployeeId is not int me) return Forbid();

        if (!ModelState.IsValid)
        {
            await LoadAsync(me, ct);
            return Page();
        }

        var command = new RaiseRequestCommand
        {
            RequesterId = me,
            ItemName = Input.ItemName,
            Category = Input.Category,
            Reason = Input.Reason
        };

        var result = await _requests.RaiseAsync(command, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await LoadAsync(me, ct);
            return Page();
        }

        TempData["Success"] = "Request submitted — your manager will review it.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostConfirmAsync(int requestId, IFormFile? photo, CancellationToken ct)
    {
        if (EmployeeId is not int me) return Forbid();

        await using var stream = photo?.OpenReadStream();
        var command = new ConfirmReceiptCommand
        {
            RequestId = requestId,
            EmployeeId = me,
            Photo = stream is null || photo is null
                ? null
                : new PhotoUpload(stream, photo.FileName, photo.ContentType, photo.Length)
        };

        Finish(await _requests.ConfirmReceiptAsync(command, ct), "Receipt confirmed — the asset is now yours.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelAsync(int requestId, CancellationToken ct)
    {
        if (EmployeeId is not int me) return Forbid();

        Finish(await _requests.CancelAsync(requestId, me, ct), "Request cancelled.");
        return RedirectToPage();
    }

    private async Task LoadAsync(int employeeId, CancellationToken ct)
    {
        Requests = await _requests.GetForEmployeeAsync(employeeId, ct);

        ViewData["NavBadges"] = new Dictionary<string, int>
        {
            ["mine"] = Requests.Count(r => r.Status == RequestStatus.Dispatched)
        };
    }
}
