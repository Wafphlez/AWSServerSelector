using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AWSServerSelector.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel(
        ObservableCollection<ServerItem> serverItems,
        ObservableCollection<ServerGroupItem> serverGroups)
    {
        ServerItems = serverItems;
        ServerGroups = serverGroups;
    }

    public ObservableCollection<ServerItem> ServerItems { get; }

    public ObservableCollection<ServerGroupItem> ServerGroups { get; }
}
