using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AWSServerSelector.Models;

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

    public MainWindowViewModel(
        ObservableCollection<ServerItem> serverItems,
        ObservableCollection<ServerGroupItem> serverGroups,
        Action applyAction,
        Action revertAction,
        Action settingsAction,
        Action aboutAction,
        Action updatesAction,
        Action openHostsAction,
        Action connectionInfoAction)
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
