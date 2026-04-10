using CommunityToolkit.Mvvm.ComponentModel;

namespace AWSServerSelector.ViewModels;

public partial class ConnectionInfoViewModel : ObservableObject
{
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
    private string gameIpText = string.Empty;

    [ObservableProperty]
    private string gameServerText = string.Empty;

    [ObservableProperty]
    private string gamePingText = string.Empty;

    [ObservableProperty]
    private string gameRegionText = string.Empty;

    [ObservableProperty]
    private string lastUpdateText = string.Empty;
}
