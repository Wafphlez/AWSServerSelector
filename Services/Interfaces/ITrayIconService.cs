using System.Windows;

namespace AWSServerSelector.Services.Interfaces;

public interface ITrayIconService : IDisposable
{
    void Initialize(Window mainWindow);
    void ShowMainWindow();
    void HideMainWindow();
    void RefreshStatus();
}
