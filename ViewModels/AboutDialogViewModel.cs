using CommunityToolkit.Mvvm.ComponentModel;

namespace AWSServerSelector.ViewModels;

public partial class AboutDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string aboutText = string.Empty;

    [ObservableProperty]
    private string developer = string.Empty;

    [ObservableProperty]
    private string versionText = string.Empty;

    [ObservableProperty]
    private string awesomeText = string.Empty;
}
