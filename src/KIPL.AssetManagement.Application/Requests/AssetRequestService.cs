using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Application.Common.Models;
using KIPL.AssetManagement.Domain.Entities;
using KIPL.AssetManagement.Domain.Enums;
using KIPL.AssetManagement.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace KIPL.AssetManagement.Application.Requests;

public class AssetRequestService : IAssetRequestService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly IPhotoService _photos;
    private readonly ICurrentUserService _currentUser;

    public AssetRequestService(
        IApplicationDbContext db,
        IAuditService audit,
        IPhotoService photos,
        ICurrentUserService currentUser)
    {
        _db = db;
        _audit = audit;
        _photos = photos;
        _currentUser = currentUser;
    }

    private IQueryable<AssetRequest> BaseQuery() => _db.AssetRequests
        .AsNoTracking()
        .Include(r => r.Requester).ThenInclude(e => e!.Department)
        .Include(r => r.ApprovedBy)
        .Include(r => r.FulfilledWithAsset);

    private static RequestListItemDto Map(AssetRequest r) => new(
        r.Id,
        r.Requester?.FullName ?? "—",
        r.Requester?.Department?.Name ?? "—",
        r.ItemName,
        r.Category,
        r.Reason,
        r.Status,
        r.Source,
        r.Mode,
        r.RaisedUtc,
        r.FulfilledWithAsset?.Tag,
        r.OrderId,
        r.CourierTrackingNumber,
        r.OfficePickupLocation,
        r.ApprovedBy?.FullName,
        r.RejectionReason);

    // ------------------------------------------------------------ queries

    public async Task<PaginatedList<RequestListItemDto>> SearchAsync(RequestFilter filter, CancellationToken ct = default)
    {
        var query = BaseQuery();

        if (filter.Status.HasValue) query = query.Where(r => r.Status == filter.Status);
        if (filter.RequesterId.HasValue) query = query.Where(r => r.RequesterId == filter.RequesterId);
        if (filter.ManagerId.HasValue) query = query.Where(r => r.Requester.ManagerId == filter.ManagerId);
        if (filter.OnlyOpen)
            query = query.Where(r => r.Status != RequestStatus.Completed
                                  && r.Status != RequestStatus.Rejected
                                  && r.Status != RequestStatus.Cancelled);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(r => r.ItemName.Contains(term) || r.Requester.FullName.Contains(term));
        }

        var ordered = query.OrderByDescending(r => r.RaisedUtc);
        return await PaginatedList<RequestListItemDto>.CreateAsync(
            ordered, Map, filter.PageNumber, filter.PageSize, ct);
    }

    public async Task<IReadOnlyList<RequestListItemDto>> GetForEmployeeAsync(int employeeId, CancellationToken ct = default)
        => (await BaseQuery().Where(r => r.RequesterId == employeeId)
                .OrderByDescending(r => r.RaisedUtc).ToListAsync(ct))
            .Select(Map).ToList();

    public async Task<IReadOnlyList<RequestListItemDto>> GetPendingForManagerAsync(int managerEmployeeId, CancellationToken ct = default)
        => (await BaseQuery()
                .Where(r => r.Status == RequestStatus.Pending && r.Requester.ManagerId == managerEmployeeId)
                .OrderBy(r => r.RaisedUtc).ToListAsync(ct))
            .Select(Map).ToList();

    /// <summary>
    /// HR's queue: requests already signed off by the requester's manager
    /// (<see cref="RequestStatus.PendingHrApproval"/>), plus fresh requests that have
    /// nobody who can ever escalate them — either the requester has no manager at all,
    /// or their manager isn't literally a Reporting Manager (e.g. the "manager" on file
    /// is an Admin/HR/IT record, or a Reporting Manager who raised a request for
    /// themselves). Only a Reporting Manager has a Team Approvals queue that anyone ever
    /// looks at, so this deliberately checks the manager's role name directly rather than
    /// a togglable permission grant — that keeps it working even if someone edits
    /// Roles & Permissions in a way that would otherwise strand these requests.
    /// </summary>
    public async Task<IReadOnlyList<RequestListItemDto>> GetPendingAsync(string? department, CancellationToken ct = default)
    {
        var query = BaseQuery().Where(r =>
            r.Status == RequestStatus.PendingHrApproval ||
            (r.Status == RequestStatus.Pending &&
                (r.Requester.ManagerId == null ||
                 r.Requester.Manager!.RoleName != Roles.ReportingManager)));

        if (!string.IsNullOrWhiteSpace(department) && department != "All departments")
            query = query.Where(r => r.Requester.Department != null && r.Requester.Department.Name == department);

        return (await query.OrderBy(r => r.RaisedUtc).ToListAsync(ct)).Select(Map).ToList();
    }

    /// <summary>Online orders that have arrived and are waiting on HR to check them in.</summary>
    public async Task<IReadOnlyList<RequestListItemDto>> GetAwaitingVerificationAsync(CancellationToken ct = default)
        => (await BaseQuery()
                .Where(r => r.Status == RequestStatus.OnlineOrdered && !r.OnlineOrderVerified)
                .OrderBy(r => r.RaisedUtc).ToListAsync(ct))
            .Select(Map).ToList();

    public async Task<RequestListItemDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var entity = await BaseQuery().FirstOrDefaultAsync(r => r.Id == id, ct);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<string>> GetDepartmentsAsync(CancellationToken ct = default)
        => await _db.Departments.AsNoTracking().OrderBy(d => d.Name).Select(d => d.Name).ToListAsync(ct);

    public async Task<RequestCountsDto> GetCountsAsync(CancellationToken ct = default)
    {
        var grouped = await _db.AssetRequests.AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int At(RequestStatus s) => grouped.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var completedThisMonth = await _db.AssetRequests.AsNoTracking()
            .CountAsync(r => r.CompletedUtc != null && r.CompletedUtc >= monthStart, ct);

        return new RequestCountsDto(
            At(RequestStatus.Pending) + At(RequestStatus.PendingHrApproval),
            At(RequestStatus.Unclaimed),
            At(RequestStatus.Claimed),
            At(RequestStatus.OnlineOrdered),
            At(RequestStatus.Dispatched),
            completedThisMonth);
    }

    // ----------------------------------------------------------- commands

    public async Task<Result<int>> RaiseAsync(RaiseRequestCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.ItemName)) return Result<int>.Failure("Please describe the item you need.");
        if (string.IsNullOrWhiteSpace(command.Reason)) return Result<int>.Failure("A business reason is required.");

        var requester = await _db.Employees.FirstOrDefaultAsync(e => e.Id == command.RequesterId, ct);
        if (requester is null) return Result<int>.Failure("Requester not found.");

        var request = new AssetRequest
        {
            RequesterId = requester.Id,
            ItemName = command.ItemName.Trim(),
            Category = command.Category,
            Reason = command.Reason.Trim(),
            Status = RequestStatus.Pending,
            RaisedUtc = DateTime.UtcNow
        };

        _db.AssetRequests.Add(request);
        await _audit.LogAsync("Raised request", command.ItemName.Trim(), ct: ct);
        await _db.SaveChangesAsync(ct);
        return Result<int>.Success(request.Id);
    }

    /// <summary>A reporting manager's sign-off — sends the request on to HR rather than
    /// releasing it straight to IT. HR (or Admin) approves directly via <see cref="ApproveAsync"/>.</summary>
    public async Task<Result> EscalateToHrAsync(int requestId, int managerEmployeeId, CancellationToken ct = default)
        => await MutateAsync(requestId, async request =>
        {
            var manager = await _db.Employees.FirstOrDefaultAsync(e => e.Id == managerEmployeeId, ct);
            if (manager is null) return Result.Failure("Manager not found.");

            if (request.RequesterId == managerEmployeeId)
                return Result.Failure("You cannot approve your own request — an administrator must action it.");

            request.EscalateToHr(manager);
            await _audit.LogAsync("Approved by manager — sent to HR", request.ItemName, ct: ct);
            return Result.Success();
        }, ct);

    public async Task<Result> ApproveAsync(int requestId, int approverEmployeeId, CancellationToken ct = default)
        => await MutateAsync(requestId, async request =>
        {
            var approver = await _db.Employees.FirstOrDefaultAsync(e => e.Id == approverEmployeeId, ct);
            if (approver is null) return Result.Failure("Approver not found.");

            if (request.RequesterId == approverEmployeeId)
                return Result.Failure("You cannot approve your own request — an administrator must action it.");

            request.Approve(approver);
            await _audit.LogAsync("Approved request", request.ItemName, ct: ct);
            return Result.Success();
        }, ct);

    public async Task<Result> RejectAsync(int requestId, int approverEmployeeId, string reason, CancellationToken ct = default)
        => await MutateAsync(requestId, async request =>
        {
            if (string.IsNullOrWhiteSpace(reason)) return Result.Failure("A rejection reason is required.");

            var approver = await _db.Employees.FirstOrDefaultAsync(e => e.Id == approverEmployeeId, ct);
            if (approver is null) return Result.Failure("Approver not found.");

            request.Reject(approver, reason.Trim());
            await _audit.LogAsync("Rejected request", request.ItemName, reason.Trim(), ct);
            return Result.Success();
        }, ct);

    public async Task<Result> ClaimAsync(int requestId, int operationsEmployeeId, CancellationToken ct = default)
        => await MutateAsync(requestId, async request =>
        {
            var user = await _db.Employees.FirstOrDefaultAsync(e => e.Id == operationsEmployeeId, ct);
            if (user is null) return Result.Failure("Operations user not found.");

            request.Claim(user);
            await _audit.LogAsync("Claimed request", request.ItemName, ct: ct);
            return Result.Success();
        }, ct);

    public async Task<Result<ChallanDto>> FulfilAsync(FulfilRequestCommand command, CancellationToken ct = default)
    {
        if (command.AssetIds.Count == 0)
            return Result<ChallanDto>.Failure("Select at least one asset to dispatch.");

        var request = await _db.AssetRequests
            .Include(r => r.Requester).ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct);
        if (request is null) return Result<ChallanDto>.Failure("Request not found.");

        var assets = await _db.Assets.Where(a => command.AssetIds.Contains(a.Id)).ToListAsync(ct);
        if (assets.Count != command.AssetIds.Count)
            return Result<ChallanDto>.Failure("One or more of the selected assets no longer exists.");

        var unavailable = assets.Where(a => !a.IsAssignable).ToList();
        if (unavailable.Count > 0)
            return Result<ChallanDto>.Failure(
                $"Not available for assignment: {string.Join(", ", unavailable.Select(a => a.Tag))}.");

        var detail = command.Mode == FulfilmentMode.Courier
            ? command.TrackingNumber
            : command.PickupPoint ?? command.PickupLocation;

        try
        {
            // Someone with fulfilment rights acting on a still-pending request
            // (e.g. HR assigning directly) implicitly approves it first.
            await EnsureApprovedAsync(request, ct);

            // The request records the first asset; every asset in the batch gets
            // its own custody row and travels on the same challan.
            request.FulfilFromStock(assets[0], command.Mode, detail);
        }
        catch (DomainException ex)
        {
            return Result<ChallanDto>.Failure(ex.Message);
        }

        foreach (var asset in assets)
        {
            asset.MarkInTransit();
            _db.AssetAssignments.Add(new AssetAssignment
            {
                AssetId = asset.Id,
                EmployeeId = request.RequesterId,
                AssignedUtc = DateTime.UtcNow,
                AssignedBy = _currentUser.UserName,
                Notes = $"Fulfilling request #{request.Id}"
            });
        }

        var photoResult = await _photos.AttachAsync(
            command.Photo, PhotoKind.Dispatch, assets[0].Id, request.Id, ct: ct);
        if (!photoResult.Succeeded) return Result<ChallanDto>.Failure(photoResult.Error!);

        await _audit.LogAsync(
            "Dispatched asset",
            string.Join(", ", assets.Select(a => a.Tag)),
            $"to {request.Requester.FullName}", ct);

        await _db.SaveChangesAsync(ct);

        return Result<ChallanDto>.Success(BuildChallan(request, assets, command.Mode,
            command.CourierService, command.TrackingNumber, command.PickupPoint ?? command.PickupLocation));
    }

    public async Task<Result<ChallanDto>> GetChallanAsync(int requestId, CancellationToken ct = default)
    {
        var request = await _db.AssetRequests.AsNoTracking()
            .Include(r => r.Requester).ThenInclude(e => e.Department)
            .Include(r => r.FulfilledWithAsset)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (request is null) return Result<ChallanDto>.Failure("Request not found.");
        if (request.FulfilledWithAsset is null)
            return Result<ChallanDto>.Failure("No challan exists — this request has not been dispatched from stock.");

        var assets = new List<Asset> { request.FulfilledWithAsset };

        return Result<ChallanDto>.Success(BuildChallan(
            request, assets, request.Mode ?? FulfilmentMode.Office,
            null, request.CourierTrackingNumber, request.OfficePickupLocation));
    }

    private ChallanDto BuildChallan(
        AssetRequest request,
        IReadOnlyList<Asset> assets,
        FulfilmentMode mode,
        string? courierService,
        string? tracking,
        string? pickupPoint)
        => new(
            $"KIPL/DC/{DateTime.UtcNow:yyyy}/{request.Id:D4}",
            DateTime.UtcNow,
            _currentUser.UserName ?? "IT Department",
            _currentUser.RoleName,
            request.Requester.FullName,
            $"Department: {request.Requester.Department?.Name ?? "—"} · {request.Requester.Email}",
            mode,
            courierService,
            tracking,
            pickupPoint ?? "IT Desk, 2nd Floor, KIPL Office",
            assets.Select(a => new ChallanLineDto(
                a.Brand, a.Category.ToString(), a.Tag, a.Condition.ToString())).ToList());

    public async Task<Result> PlaceOnlineOrderAsync(OnlineOrderCommand command, CancellationToken ct = default)
        => await MutateAsync(command.RequestId, async request =>
        {
            if (string.IsNullOrWhiteSpace(command.OrderId)) return Result.Failure("An order reference is required.");

            // Placing an order on a still-pending request implicitly approves it.
            await EnsureApprovedAsync(request, ct);

            request.PlaceOnlineOrder(
                command.OrderId.Trim(),
                command.Vendor?.Trim() ?? string.Empty,
                command.Brand?.Trim() ?? string.Empty,
                command.Model?.Trim());

            await _audit.LogAsync("Logged order", command.OrderId.Trim(), ct: ct);
            return Result.Success();
        }, ct);

    public async Task<Result> VerifyOnlineDeliveryAsync(VerifyOnlineOrderCommand command, CancellationToken ct = default)
    {
        var request = await _db.AssetRequests
            .Include(r => r.Requester)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct);
        if (request is null) return Result.Failure("Request not found.");

        if (string.IsNullOrWhiteSpace(command.SerialNumber))
            return Result.Failure("Record the manufacturer serial number before verifying.");

        // A mismatch is not a hard failure — it is logged and escalated, which is
        // what the prototype's warning path does.
        if (command.TagMismatch && string.IsNullOrWhiteSpace(command.MismatchNote))
            return Result.Failure("Describe the mismatch so IT can follow it up.");

        try
        {
            request.VerifyOnlineDelivery();
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message);
        }

        // The delivered item becomes a real inventory asset.
        var asset = new Asset
        {
            Tag = string.IsNullOrWhiteSpace(command.AssetTag)
                ? $"AST-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}"
                : command.AssetTag.Trim().ToUpperInvariant(),
            Category = request.Category,
            Brand = string.IsNullOrWhiteSpace(request.OrderBrand) ? request.ItemName : request.OrderBrand!,
            Model = request.OrderModel,
            SerialNumber = command.SerialNumber.Trim(),
            Condition = AssetCondition.New,
            Status = AssetStatus.InStock,
            Notes = command.TagMismatch ? $"Tag mismatch on delivery: {command.MismatchNote}" : null
        };

        _db.Assets.Add(asset);
        await _db.SaveChangesAsync(ct);

        request.FulfilledWithAssetId = asset.Id;

        var photoResult = await _photos.AttachAsync(
            command.Photo, PhotoKind.OnlineOrderVerification, asset.Id, request.Id, ct: ct);
        if (!photoResult.Succeeded) return Result.Failure(photoResult.Error!);

        await _audit.LogAsync(
            "Verified online order",
            request.OrderId ?? request.ItemName,
            command.TagMismatch ? $"Tag mismatch — {command.MismatchNote}" : $"Serial {command.SerialNumber.Trim()}",
            ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ConfirmReceiptAsync(ConfirmReceiptCommand command, CancellationToken ct = default)
    {
        var request = await _db.AssetRequests
            .Include(r => r.FulfilledWithAsset)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct);

        if (request is null) return Result.Failure("Request not found.");
        if (request.RequesterId != command.EmployeeId)
            return Result.Failure("You can only confirm receipt of your own request.");

        try
        {
            request.ConfirmReceipt();
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message);
        }

        // Everything dispatched for this request lands with the requester.
        var assignments = await _db.AssetAssignments
            .Where(x => x.EmployeeId == request.RequesterId && x.ReturnedUtc == null)
            .Select(x => x.AssetId)
            .ToListAsync(ct);

        var inTransit = await _db.Assets
            .Where(a => assignments.Contains(a.Id) && a.Status == AssetStatus.InTransit)
            .ToListAsync(ct);

        foreach (var asset in inTransit)
        {
            asset.Status = AssetStatus.Assigned;
            asset.AssignedToEmployeeId = request.RequesterId;
        }

        var photoResult = await _photos.AttachAsync(
            command.Photo, PhotoKind.ProofOfReceipt, request.FulfilledWithAssetId, request.Id, ct: ct);
        if (!photoResult.Succeeded) return Result.Failure(photoResult.Error!);

        await _audit.LogAsync("Confirmed receipt", request.FulfilledWithAsset?.Tag ?? request.ItemName, ct: ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> CancelAsync(int requestId, int employeeId, CancellationToken ct = default)
        => await MutateAsync(requestId, async request =>
        {
            if (request.RequesterId != employeeId) return Result.Failure("You can only cancel your own request.");
            if (request.Status != RequestStatus.Pending) return Result.Failure("Only a pending request can be cancelled.");

            request.Status = RequestStatus.Cancelled;
            await _audit.LogAsync("Cancelled request", request.ItemName, ct: ct);
            return Result.Success();
        }, ct);

    /// <summary>Loads a tracked request, runs the mutation, translates domain errors and saves.</summary>
    /// <summary>
    /// Fulfilling or ordering for a request that hasn't cleared approval yet is an
    /// implicit approval by whoever is doing it — used as a one-click shortcut by
    /// staff who hold both approval and fulfilment rights (e.g. HR, Admin).
    /// </summary>
    private async Task EnsureApprovedAsync(AssetRequest request, CancellationToken ct)
    {
        if (request.Status is not (RequestStatus.Pending or RequestStatus.PendingHrApproval)) return;
        if (_currentUser.EmployeeId is not int approverId) return;

        var approver = await _db.Employees.FirstOrDefaultAsync(e => e.Id == approverId, ct);
        if (approver is null) return;

        request.Approve(approver);
    }

    private async Task<Result> MutateAsync(int requestId, Func<AssetRequest, Task<Result>> mutate, CancellationToken ct)
    {
        var request = await _db.AssetRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);
        if (request is null) return Result.Failure("Request not found.");

        Result result;
        try
        {
            result = await mutate(request);
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message);
        }

        if (!result.Succeeded) return result;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
