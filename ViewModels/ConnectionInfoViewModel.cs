using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace AWSServerSelector.ViewModels;

public partial class ConnectionInfoViewModel : ObservableObject
{
    [ObservableProperty]
    private Brush lobbyStatusForeground = Brushes.White;

    [ObservableProperty]
    private Brush lobbyPingForeground = Brushes.White;

    [ObservableProperty]
    private Visibility lobbyCopyVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private string lobbyStatusText = string.Empty;

    [ObservableProperty]
    private string lobbyIpText = string.Empty;

    [ObservableProperty]
    private string lobbyServerText = string.Empty;

    [ObservableProperty]
    private string lobbyPingText = string.Empty;

    [ObservableProperty]
    private string lobbyRegionText = string.Empty;

    [ObservableProperty]
    private string gameStatusText = string.Empty;

    [ObservableProperty]
    private Brush gameStatusForeground = Brushes.White;

    [ObservableProperty]
    private Brush gamePingForeground = Brushes.White;

    [ObservableProperty]
    private Visibility gameCopyVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private string gameIpText = string.Empty;

    [ObservableProperty]
    private string gameServerText = string.Empty;

    [ObservableProperty]
    private string gamePingText = string.Empty;

    [ObservableProperty]
    private string gameRegionText = string.Empty;

    [ObservableProperty]
    private string lastUpdateText = string.Empty;

    [ObservableProperty]
    private Brush lastUpdateForeground = Brushes.White;

    public void ApplyLobbyConnected(string endpoint, string server, string region, string pingText, Brush pingBrush)
    {
        LobbyStatusText = LocalizationManager.Connected;
        LobbyStatusForeground = new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45));
        LobbyIpText = endpoint;
        LobbyCopyVisibility = Visibility.Visible;
        LobbyServerText = server;
        LobbyRegionText = region;
        LobbyPingText = pingText;
        LobbyPingForeground = pingBrush;
    }

    public void ApplyLobbyDisconnected()
    {
        LobbyStatusText = LocalizationManager.NotConnected;
        LobbyStatusForeground = new SolidColorBrush(Color.FromRgb(0xDC, 0x14, 0x3C));
        LobbyIpText = LocalizationManager.NotDetermined;
        LobbyCopyVisibility = Visibility.Collapsed;
        LobbyServerText = LocalizationManager.NotDetermined;
        LobbyRegionText = LocalizationManager.NotDetermined;
        LobbyPingText = LocalizationManager.NotMeasured;
        LobbyPingForeground = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
    }

    public void ApplyLobbyNotRunning()
    {
        LobbyStatusText = LocalizationManager.GameNotRunning;
        LobbyStatusForeground = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));
        LobbyIpText = LocalizationManager.NotDetermined;
        LobbyCopyVisibility = Visibility.Collapsed;
        LobbyServerText = LocalizationManager.NotDetermined;
        LobbyRegionText = LocalizationManager.NotDetermined;
        LobbyPingText = LocalizationManager.NotMeasured;
        LobbyPingForeground = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));
    }

    public void ApplyGameConnected(string statusText, string endpoint, string server, string region)
    {
        GameStatusText = statusText;
        GameStatusForeground = new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45));
        GameIpText = endpoint;
        GameCopyVisibility = Visibility.Visible;
        GameServerText = server;
        GameRegionText = region;
    }

    public void ApplyGameDisconnected(string statusText)
    {
        GameStatusText = statusText;
        GameStatusForeground = new SolidColorBrush(Color.FromRgb(0xDC, 0x14, 0x3C));
        GameIpText = LocalizationManager.NotDetermined;
        GameCopyVisibility = Visibility.Collapsed;
        GameServerText = LocalizationManager.NotDetermined;
        GameRegionText = LocalizationManager.NotDetermined;
        GamePingText = LocalizationManager.NotMeasured;
        GamePingForeground = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
    }

    public void ApplyGameNotRunning(string statusText)
    {
        GameStatusText = statusText;
        GameStatusForeground = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));
        GameIpText = LocalizationManager.NotDetermined;
        GameCopyVisibility = Visibility.Collapsed;
        GameServerText = LocalizationManager.NotDetermined;
        GameRegionText = LocalizationManager.NotDetermined;
        GamePingText = LocalizationManager.NotMeasured;
        GamePingForeground = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));
    }
}
