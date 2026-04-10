using System;
using System.Windows;
using AWSServerSelector.ViewModels;

namespace AWSServerSelector;

public partial class SettingsDialog : Window
{
    private readonly SettingsDialogViewModel _viewModel;

    public string SelectedLanguage => _viewModel.SelectedLanguage;
    public string SelectedMode => _viewModel.SelectedMode;
    public bool IsBlockBoth => _viewModel.IsBlockBoth;
    public bool IsBlockPing => _viewModel.IsBlockPing;
    public bool IsBlockService => _viewModel.IsBlockService;
    public bool IsMergeUnstable => _viewModel.IsMergeUnstable;

    public SettingsDialog(SettingsDialogViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;

        Title = _viewModel.SettingsTitle;
        LocalizationManager.LanguageChanged += OnLanguageChanged;
    }

    public void InitializeFromSettings(
        string language,
        string mode,
        bool blockBoth,
        bool blockPing,
        bool blockService,
        bool mergeUnstable)
    {
        _viewModel.SelectedLanguage = language;
        _viewModel.SelectedMode = mode;
        _viewModel.IsBlockBoth = blockBoth;
        _viewModel.IsBlockPing = blockPing;
        _viewModel.IsBlockService = blockService;
        _viewModel.IsMergeUnstable = mergeUnstable;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        _viewModel.NotifyLocalizationChanged();
        Title = _viewModel.SettingsTitle;
    }

    private void DefaultButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ResetDefaults();
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        LocalizationManager.LanguageChanged -= OnLanguageChanged;
        base.OnClosed(e);
    }
}

public class LanguageItem
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class ModeItem
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
