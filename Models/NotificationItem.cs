using CommunityToolkit.Mvvm.ComponentModel;

namespace AWSServerSelector.Models;

public enum NotificationType
{
    Success,
    Warning,
    Error
}

public partial class NotificationItem : ObservableObject
{
    public required string Id { get; init; }
    public required string Message { get; init; }
    public required NotificationType Type { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }

    [ObservableProperty]
    private bool isClosing;
}
