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

    public string SettingsTitle => LocalizationManager.GetString("Settings");
    public string LanguageText => LocalizationManager.GetString("Language");
    public string MethodText => LocalizationManager.GetString("Method");
    public string GatekeepOptionsText => LocalizationManager.GetString("GatekeepOptions");
    public string BlockBothText => LocalizationManager.GetString("BlockBoth");
    public string BlockPingText => LocalizationManager.GetString("BlockPing");
    public string BlockServiceText => LocalizationManager.GetString("BlockService");
    public string MergeUnstableText => LocalizationManager.GetString("MergeUnstable");
    public string DefaultOptionsText => LocalizationManager.GetString("DefaultOptions");
    public string ApplyChangesText => LocalizationManager.GetString("ApplyChanges");

    public void ResetDefaults()
    {
        SelectedLanguage = "en";
        SelectedMode = "hosts";
        IsBlockBoth = true;
        IsBlockPing = false;
        IsBlockService = false;
        IsMergeUnstable = false;
    }

    public void NotifyLocalizationChanged()
    {
        OnPropertyChanged(nameof(SettingsTitle));
        OnPropertyChanged(nameof(LanguageText));
        OnPropertyChanged(nameof(MethodText));
        OnPropertyChanged(nameof(GatekeepOptionsText));
        OnPropertyChanged(nameof(BlockBothText));
        OnPropertyChanged(nameof(BlockPingText));
        OnPropertyChanged(nameof(BlockServiceText));
        OnPropertyChanged(nameof(MergeUnstableText));
        OnPropertyChanged(nameof(DefaultOptionsText));
        OnPropertyChanged(nameof(ApplyChangesText));
    }
}
