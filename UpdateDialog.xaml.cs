using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace AWSServerSelector
{
    public partial class UpdateDialog : Window, INotifyPropertyChanged
    {
        private string _statusText = "Проверяем наличие обновлений...";
        private string _updateTitle = "";
        private string _updateDescription = "";
        private string _updateSize = "";
        private string _downloadUrl = "";

        public string StatusText 
        { 
            get => _statusText; 
            set 
            { 
                _statusText = value; 
                OnPropertyChanged(nameof(StatusText)); 
            } 
        }

        public string UpdateTitle 
        { 
            get => _updateTitle; 
            set 
            { 
                _updateTitle = value; 
                OnPropertyChanged(nameof(UpdateTitle)); 
            } 
        }

        public string UpdateDescription 
        { 
            get => _updateDescription; 
            set 
            { 
                _updateDescription = value; 
                OnPropertyChanged(nameof(UpdateDescription)); 
            } 
        }

        public string UpdateSize 
        { 
            get => _updateSize; 
            set 
            { 
                _updateSize = value; 
                OnPropertyChanged(nameof(UpdateSize)); 
            } 
        }

        public UpdateDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void StartUpdateCheck(string currentVersion)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var releaseChecker = new GitHubReleaseChecker(currentVersion);
                    var latestRelease = await releaseChecker.GetLatestReleaseAsync();
                    
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (latestRelease != null)
                        {
                            if (releaseChecker.IsUpdateAvailable(latestRelease))
                            {
                                var downloadAsset = releaseChecker.GetDownloadAsset(latestRelease);
                                if (downloadAsset != null)
                                {
                                    SetUpdateInfo(latestRelease, downloadAsset);
                                }
                                else
                                {
                                    StatusText = LocalizationManager.GetString("UpdateDownloadUnavailable");
                                }
                            }
                            else
                            {
                                SetNoUpdateInfo();
                            }
                        }
                        else
                        {
                            StatusText = LocalizationManager.GetString("UpdateCheckFailed");
                        }
                    });
                }
                catch (Exception ex)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        StatusText = $"{LocalizationManager.GetString("UpdateCheckError")} {ex.Message}";
                    });
                }
            });
        }

        public void SetUpdateInfo(GitHubReleaseInfo release, GitHubAsset downloadAsset)
        {
            UpdateTitle = $"{LocalizationManager.GetString("UpdateAvailable")} {release.TagName}";
            UpdateDescription = release.Body.Length > 200 ? 
                release.Body.Substring(0, 200) + "..." : release.Body;
            UpdateSize = $"{LocalizationManager.GetString("UpdateSize")} {FormatFileSize(downloadAsset.Size)}";
            _downloadUrl = downloadAsset.BrowserDownloadUrl;
            
            UpdateInfoPanel.Visibility = Visibility.Visible;
            DownloadButton.Visibility = Visibility.Visible;
            DownloadButton.Content = LocalizationManager.GetString("DownloadUpdate");
        }

        public void SetNoUpdateInfo()
        {
            StatusText = LocalizationManager.GetString("NoUpdatesAvailable");
            UpdateInfoPanel.Visibility = Visibility.Collapsed;
            DownloadButton.Visibility = Visibility.Collapsed;
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_downloadUrl))
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
                    FileName = _downloadUrl,
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

