using KIPL.AssetManagement.Application.Common.Interfaces;

namespace KIPL.AssetManagement.Application.Notifications;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationItemDto>> GetNotificationsAsync(
        ICurrentUserService currentUser,
        CancellationToken ct = default);
}
