namespace KIPL.AssetManagement.Application.Notifications;

public record NotificationItemDto(
    string Id,
    string Title,
    string Message,
    string TimeAgo,
    string Icon,
    string Tone,
    string? TargetUrl,
    bool IsUrgent,
    DateTime CreatedUtc
);
