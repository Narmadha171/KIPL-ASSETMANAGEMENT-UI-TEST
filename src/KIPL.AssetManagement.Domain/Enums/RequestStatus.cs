namespace KIPL.AssetManagement.Domain.Enums;

/// <summary>Lifecycle of an employee asset request.</summary>
public enum RequestStatus
{
    /// <summary>Raised by an employee, awaiting manager / HR approval.</summary>
    Pending = 0,
    /// <summary>Approved but no operations person has picked it up yet.</summary>
    Unclaimed = 1,
    /// <summary>Claimed by an operations user, being fulfilled.</summary>
    Claimed = 2,
    /// <summary>Ordered from an external vendor, awaiting delivery + verification.</summary>
    OnlineOrdered = 3,
    /// <summary>Handed to courier or ready at the office desk.</summary>
    Dispatched = 4,
    /// <summary>Employee confirmed receipt.</summary>
    Completed = 5,
    Rejected = 6,
    Cancelled = 7,
    /// <summary>The requester's manager has approved it; HR still needs to give the final sign-off.</summary>
    PendingHrApproval = 8
}
