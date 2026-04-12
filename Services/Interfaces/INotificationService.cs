using System.Collections.ObjectModel;
using AWSServerSelector.Models;

namespace AWSServerSelector.Services.Interfaces;

public interface INotificationService
{
    ReadOnlyObservableCollection<NotificationItem> GetNotifications(string channel);
    void ShowSuccess(string channel, string message, int durationMs = 3000);
    void ShowWarning(string channel, string message, int durationMs = 3500);
    void ShowError(string channel, string message, int durationMs = 4500);
}
