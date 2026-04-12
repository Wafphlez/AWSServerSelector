using CommunityToolkit.Mvvm.ComponentModel;
using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.ViewModels;

public partial class AboutDialogViewModel : ObservableObject
{
    public string DialogTitle => LocalizationManager.GetString("AboutTitle");

    [ObservableProperty]
    private string aboutText = string.Empty;

    [ObservableProperty]
    private string developer = string.Empty;

    [ObservableProperty]
    private string versionText = string.Empty;

    [ObservableProperty]
    private string awesomeText = string.Empty;

    public void Configure(ILocalizationService localizationService, string currentVersion)
    {
        AboutText = localizationService.GetString("AboutText");
        Developer = localizationService.GetString("Developer");
        VersionText = localizationService.GetString("Version", currentVersion);
        AwesomeText = localizationService.GetString("Awesome");
        OnPropertyChanged(nameof(DialogTitle));
    }
}
