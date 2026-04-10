using System.Windows;
using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.Services;

public sealed class MessageService : IMessageService
{
    public MessageBoxResult Show(string message, string title, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None)
    {
        return MessageBox.Show(message, title, button, icon);
    }
}
