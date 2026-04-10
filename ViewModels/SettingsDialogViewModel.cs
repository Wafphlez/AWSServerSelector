using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AWSServerSelector.ViewModels;

public partial class SettingsDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string selectedLanguage = "en";

    [ObservableProperty]
    private string selectedMode = "hosts";

    [ObservableProperty]
    private bool isBlockBoth = true;

    [ObservableProperty]
    private bool isBlockPing;

    [ObservableProperty]
    private bool isBlockService;

    [ObservableProperty]
    private bool isMergeUnstable;

    public List<LanguageItem> Languages { get; } = new()
    {
        new LanguageItem { Code = "en", Name = "English" },
        new LanguageItem { Code = "ru", Name = "Русский" }
    };

    public List<ModeItem> Modes { get; } = new()
    {
        new ModeItem { Code = "hosts", Name = "Hosts File" },
        new ModeItem { Code = "service", Name = "Service" }
    };
}
