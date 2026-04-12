using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AWSServerSelector.Models;
using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly Action _applyAction;
    private readonly Action _revertAction;
    private readonly Action _settingsAction;
    private readonly Action _aboutAction;
    private readonly Action _updatesAction;
    private readonly Action _openHostsAction;
    private readonly Action _connectionInfoAction;
    private readonly ILocalizationService _localizationService;

    public MainWindowViewModel(
        ObservableCollection<ServerItem> serverItems,
        ObservableCollection<ServerGroupItem> serverGroups,
        Action applyAction,
        Action revertAction,
        Action settingsAction,
        Action aboutAction,
        Action updatesAction,
        Action openHostsAction,
        Action connectionInfoAction,
        ILocalizationService localizationService)
    {
        ServerItems = serverItems;
        ServerGroups = serverGroups;
        _applyAction = applyAction;
        _revertAction = revertAction;
        _settingsAction = settingsAction;
        _aboutAction = aboutAction;
        _updatesAction = updatesAction;
        _openHostsAction = openHostsAction;
        _connectionInfoAction = connectionInfoAction;
        _localizationService = localizationService;
    }

    public ObservableCollection<ServerItem> ServerItems { get; }

    public ObservableCollection<ServerGroupItem> ServerGroups { get; }

    [ObservableProperty]
    private ApplyMode applyMode = ApplyMode.Gatekeep;

    [ObservableProperty]
    private BlockMode blockMode = BlockMode.Both;

    [ObservableProperty]
    private bool mergeUnstable = true;

    [ObservableProperty]
    private string language = "en";

    public string WindowTitle => _localizationService.GetString("AppTitle");
    public string SettingsMenuText => _localizationService.GetString("Settings");
    public string AboutMenuText => _localizationService.GetString("About");
    public string CheckUpdatesMenuText => _localizationService.GetString("CheckUpdates");
    public string OpenHostsMenuText => _localizationService.GetString("OpenHosts");
    public string ConnectionInfoMenuText => _localizationService.GetString("ConnectionInfo");
    public string SelectServersText => _localizationService.GetString("SelectServers");
    public string LatencyHeaderText => _localizationService.GetString("Latency");
    public string StatusText => _localizationService.GetString("StatusText");
    public string RevertButtonText => _localizationService.GetString("ResetToDefault");
    public string ApplyButtonText => _localizationService.GetString("ApplySelection");

    public void NotifyLocalizationChanged()
    {
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(SettingsMenuText));
        OnPropertyChanged(nameof(AboutMenuText));
        OnPropertyChanged(nameof(CheckUpdatesMenuText));
        OnPropertyChanged(nameof(OpenHostsMenuText));
        OnPropertyChanged(nameof(ConnectionInfoMenuText));
        OnPropertyChanged(nameof(SelectServersText));
        OnPropertyChanged(nameof(LatencyHeaderText));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(RevertButtonText));
        OnPropertyChanged(nameof(ApplyButtonText));
    }

    [RelayCommand]
    private void ApplySelection() => _applyAction();

    [RelayCommand]
    private void RevertSelection() => _revertAction();

    [RelayCommand]
    private void OpenSettings() => _settingsAction();

    [RelayCommand]
    private void OpenAbout() => _aboutAction();

    [RelayCommand]
    private void CheckUpdates() => _updatesAction();

    [RelayCommand]
    private void OpenHosts() => _openHostsAction();

    [RelayCommand]
    private void OpenConnectionInfo() => _connectionInfoAction();
}
