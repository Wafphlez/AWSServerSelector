using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using AWSServerSelector.Models;
using AWSServerSelector.Services.Interfaces;
using AWSServerSelector.ViewModels;

namespace AWSServerSelector;

public partial class UpdateDialog : Window
{
    private const string UpdateNotificationsChannel = "update";

    private readonly UpdateDialogViewModel _viewModel;
    private readonly INotificationService _notificationService;
    private readonly IExternalNavigationService _externalNavigationService;
    public ReadOnlyObservableCollection<NotificationItem> ToastNotifications { get; }

    public UpdateDialog(
        UpdateDialogViewModel viewModel,
        INotificationService notificationService,
        IExternalNavigationService externalNavigationService)
    {
        _viewModel = viewModel;
        _notificationService = notificationService;
        _externalNavigationService = externalNavigationService;
        ToastNotifications = _notificationService.GetNotifications(UpdateNotificationsChannel);
        InitializeComponent();
        DataContext = _viewModel;
        DialogActionBar.SecondaryButtonText = LocalizationManager.GetString("DownloadUpdate");
        DialogActionBar.PrimaryButtonText = LocalizationManager.GetString("Close");
    }

    public async Task StartUpdateCheckAsync(string currentVersion)
    {
        try
        {
            await _viewModel.CheckForUpdatesAsync(currentVersion);
        }
        catch (Exception ex)
        {
            var errorText = LocalizationManager.GetString("UpdateCheckError") ?? "Error checking for updates:";
            _viewModel.StatusText = $"{errorText} {ex.Message}";
        }
    }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
        if (string.IsNullOrEmpty(_viewModel.DownloadUrl))
        {
            _notificationService.ShowError(UpdateNotificationsChannel, "Ссылка для скачивания недоступна.");
            return;
        }

        try
        {
            DialogActionBar.IsEnabled = false;
            DialogActionBar.SecondaryButtonText = "Скачивание...";
            _externalNavigationService.OpenUrl(_viewModel.DownloadUrl);

            _viewModel.StatusText = "Ссылка для скачивания открыта в браузере.";
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(UpdateNotificationsChannel, $"Ошибка при открытии ссылки: {ex.Message}");
        }
        finally
        {
            DialogActionBar.IsEnabled = true;
            DialogActionBar.SecondaryButtonText = "Скачать обновление";
        }
    }
}

