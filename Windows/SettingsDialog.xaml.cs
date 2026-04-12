using System;
using System.Windows;
using AWSServerSelector.ViewModels;

namespace AWSServerSelector;

public partial class SettingsDialog : Window
{
    private readonly SettingsDialogViewModel _viewModel;

    public SettingsDialogViewModel ViewModel => _viewModel;

    public SettingsDialog(SettingsDialogViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;

        Title = _viewModel.SettingsTitle;
        LocalizationManager.LanguageChanged += OnLanguageChanged;
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
