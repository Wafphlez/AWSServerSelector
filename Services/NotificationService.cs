using System.Collections.ObjectModel;
using System.Windows;
using AWSServerSelector.Models;
using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.Services;

public sealed class NotificationService : INotificationService
{
    private const int MaxNotificationsPerChannel = 3;
    private const int EnterAnimationMs = 220;
    private const int ExitAnimationMs = 240;
    private static readonly TimeSpan DedupWindow = TimeSpan.FromMilliseconds(500);

    private readonly object _sync = new();
    private readonly Dictionary<string, ObservableCollection<NotificationItem>> _channelCollections = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ReadOnlyObservableCollection<NotificationItem>> _readonlyChannels = new(StringComparer.OrdinalIgnoreCase);
    private readonly IDispatcherTimerFactory _dispatcherTimerFactory;

    public NotificationService(IDispatcherTimerFactory dispatcherTimerFactory)
    {
        _dispatcherTimerFactory = dispatcherTimerFactory;
    }

    public ReadOnlyObservableCollection<NotificationItem> GetNotifications(string channel)
    {
        lock (_sync)
        {
            if (_readonlyChannels.TryGetValue(channel, out var existing))
            {
                return existing;
            }

            var collection = new ObservableCollection<NotificationItem>();
            var readonlyCollection = new ReadOnlyObservableCollection<NotificationItem>(collection);
            _channelCollections[channel] = collection;
            _readonlyChannels[channel] = readonlyCollection;
            return readonlyCollection;
        }
    }

    public void ShowSuccess(string channel, string message, int durationMs = 3000) =>
        Enqueue(channel, message, NotificationType.Success, durationMs);

    public void ShowWarning(string channel, string message, int durationMs = 3500) =>
        Enqueue(channel, message, NotificationType.Warning, durationMs);

    public void ShowError(string channel, string message, int durationMs = 4500) =>
        Enqueue(channel, message, NotificationType.Error, durationMs);

    private void Enqueue(string channel, string message, NotificationType type, int durationMs)
    {
        if (string.IsNullOrWhiteSpace(channel) || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (durationMs < 500)
        {
            durationMs = 500;
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            AddNotification(channel, message, type, durationMs);
            return;
        }

        dispatcher.Invoke(() => AddNotification(channel, message, type, durationMs));
    }

    private void AddNotification(string channel, string message, NotificationType type, int durationMs)
    {
        var notifications = GetOrCreateChannelCollection(channel);
        var now = DateTimeOffset.UtcNow;

        var isDuplicate = notifications.Any(item =>
            item.Type == type &&
            string.Equals(item.Message, message, StringComparison.Ordinal) &&
            now - item.CreatedAtUtc <= DedupWindow);

        if (isDuplicate)
        {
            return;
        }

        RequestOverflowCloseIfNeeded(channel, notifications);

        var notification = new NotificationItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Message = message,
            Type = type,
            CreatedAtUtc = now
        };

        notifications.Add(notification);
        ScheduleRemoval(channel, notification, durationMs);
    }

    private void RequestOverflowCloseIfNeeded(string channel, ObservableCollection<NotificationItem> notifications)
    {
        if (notifications.Count < MaxNotificationsPerChannel)
        {
            return;
        }

        var oldestActive = notifications.FirstOrDefault(item => !item.IsClosing);
        if (oldestActive != null)
        {
            BeginClosing(channel, oldestActive, ExitAnimationMs);
            return;
        }

        // Fallback: если все элементы уже закрываются, удаляем самый старый.
        notifications.RemoveAt(0);
    }

    private ObservableCollection<NotificationItem> GetOrCreateChannelCollection(string channel)
    {
        lock (_sync)
        {
            if (_channelCollections.TryGetValue(channel, out var collection))
            {
                return collection;
            }

            collection = new ObservableCollection<NotificationItem>();
            _channelCollections[channel] = collection;
            _readonlyChannels[channel] = new ReadOnlyObservableCollection<NotificationItem>(collection);
            return collection;
        }
    }

    private void ScheduleRemoval(string channel, NotificationItem notification, int durationMs)
    {
        var timer = _dispatcherTimerFactory.Create(TimeSpan.FromMilliseconds(durationMs));
        EventHandler? onTick = null;
        onTick = (_, _) =>
        {
            timer.Stop();
            timer.Tick -= onTick;
            BeginClosing(channel, notification, ExitAnimationMs);
        };

        timer.Tick += onTick;
        timer.Start();
    }

    private void BeginClosing(string channel, NotificationItem notification, int removeDelayMs)
    {
        if (notification.IsClosing)
        {
            return;
        }

        notification.IsClosing = true;
        var removalDelay = Math.Max(removeDelayMs, EnterAnimationMs);
        var timer = _dispatcherTimerFactory.Create(TimeSpan.FromMilliseconds(removalDelay));
        EventHandler? onTick = null;
        onTick = (_, _) =>
        {
            timer.Stop();
            timer.Tick -= onTick;
            RemoveNotification(channel, notification.Id);
        };

        timer.Tick += onTick;
        timer.Start();
    }

    private void RemoveNotification(string channel, string notificationId)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            RemoveNotificationCore(channel, notificationId);
            return;
        }

        dispatcher.Invoke(() => RemoveNotificationCore(channel, notificationId));
    }

    private void RemoveNotificationCore(string channel, string notificationId)
    {
        var notifications = GetOrCreateChannelCollection(channel);
        var target = notifications.FirstOrDefault(item => item.Id == notificationId);
        if (target != null)
        {
            notifications.Remove(target);
        }
    }
}
