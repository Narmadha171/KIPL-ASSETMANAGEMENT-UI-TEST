using KIPL.AssetManagement.Application.Audit;
using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Dashboard;
using KIPL.AssetManagement.Application.Inventory;
using KIPL.AssetManagement.Application.Requests;
using KIPL.AssetManagement.Application.Returns;
using KIPL.AssetManagement.Application.ServiceRequests;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KIPL.AssetManagement.Web.Pages.Ops;

/// <summary>
/// The operations overview. A single panel whose tab decides which working list
/// is shown, and whose column headers sort it — mirroring the prototype.
/// </summary>
public class DashboardModel : PageModel
{
    private readonly IDashboardService _dashboard;
    private readonly IAssetRequestService _requests;
    private readonly IReturnService _returns;
    private readonly IServiceRequestService _service;
    private readonly IAuditQueryService _audit;
    private readonly ICurrentUserService _currentUser;

    public DashboardModel(
        IDashboardService dashboard,
        IAssetRequestService requests,
        IReturnService returns,
        IServiceRequestService service,
        IAuditQueryService audit,
        ICurrentUserService currentUser)
    {
        _dashboard = dashboard;
        _requests = requests;
        _returns = returns;
        _service = service;
        _audit = audit;
        _currentUser = currentUser;
    }

    public enum DashTab { Requests, Returns, Service, Activity }

    [BindProperty(SupportsGet = true)] public DashTab Tab { get; set; } = DashTab.Requests;
    [BindProperty(SupportsGet = true)] public string? Sort { get; set; }
    [BindProperty(SupportsGet = true)] public bool Desc { get; set; }

    public OperationsDashboardDto Data { get; private set; } = null!;
    public IReadOnlyList<RequestListItemDto> RequestRows { get; private set; } = Array.Empty<RequestListItemDto>();
    public IReadOnlyList<ReturnListItemDto> ReturnRows { get; private set; } = Array.Empty<ReturnListItemDto>();
    public IReadOnlyList<ServiceRequestDto> ServiceRows { get; private set; } = Array.Empty<ServiceRequestDto>();
    public IReadOnlyList<AuditEntryDto> ActivityRows { get; private set; } = Array.Empty<AuditEntryDto>();

    public async Task OnGetAsync(CancellationToken ct)
    {
        Data = await _dashboard.GetOperationsAsync(ct);

        switch (Tab)
        {
            case DashTab.Returns:
                ReturnRows = Sorted(await _returns.GetAllAsync(null, ct));
                break;
            case DashTab.Service:
                ServiceRows = Sorted(await _service.GetAllAsync(null, ct));
                break;
            case DashTab.Activity:
                ActivityRows = await _audit.GetRecentAsync(40, ct);
                break;
            default:
                var open = await _requests.SearchAsync(new RequestFilter { OnlyOpen = true, PageSize = 100 }, ct);
                RequestRows = Sorted(open.Items);
                break;
        }

        var badges = new Dictionary<string, int>
        {
            ["approvals"] = Data.Requests.Pending + Data.Requests.Unclaimed
        };

        if (_currentUser.EmployeeId is int me)
        {
            var mine = await _requests.GetForEmployeeAsync(me, ct);
            badges["mine"] = mine.Count(r => r.Status == RequestStatus.Dispatched);
        }

        ViewData["NavBadges"] = badges;
    }

    /// <summary>Applies the current sort column to whichever list the tab is showing.</summary>
    private IReadOnlyList<T> Sorted<T>(IReadOnlyList<T> rows)
    {
        if (string.IsNullOrWhiteSpace(Sort)) return rows;

        Func<T, object?> key = Sort.ToLowerInvariant() switch
        {
            "requester" => r => (r as RequestListItemDto)?.RequesterName ?? (r as ReturnListItemDto)?.EmployeeName,
            "item" => r => (r as RequestListItemDto)?.ItemName ?? (r as ReturnListItemDto)?.AssetName,
            "status" => r => (r as RequestListItemDto)?.Status.ToString()
                          ?? (r as ReturnListItemDto)?.Stage.ToString()
                          ?? (r as ServiceRequestDto)?.Status.ToString(),
            "raised" => r => (r as RequestListItemDto)?.RaisedUtc
                          ?? (r as ReturnListItemDto)?.RaisedUtc
                          ?? (r as ServiceRequestDto)?.RaisedUtc,
            "urgency" => r => (r as ServiceRequestDto)?.Urgency,
            "asset" => r => (r as ReturnListItemDto)?.AssetName ?? (r as ServiceRequestDto)?.AssetName,
            "reference" => r => (r as ReturnListItemDto)?.Reference ?? (r as ServiceRequestDto)?.Reference,
            _ => _ => null
        };

        return Desc
            ? rows.OrderByDescending(key).ToList()
            : rows.OrderBy(key).ToList();
    }

    /// <summary>Builds the link for a sortable column header, flipping direction on repeat clicks.</summary>
    public string SortLink(string column)
        => $"?Tab={Tab}&Sort={column}&Desc={(string.Equals(Sort, column, StringComparison.OrdinalIgnoreCase) && !Desc).ToString().ToLowerInvariant()}";

    public string SortArrow(string column)
        => string.Equals(Sort, column, StringComparison.OrdinalIgnoreCase) ? (Desc ? "▼" : "▲") : "";
}
