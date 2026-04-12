using System;
using System.Collections.ObjectModel;
using System.Windows;
using AWSServerSelector.Models;
using AWSServerSelector.Services.Interfaces;
using AWSServerSelector.ViewModels;

namespace AWSServerSelector;

public partial class SettingsDialog : Window
{
    private const string SettingsNotificationsChannel = "settings";

    private readonly SettingsDialogViewModel _viewModel;
    private readonly INotificationService _notificationService;
    private readonly ILocalizationService _localizationService;

    public SettingsDialogViewModel ViewModel => _viewModel;
    public ReadOnlyObservableCollection<NotificationItem> ToastNotifications { get; }
    public event Action<(string selectedLanguage, ApplyMode applyMode, BlockMode blockMode, bool mergeUnstable)>? ApplyRequested;

    public SettingsDialog(
        SettingsDialogViewModel viewModel,
        INotificationService notificationService,
        ILocalizationService localizationService)
    {
        _viewModel = viewModel;
        _notificationService = notificationService;
        _localizationService = localizationService;
        ToastNotifications = _notificationService.GetNotifications(SettingsNotificationsChannel);
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
        var result = _viewModel.CreateResult();
        ApplyRequested?.Invoke(result);
        _notificationService.ShowSuccess(
            SettingsNotificationsChannel,
            _localizationService.GetString("SettingsApplied"));
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
