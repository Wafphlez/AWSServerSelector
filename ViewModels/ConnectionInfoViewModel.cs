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
}
