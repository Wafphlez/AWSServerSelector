using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using AWSServerSelector.ViewModels;

namespace AWSServerSelector
{
    public partial class UpdateDialog : Window, INotifyPropertyChanged
    {
        private readonly UpdateDialogViewModel _viewModel;

        public string StatusText 
        { 
            get => _viewModel.StatusText;
            set 
            { 
                _viewModel.StatusText = value;
                OnPropertyChanged(nameof(StatusText)); 
            } 
        }

        public string UpdateTitle 
        { 
            get => _viewModel.UpdateTitle;
            set 
            { 
                _viewModel.UpdateTitle = value;
                OnPropertyChanged(nameof(UpdateTitle)); 
            } 
        }

        public string UpdateDescription 
        { 
            get => _viewModel.UpdateDescription;
            set 
            { 
                _viewModel.UpdateDescription = value;
                OnPropertyChanged(nameof(UpdateDescription)); 
            } 
        }

        public string UpdateSize 
        { 
            get => _viewModel.UpdateSize;
            set 
            { 
                _viewModel.UpdateSize = value;
                OnPropertyChanged(nameof(UpdateSize)); 
            } 
        }

        public string YourVersionText 
        { 
            get => _viewModel.YourVersionText;
            set 
            { 
                _viewModel.YourVersionText = value;
                OnPropertyChanged(nameof(YourVersionText)); 
            } 
        }

        public string LatestVersionText 
        { 
            get => _viewModel.LatestVersionText;
            set 
            { 
                _viewModel.LatestVersionText = value;
                OnPropertyChanged(nameof(LatestVersionText)); 
            } 
        }

        public UpdateDialog(UpdateDialogViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            DataContext = this;
            _viewModel.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName ?? string.Empty);
        }

        public void StartUpdateCheck(string currentVersion)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _viewModel.CheckForUpdatesAsync(currentVersion);
                    
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (!string.IsNullOrWhiteSpace(_viewModel.DownloadUrl))
                        {
                            UpdateInfoPanel.Visibility = Visibility.Visible;
                            DownloadButton.Visibility = Visibility.Visible;
                            DownloadButton.Content = LocalizationManager.GetString("DownloadUpdate");
                        }
                        else
                        {
                            UpdateInfoPanel.Visibility = Visibility.Collapsed;
                            DownloadButton.Visibility = Visibility.Collapsed;
                        }
                    });
                }
                catch (Exception ex)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        var errorText = LocalizationManager.GetString("UpdateCheckError") ?? "Error checking for updates:";
                        StatusText = $"{LocalizationManager.GetString("UpdateCheckError")} {ex.Message}";
                    });
                }
            });
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_viewModel.DownloadUrl))
            {
                MessageBox.Show("Ссылка для скачивания недоступна.", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                DownloadButton.IsEnabled = false;
                DownloadButton.Content = "Скачивание...";
                
                // Открываем ссылку в браузере
                Process.Start(new ProcessStartInfo
                {
                    FileName = _viewModel.DownloadUrl,
                    UseShellExecute = true
                });

                StatusText = "Ссылка для скачивания открыта в браузере.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии ссылки: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DownloadButton.IsEnabled = true;
                DownloadButton.Content = "Скачать обновление";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

