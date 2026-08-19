using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KIPL.AssetManagement.Application.Notifications;

public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _db;

    public NotificationService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<NotificationItemDto>> GetNotificationsAsync(
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        var list = new List<NotificationItemDto>();
        var employeeId = currentUser.EmployeeId;
        var role = currentUser.RoleName;

        // 1. Employee self-service notifications
        if (employeeId is int me)
        {
            // Dispatched requests awaiting employee receipt confirmation
            var dispatched = await _db.AssetRequests
                .AsNoTracking()
                .Where(r => r.RequesterId == me && r.Status == RequestStatus.Dispatched)
                .OrderByDescending(r => r.DispatchedUtc ?? r.ModifiedUtc ?? r.CreatedUtc)
                .Take(5)
                .ToListAsync(ct);

            foreach (var req in dispatched)
            {
                var modeDesc = req.Mode == FulfilmentMode.Courier
                    ? $"via Courier {(string.IsNullOrWhiteSpace(req.CourierTrackingNumber) ? "" : $"({req.CourierTrackingNumber})")}"
                    : $"for Office Pickup {(string.IsNullOrWhiteSpace(req.OfficePickupLocation) ? "" : $"at {req.OfficePickupLocation}")}";

                list.Add(new NotificationItemDto(
                    Id: $"req-dispatched-{req.Id}",
                    Title: "Asset Ready For Receipt",
                    Message: $"Your requested {req.ItemName} was dispatched {modeDesc.Trim()}. Please confirm receipt.",
                    TimeAgo: FormatRelativeTime(req.DispatchedUtc ?? req.ModifiedUtc ?? req.CreatedUtc),
                    Icon: "truck",
                    Tone: "signal",
                    TargetUrl: "/Me/Requests",
                    IsUrgent: true,
                    CreatedUtc: req.DispatchedUtc ?? req.ModifiedUtc ?? req.CreatedUtc
                ));
            }

            // Pending handover returns
            var pendingReturns = await _db.AssetReturns
                .AsNoTracking()
                .Include(r => r.Asset)
                .Where(r => r.EmployeeId == me && r.Stage == ReturnStage.AwaitingPickup)
                .OrderByDescending(r => r.RaisedUtc)
                .Take(5)
                .ToListAsync(ct);

            foreach (var ret in pendingReturns)
            {
                var assetName = ret.Asset != null ? $"{ret.Asset.Brand} ({ret.Asset.Tag})" : "asset";
                list.Add(new NotificationItemDto(
                    Id: $"ret-handover-{ret.Id}",
                    Title: "Return Handover Pending",
                    Message: $"Return request for {assetName} is awaiting handover.",
                    TimeAgo: FormatRelativeTime(ret.RaisedUtc),
                    Icon: "undo",
                    Tone: "amber",
                    TargetUrl: "/Me/Returns",
                    IsUrgent: true,
                    CreatedUtc: ret.RaisedUtc
                ));
            }
        }

        // 2. Reporting Manager approvals
        if (currentUser.HasPermission(Permissions.ApproveTeamRequests) && employeeId is int managerId)
        {
            var pendingTeamRequests = await _db.AssetRequests
                .AsNoTracking()
                .Include(r => r.Requester)
                .Where(r => r.Requester.ManagerId == managerId && r.Status == RequestStatus.Pending)
                .OrderByDescending(r => r.RaisedUtc)
                .Take(10)
                .ToListAsync(ct);

            foreach (var req in pendingTeamRequests)
            {
                list.Add(new NotificationItemDto(
                    Id: $"mgr-req-{req.Id}",
                    Title: "Team Request Awaiting Approval",
                    Message: $"{req.Requester.FullName} requested {req.ItemName} ({req.Category}).",
                    TimeAgo: FormatRelativeTime(req.RaisedUtc),
                    Icon: "checkcheck",
                    Tone: "amber",
                    TargetUrl: "/Team/Approvals",
                    IsUrgent: true,
                    CreatedUtc: req.RaisedUtc
                ));
            }
        }

        // 3. HR Approvals & Online Order Deliveries
        if (currentUser.HasPermission(Permissions.ApproveRequests))
        {
            var pendingHr = await _db.AssetRequests
                .AsNoTracking()
                .Include(r => r.Requester)
                .Where(r => r.Status == RequestStatus.PendingHrApproval)
                .OrderByDescending(r => r.ApprovedUtc ?? r.RaisedUtc)
                .Take(10)
                .ToListAsync(ct);

            foreach (var req in pendingHr)
            {
                list.Add(new NotificationItemDto(
                    Id: $"hr-req-{req.Id}",
                    Title: "HR Approval Required",
                    Message: $"{req.Requester.FullName}'s request for {req.ItemName} requires sign-off.",
                    TimeAgo: FormatRelativeTime(req.ApprovedUtc ?? req.RaisedUtc),
                    Icon: "checkcheck",
                    Tone: "amber",
                    TargetUrl: "/Ops/HrApprovals",
                    IsUrgent: true,
                    CreatedUtc: req.ApprovedUtc ?? req.RaisedUtc
                ));
            }

            var unverifiedOrders = await _db.AssetRequests
                .AsNoTracking()
                .Include(r => r.Requester)
                .Where(r => r.Status == RequestStatus.OnlineOrdered && !r.OnlineOrderVerified)
                .OrderByDescending(r => r.ModifiedUtc ?? r.RaisedUtc)
                .Take(5)
                .ToListAsync(ct);

            foreach (var req in unverifiedOrders)
            {
                list.Add(new NotificationItemDto(
                    Id: $"hr-order-{req.Id}",
                    Title: "Online Delivery Verification",
                    Message: $"Order #{req.OrderId ?? "N/A"} ({req.OrderBrand ?? req.ItemName}) for {req.Requester.FullName} awaits check-in.",
                    TimeAgo: FormatRelativeTime(req.ModifiedUtc ?? req.RaisedUtc),
                    Icon: "cart",
                    Tone: "signal",
                    TargetUrl: "/Ops/HrApprovals",
                    IsUrgent: false,
                    CreatedUtc: req.ModifiedUtc ?? req.RaisedUtc
                ));
            }
        }

        // 4. Operations / IT Asset Executive fulfilment queue
        if (currentUser.HasPermission(Permissions.ClaimRequests))
        {
            var unclaimed = await _db.AssetRequests
                .AsNoTracking()
                .Include(r => r.Requester)
                .Where(r => r.Status == RequestStatus.Unclaimed)
                .OrderByDescending(r => r.ApprovedUtc ?? r.RaisedUtc)
                .Take(10)
                .ToListAsync(ct);

            foreach (var req in unclaimed)
            {
                list.Add(new NotificationItemDto(
                    Id: $"exec-unclaimed-{req.Id}",
                    Title: "Unclaimed Request In Queue",
                    Message: $"{req.ItemName} for {req.Requester.FullName} is ready to be claimed and fulfilled.",
                    TimeAgo: FormatRelativeTime(req.ApprovedUtc ?? req.RaisedUtc),
                    Icon: "package",
                    Tone: "signal",
                    TargetUrl: "/Ops/ExecApprovals",
                    IsUrgent: true,
                    CreatedUtc: req.ApprovedUtc ?? req.RaisedUtc
                ));
            }
        }

        // 5. Returns tracking (inspections needed)
        if (currentUser.HasPermission(Permissions.TrackReturns))
        {
            var pendingInspection = await _db.AssetReturns
                .AsNoTracking()
                .Include(r => r.Asset)
                .Include(r => r.Employee)
                .Where(r => r.Stage == ReturnStage.ReceivedForInspection)
                .OrderByDescending(r => r.ReceivedUtc ?? r.RaisedUtc)
                .Take(5)
                .ToListAsync(ct);

            foreach (var ret in pendingInspection)
            {
                var tag = ret.Asset?.Tag ?? "Asset";
                list.Add(new NotificationItemDto(
                    Id: $"ret-inspect-{ret.Id}",
                    Title: "Return Received — Inspection Needed",
                    Message: $"{tag} from {ret.Employee.FullName} is received and pending physical inspection.",
                    TimeAgo: FormatRelativeTime(ret.ReceivedUtc ?? ret.RaisedUtc),
                    Icon: "inbox",
                    Tone: "amber",
                    TargetUrl: "/Ops/ReturnTracking",
                    IsUrgent: false,
                    CreatedUtc: ret.ReceivedUtc ?? ret.RaisedUtc
                ));
            }
        }

        // 6. Service Queue
        if (currentUser.HasPermission(Permissions.ManageInventory))
        {
            var pendingService = await _db.ServiceRequests
                .AsNoTracking()
                .Include(s => s.Asset)
                .Where(s => s.Status == ServiceRequestStatus.Open)
                .OrderByDescending(s => s.RaisedUtc)
                .Take(5)
                .ToListAsync(ct);

            foreach (var srv in pendingService)
            {
                var tag = srv.Asset?.Tag ?? "Asset";
                list.Add(new NotificationItemDto(
                    Id: $"srv-pending-{srv.Id}",
                    Title: "Service Queue Maintenance",
                    Message: $"{tag} ({srv.Asset?.Brand}) reported with '{srv.IssueType}'.",
                    TimeAgo: FormatRelativeTime(srv.RaisedUtc),
                    Icon: "wrench",
                    Tone: "danger",
                    TargetUrl: "/Ops/ServiceQueue",
                    IsUrgent: srv.Urgency == ServiceUrgency.CannotWork,
                    CreatedUtc: srv.RaisedUtc
                ));
            }
        }

        return list.OrderByDescending(n => n.IsUrgent)
                   .ThenByDescending(n => n.CreatedUtc)
                   .ToList();
    }

    private static string FormatRelativeTime(DateTime utc)
    {
        var delta = DateTime.UtcNow - utc;
        if (delta.TotalMinutes < 2) return "Just now";
        if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes}m ago";
        if (delta.TotalHours < 24) return $"{(int)delta.TotalHours}h ago";
        if (delta.TotalDays < 7) return $"{(int)delta.TotalDays}d ago";
        return utc.ToLocalTime().ToString("MMM d");
    }
}
