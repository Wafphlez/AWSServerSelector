using System;
using System.Threading.Tasks;
using System.Windows;
using AWSServerSelector.Services.Interfaces;
using AWSServerSelector.ViewModels;

namespace AWSServerSelector;

public partial class UpdateDialog : Window
{
    private readonly UpdateDialogViewModel _viewModel;
    private readonly IMessageService _messageService;
    private readonly IExternalNavigationService _externalNavigationService;

    public UpdateDialog(
        UpdateDialogViewModel viewModel,
        IMessageService messageService,
        IExternalNavigationService externalNavigationService)
    {
        _viewModel = viewModel;
        _messageService = messageService;
        _externalNavigationService = externalNavigationService;
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
            _messageService.Show("Ссылка для скачивания недоступна.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
            _messageService.Show($"Ошибка при открытии ссылки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            DialogActionBar.IsEnabled = true;
            DialogActionBar.SecondaryButtonText = "Скачать обновление";
        }
    }
}

