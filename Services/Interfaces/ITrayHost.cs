using AWSServerSelector.Models;

namespace AWSServerSelector.Services.Interfaces;

public interface ITrayHost
{
    void ShowFromTray();
    void HideToTray();
    void ToggleVisibilityFromTray();
    void ApplySelectionFromTray();
    void ResetSelectionFromTray();
    void OpenConnectionInfoFromTray();
    Task RefreshPingFromTrayAsync();
    TrayStatusSnapshot GetTrayStatusSnapshot();
    void ExitFromTray();
}
