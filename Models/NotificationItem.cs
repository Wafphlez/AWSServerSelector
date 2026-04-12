namespace AWSServerSelector.Models;

public enum NotificationType
{
    Success,
    Warning,
    Error
}

public sealed class NotificationItem
{
    public required string Id { get; init; }
    public required string Message { get; init; }
    public required NotificationType Type { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
}
