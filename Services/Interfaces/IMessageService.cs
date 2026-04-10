using System.Windows;

namespace AWSServerSelector.Services.Interfaces;

public interface IMessageService
{
    MessageBoxResult Show(string message, string title, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None);
}
