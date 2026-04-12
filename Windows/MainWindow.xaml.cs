using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
// using System.Windows.Shapes; // Removed to avoid Path ambiguity
using System.Windows.Threading;
using System.Runtime.Versioning;
using AWSServerSelector.Models;
using AWSServerSelector.Services;
using AWSServerSelector.Services.Interfaces;
using AWSServerSelector.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AWSServerSelector
{
    [SupportedOSPlatform("windows")]
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Constants and Fields
        
        private static readonly string CurrentVersion =
            typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "1.0.4";
        
        private readonly IReadOnlyDictionary<string, RegionDefinition> _regions;

        private DispatcherTimer? _pingTimer;
        private ConnectionInfoWindow? _connectionInfoWindow;

        private readonly ISettingsService _settingsService;
        private readonly IHostsFileService _hostsFileService;
        private readonly INetworkProbeService _networkProbeService;
        private readonly IDialogService _dialogService;
        private readonly IRegionCatalogService _regionCatalogService;
        private readonly IExternalNavigationService _externalNavigationService;
        private readonly IDispatcherTimerFactory _dispatcherTimerFactory;
        private readonly INotificationService _notificationService;
        private readonly IHostsContentBuilder _hostsContentBuilder;
        private readonly ILocalizationService _localizationService;
        private readonly AppLinksOptions _appLinksOptions;
        private readonly MonitoringOptions _monitoringOptions;
        
        private const string MainNotificationsChannel = "main";

        private ApplyMode _applyMode = ApplyMode.Gatekeep;
        private BlockMode _blockMode = BlockMode.Both;
        private bool _mergeUnstable = true;
        private string _currentLanguage = "en";

        // Hosts file section marker and path
        private const string SectionMarker = "# -- Ping by Daylight --";
        private static string HostsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "drivers\\etc\\hosts");

        public ObservableCollection<ServerItem> ServerItems { get; set; } = new();
        public ObservableCollection<ServerGroupItem> ServerGroups { get; set; } = new();
        public ReadOnlyObservableCollection<NotificationItem> ToastNotifications { get; }
        public MainWindowViewModel ViewModel { get; }

        #endregion

        #region Constructor and Initialization

        public MainWindow(
            ISettingsService settingsService,
            IHostsFileService hostsFileService,
            INetworkProbeService networkProbeService,
            IDialogService dialogService,
            IRegionCatalogService regionCatalogService,
            IExternalNavigationService externalNavigationService,
            IDispatcherTimerFactory dispatcherTimerFactory,
            INotificationService notificationService,
            IHostsContentBuilder hostsContentBuilder,
            ILocalizationService localizationService,
            IOptions<AppLinksOptions> appLinksOptions,
            IOptions<MonitoringOptions> monitoringOptions)
        {
            _settingsService = settingsService;
            _hostsFileService = hostsFileService;
            _networkProbeService = networkProbeService;
            _dialogService = dialogService;
            _regionCatalogService = regionCatalogService;
            _externalNavigationService = externalNavigationService;
            _dispatcherTimerFactory = dispatcherTimerFactory;
            _notificationService = notificationService;
            _hostsContentBuilder = hostsContentBuilder;
            _localizationService = localizationService;
            _appLinksOptions = appLinksOptions.Value;
            _monitoringOptions = monitoringOptions.Value;
            _regions = _regionCatalogService.Regions;
            ToastNotifications = _notificationService.GetNotifications(MainNotificationsChannel);

            ViewModel = new MainWindowViewModel(
                ServerItems,
                ServerGroups,
                () => ApplyButton_Click(this, new RoutedEventArgs()),
                () => RevertButton_Click(this, new RoutedEventArgs()),
                () => Settings_Click(this, new RoutedEventArgs()),
                () => About_Click(this, new RoutedEventArgs()),
                () => CheckUpdates_Click(this, new RoutedEventArgs()),
                () => OpenHostsButton_Click(this, new RoutedEventArgs()),
                () => ConnectionInfo_Click(this, new RoutedEventArgs()),
                _localizationService);
            InitializeComponent();
            DataContext = ViewModel;
            LoadSettings();
            
            InitializeApplication();
            
            // Subscribe to language change events
            LocalizationManager.LanguageChanged += OnLanguageChanged;
        }

        private void InitializeApplication()
        {
            InitializeServerList();
            StartPingTimer();
            
            UpdateRegionListViewAppearance();
            ViewModel.NotifyLocalizationChanged();
        }

        private void InitializeServerList()
        {
            ServerItems.Clear();
            ServerGroups.Clear();
            
            // Группируем серверы по регионам
            var groupedServers = _regions.GroupBy(kv => GetGroupName(kv.Key))
                                       .OrderBy(g => GetGroupOrder(g.Key));
            
            foreach (var group in groupedServers)
            {
                var groupItem = new ServerGroupItem
                {
                    GroupName = _localizationService.GetGroupDisplayName(group.Key),
                    IsExpanded = true
                };
                
                foreach (var kv in group.OrderBy(x => x.Key))
                {
                    var regionKey = kv.Key;
                    var translatedName = _localizationService.GetServerDisplayName(regionKey, kv.Value.DisplayNameKey);
                    var displayName = translatedName + (kv.Value.Stable ? string.Empty : " ⚠︎");
                    var isDisabled = IsServerDisabledInHosts(regionKey);
                    
                    var item = new ServerItem
                    {
                        RegionKey = regionKey,
                        DisplayName = displayName,
                        IsSelected = !isDisabled, // Выбираем все серверы, кроме отключенных в hosts
                        LatencyText = "…",
                        IsStable = kv.Value.Stable,
                        ParentGroup = groupItem
                    };
                    
                    // Проверяем, отключен ли сервер в hosts файле при инициализации
                    if (isDisabled)
                    {
                        item.LatencyText = "disabled";
                        item.LatencyColor = new SolidColorBrush(Color.FromRgb(0xDC, 0x14, 0x3C)); // Crimson red
                    }
                    
                    if (!kv.Value.Stable)
                    {
                        item.TextColor = new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)); // Warning yellow
                        item.ToolTipText = "Unstable server: latency issues may occur.";
                    }
                    else
                    {
                        item.TextColor = new SolidColorBrush(Colors.White);
                    }
                    
                    ServerItems.Add(item);
                    groupItem.Servers.Add(item);
                }
                
                // Update the select all state for this group
                groupItem.UpdateSelectAllState();
                ServerGroups.Add(groupItem);
            }
            
            // Debug: Check if items are properly added
            System.Diagnostics.Debug.WriteLine($"Added {ServerItems.Count} server items in {ServerGroups.Count} groups");
            foreach (var group in ServerGroups)
            {
                System.Diagnostics.Debug.WriteLine($"Group: {group.GroupName}, Servers: {group.Servers.Count}");
            }
        }

        #endregion

        #region Settings Management

        private void LoadSettings()
        {
            try
            {
                var settings = _settingsService.Load();
                _applyMode = settings.ApplyMode;
                _blockMode = settings.BlockMode;
                _mergeUnstable = settings.MergeUnstable;
                _currentLanguage = string.IsNullOrWhiteSpace(settings.Language) ? "en" : settings.Language;
                ViewModel.ApplyMode = _applyMode;
                ViewModel.BlockMode = _blockMode;
                ViewModel.MergeUnstable = _mergeUnstable;
                ViewModel.Language = _currentLanguage;
                _localizationService.SetLanguage(_currentLanguage);
                ViewModel.NotifyLocalizationChanged();
            }
            catch (Exception ex)
            {
                // Set default language to English on any error
                AppLogger.Error("LoadSettings failed", ex);
                _currentLanguage = "en";
                _localizationService.SetLanguage(_currentLanguage);
                ViewModel.NotifyLocalizationChanged();
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new Models.UserSettings
                {
                    ApplyMode = _applyMode,
                    BlockMode = _blockMode,
                    MergeUnstable = _mergeUnstable,
                    Language = _currentLanguage,
                };
                _settingsService.Save(settings);
                ViewModel.ApplyMode = _applyMode;
                ViewModel.BlockMode = _blockMode;
                ViewModel.MergeUnstable = _mergeUnstable;
                ViewModel.Language = _currentLanguage;
            }
            catch (Exception ex)
            {
                AppLogger.Error("SaveSettings failed", ex);
            }
        }

        #endregion

        #region Ping Management

        private void StartPingTimer()
        {
            _pingTimer = _dispatcherTimerFactory.Create(TimeSpan.FromSeconds(_monitoringOptions.MainPingIntervalSeconds));
            _pingTimer.Tick += async (_, __) => await UpdatePingResults();
            _pingTimer.Start();
        }

        private async Task UpdatePingResults()
        {
            var results = new Dictionary<string, long>();
            
            foreach (var group in ServerGroups)
            {
                foreach (var item in group.Servers)
                {
                    // Сначала проверяем, отключен ли сервер в hosts файле
                    if (IsServerDisabledInHosts(item.RegionKey))
                    {
                        results[item.RegionKey] = -2; // Специальное значение для disabled
                        continue;
                    }

                    long ms;
                    try
                    {
                        var hosts = _regions[item.RegionKey].Hosts;
                        ms = await _networkProbeService.PingAsync(hosts[0], _monitoringOptions.MainPingTimeoutMs);
                    }
                    catch
                    {
                        ms = -1;
                    }
                    results[item.RegionKey] = ms;
                }
            }

            // Update UI
            Dispatcher.Invoke(() =>
            {
                foreach (var group in ServerGroups)
                {
                    foreach (var item in group.Servers)
                    {
                        var ms = results[item.RegionKey];
                        if (ms == -2)
                        {
                            item.LatencyText = "disabled";
                            item.LatencyColor = new SolidColorBrush(Color.FromRgb(0xDC, 0x14, 0x3C)); // Crimson red
                        }
                        else
                        {
                            item.LatencyText = ms >= 0 ? $"{ms} ms" : "disconnected";
                            item.LatencyColor = GetColorForLatency(ms);
                        }
                    }
                }
            });
        }

        private SolidColorBrush GetColorForLatency(long ms)
        {
            if (ms == -2) return new SolidColorBrush(Color.FromRgb(0xDC, 0x14, 0x3C)); // disabled - crimson
            if (ms < 0) return new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)); // disconnected - gray
            if (ms < 80) return new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45)); // good - green
            if (ms < 130) return new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)); // warning - yellow
            if (ms < 250) return new SolidColorBrush(Color.FromRgb(0xDC, 0x14, 0x3C)); // bad - crimson
            return new SolidColorBrush(Color.FromRgb(0x6F, 0x42, 0xC1)); // very bad - purple
        }

        private bool IsServerDisabledInHosts(string regionKey)
        {
            try
            {
                var hosts = _regions[regionKey].Hosts;
                
                // Проверяем, есть ли записи 0.0.0.0 для всех хостов региона
                foreach (var host in hosts)
                {
                    if (!_hostsFileService.IsHostBlocked(host))
                    {
                        return false; // Если хотя бы один хост не заблокирован, сервер не отключен
                    }
                }
                return true; // Все хосты заблокированы
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Event Handlers

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = ServerGroups.SelectMany(g => g.Servers).Where(x => x.IsSelected).ToList();
            
            // Universal Redirect mode
            if (_applyMode == ApplyMode.UniversalRedirect)
            {
                if (selectedItems.Count != 1)
                {
                    ShowWarningToast("Пожалуйста, выберите только один сервер при использовании режима Universal Redirect.");
                    return;
                }

                var regionKey = selectedItems[0].RegionKey;
                var hosts = _regions[regionKey].Hosts;
                var serviceHost = hosts[0];
                var pingHost = hosts.Length > 1 ? hosts[1] : hosts[0];

                // Resolve via DNS lookup
                string svcIp, pingIp;
                try
                {
                    var svcAddrs = _networkProbeService.ResolveHostAddresses(serviceHost);
                    var pingAddrs = _networkProbeService.ResolveHostAddresses(pingHost);
                    if (svcAddrs.Length == 0 || pingAddrs.Length == 0)
                        throw new Exception("DNS lookup returned no addresses");

                    svcIp = svcAddrs[0].ToString();
                    pingIp = pingAddrs[0].ToString();
                }
                catch (Exception ex)
                {
                    ShowErrorToast("Не удалось разрешить IP-адреса для режима Universal Redirect через DNS:\n" + ex.Message);
                    return;
                }

                try
                {
                    var buildResult = _hostsContentBuilder.BuildUniversalRedirect(
                        _regions,
                        _appLinksOptions.DiscordUrl,
                        svcIp,
                        pingIp,
                        GetGroupName,
                        GetGroupDisplayName);
                    WriteWrappedHostsSection(buildResult.Content);
                    FlushDns();
                    _notificationService.ShowSuccess(
                        MainNotificationsChannel,
                        _localizationService.GetString("HostsApplied"));
                }
                catch (UnauthorizedAccessException)
                {
                    ShowWarningToast("Пожалуйста, запустите программу от имени администратора для изменения файла hosts.");
                }
                catch (Exception ex)
                {
                    ShowErrorToast(ex.Message);
                }
                return;
            }

            // Gatekeep mode
            if (selectedItems.Count == 0)
            {
                ShowWarningToast("Пожалуйста, выберите хотя бы один сервер для разрешения.");
                return;
            }

            try
            {
                var selectedRegions = selectedItems.Select(item => item.RegionKey).ToList();
                var orderedRegionKeys = ServerGroups
                    .SelectMany(group => group.Servers)
                    .Select(server => server.RegionKey)
                    .ToList();
                var buildResult = _hostsContentBuilder.BuildGatekeep(
                    _regions,
                    orderedRegionKeys,
                    selectedRegions,
                    _blockMode,
                    _mergeUnstable,
                    _appLinksOptions.DiscordUrl,
                    GetGroupName,
                    GetGroupDisplayName);
                if (!buildResult.Success)
                {
                    ShowErrorToast(buildResult.ErrorMessage ?? "Не удалось подготовить hosts-контент.");
                    return;
                }

                WriteWrappedHostsSection(buildResult.Content);
                FlushDns();
                _notificationService.ShowSuccess(
                    MainNotificationsChannel,
                    _localizationService.GetString("HostsApplied"));
            }
            catch (UnauthorizedAccessException)
            {
                ShowWarningToast("Пожалуйста, запустите программу от имени администратора для изменения файла hosts.");
            }
            catch (Exception ex)
            {
                ShowErrorToast(ex.Message);
            }
        }

        private void RevertButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _hostsFileService.Backup();
                WriteWrappedHostsSection(string.Empty);
                FlushDns();
                _notificationService.ShowSuccess(
                    MainNotificationsChannel,
                    _localizationService.GetString("HostsReverted"));
            }
            catch (UnauthorizedAccessException)
            {
                ShowWarningToast("Пожалуйста, запустите программу от имени администратора для изменения файла hosts.");
            }
            catch (Exception ex)
            {
                ShowErrorToast(ex.Message);
            }
        }

        #endregion

        #region Menu Event Handlers


        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            ShowSettingsDialog();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            ShowAboutDialog();
        }

        private void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            ShowCheckUpdatesDialog();
        }

        #endregion

        #region Helper Methods

        private string GetGroupName(string region)
        {
            return _regionCatalogService.GetGroupName(region);
        }
        
        private string GetGroupDisplayName(string groupName)
        {
            return _regionCatalogService.GetGroupDisplayName(groupName);
        }
        
        private int GetGroupOrder(string groupName)
        {
            return _regionCatalogService.GetGroupOrder(groupName);
        }

        private void UpdateRegionListViewAppearance()
        {
            foreach (var group in ServerGroups)
            {
                foreach (var item in group.Servers)
                {
                    var regionKey = item.RegionKey;
                    if (_mergeUnstable && !_regions[regionKey].Stable)
                    {
                        item.DisplayName = _localizationService.GetServerDisplayName(regionKey, _regions[regionKey].DisplayNameKey);
                        item.TextColor = new SolidColorBrush(Colors.White);
                        item.ToolTipText = string.Empty;
                    }
                    else if (!_regions[regionKey].Stable)
                    {
                        item.DisplayName = _localizationService.GetServerDisplayName(regionKey, _regions[regionKey].DisplayNameKey) + " ⚠︎";
                        item.TextColor = new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)); // Warning yellow
                        item.ToolTipText = "Unstable server: latency issues may occur.";
                    }
                }
            }
        }

        private void WriteWrappedHostsSection(string innerContent)
        {
            var original = _hostsFileService.Read();
            _hostsFileService.Backup();
            var updated = HostsSectionBuilder.Build(original, SectionMarker, innerContent);
            _hostsFileService.Write(updated);
        }

        private void FlushDns()
        {
            _hostsFileService.FlushDns();
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ViewModel.NotifyLocalizationChanged();
                UpdateServerNames();
            });
        }

        private void UpdateServerNames()
        {
            // Update group names
            foreach (var group in ServerGroups)
            {
                var originalGroupName = GetGroupName(group.Servers.FirstOrDefault()?.RegionKey ?? "");
                group.GroupName = _localizationService.GetGroupDisplayName(originalGroupName);
            }

            // Update server names
            foreach (var group in ServerGroups)
            {
                foreach (var item in group.Servers)
                {
                    var translatedName = _localizationService.GetServerDisplayName(item.RegionKey, _regions[item.RegionKey].DisplayNameKey);
                    var isUnstable = !_regions[item.RegionKey].Stable;
                    item.DisplayName = translatedName + (isUnstable ? " ⚠︎" : "");
                }
            }
        }

        private void ShowCheckUpdatesDialog()
        {
            var updateDialog = App.Services.GetRequiredService<UpdateDialog>();
            updateDialog.Owner = this;
            
            // Запускаем проверку обновлений в диалоге
            _ = updateDialog.StartUpdateCheckAsync(CurrentVersion);
            
            _dialogService.ShowUpdateDialog(updateDialog);
        }

        private void ShowAboutDialog()
        {
            var about = App.Services.GetRequiredService<AboutDialog>();
            about.Owner = this;
            about.ViewModel.Configure(_localizationService, CurrentVersion);
            about.Title = about.ViewModel.DialogTitle;
            
            _dialogService.ShowAboutDialog(about);
        }

        private void ShowSettingsDialog()
        {
            var dialog = App.Services.GetRequiredService<SettingsDialog>();
            dialog.Owner = this;
            dialog.ViewModel.InitializeFromSettings(
                _currentLanguage,
                _applyMode == ApplyMode.UniversalRedirect ? "service" : "hosts",
                _blockMode == BlockMode.Both,
                _blockMode == BlockMode.OnlyPing,
                _blockMode == BlockMode.OnlyService,
                _mergeUnstable);

            dialog.ApplyRequested += ApplySettingsResult;
            try
            {
                _dialogService.ShowSettingsDialog(dialog);
            }
            finally
            {
                dialog.ApplyRequested -= ApplySettingsResult;
            }
        }

        private void ApplySettingsResult((string selectedLanguage, ApplyMode applyMode, BlockMode blockMode, bool mergeUnstable) result)
        {
            // Check if language changed
            bool languageChanged = result.selectedLanguage != _currentLanguage;

            // Check if mode changed
            bool modeChanged = result.applyMode != _applyMode;
            bool blockModeChanged = result.blockMode != _blockMode;

            // Check if merge unstable changed
            bool mergeUnstableChanged = result.mergeUnstable != _mergeUnstable;

            // Apply changes
            if (languageChanged)
            {
                _currentLanguage = result.selectedLanguage;
                _localizationService.SetLanguageAndNotify(_currentLanguage);
            }

            if (modeChanged)
            {
                _applyMode = result.applyMode;
                OnPropertyChanged(nameof(ApplyMode));
            }

            if (blockModeChanged)
            {
                _blockMode = result.blockMode;
                OnPropertyChanged(nameof(BlockMode));
            }

            if (mergeUnstableChanged)
            {
                _mergeUnstable = result.mergeUnstable;
                OnPropertyChanged(nameof(_mergeUnstable));
            }

            // Save settings
            SaveSettings();
            ViewModel.NotifyLocalizationChanged();
            UpdateRegionListViewAppearance();
            if (languageChanged)
            {
                UpdateServerNames();
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Cleanup

        protected override void OnClosed(EventArgs e)
        {
            _pingTimer?.Stop();
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
            base.OnClosed(e);
        }

        #endregion

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    yield return result;
                foreach (var childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }

    }

    // Event handlers
    public partial class MainWindow
    {
        private void ServerItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Останавливаем всплытие события, чтобы клик не дошел до Expander
            e.Handled = true;
            
            // Вручную выполняем команду переключения
            if (sender is FrameworkElement element && element.DataContext is ServerItem serverItem)
            {
                serverItem.IsSelected = !serverItem.IsSelected;
            }
        }
        
        private void GroupHeader_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ServerGroupItem groupItem)
            {
                groupItem.IsGroupHovered = true;
                
                // Подсвечиваем все серверы в группе
                UpdateServerItemsBackgroundFromHeader(element, true);
            }
        }
        
        private void GroupHeader_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ServerGroupItem groupItem)
            {
                groupItem.IsGroupHovered = false;
                
                // Убираем подсветку со всех серверов в группе
                UpdateServerItemsBackgroundFromHeader(element, false);
            }
        }
        
        private void UpdateServerItemsBackgroundFromHeader(FrameworkElement header, bool isHovered)
        {
            // Ищем родительский Expander
            var expander = FindParent<Expander>(header);
            if (expander != null)
            {
                // Ищем только внешние Border элементы серверов (с Margin="15,4" и Cursor="Hand")
                var serverBorders = FindVisualChildren<Border>(expander)
                    .Where(b => b.DataContext is ServerItem 
                               && b.Cursor == Cursors.Hand
                               && b.Margin.Left == 15 
                               && b.Margin.Top == 4);
                
                var color = isHovered ? new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)) : Brushes.Transparent;
                
                foreach (var border in serverBorders)
                {
                    border.Background = color;
                }
            }
        }
        
        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);
            
            if (parentObject == null) return null;
            
            if (parentObject is T parent)
                return parent;
            
            return FindParent<T>(parentObject);
        }
        
        private void ServerItem_MouseEnter(object sender, MouseEventArgs e)
        {
            var border = sender as Border;
            if (border == null && sender is FrameworkElement element)
            {
                border = FindVisualChildren<Border>(element)
                    .FirstOrDefault(b => b.Cursor == Cursors.Hand);
            }

            if (border != null)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A));
            }
        }
        
        private void ServerItem_MouseLeave(object sender, MouseEventArgs e)
        {
            var border = sender as Border;
            if (border == null && sender is FrameworkElement element)
            {
                border = FindVisualChildren<Border>(element)
                    .FirstOrDefault(b => b.Cursor == Cursors.Hand);
            }

            if (border != null && border.DataContext is ServerItem serverItem)
            {
                // Проверяем, не hovering ли группа
                if (serverItem.ParentGroup?.IsGroupHovered == true)
                {
                    border.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A));
                }
                else
                {
                    border.Background = Brushes.Transparent;
                }
            }
        }
        
        private void OpenHostsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open hosts file in default text editor
                _externalNavigationService.OpenFile(HostsPath);
            }
            catch (Exception ex)
            {
                ShowErrorToast($"Не удалось открыть файл hosts: {ex.Message}\n\nПуть: {HostsPath}");
            }
        }

        private void ConnectionInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Если окно уже открыто, активируем его
                if (_connectionInfoWindow != null && _connectionInfoWindow.IsLoaded)
                {
                    _connectionInfoWindow.Activate();
                    return;
                }

                // Создаем новое окно
                _connectionInfoWindow = App.Services.GetRequiredService<ConnectionInfoWindow>();

                // Позиционируем окно справа от главного окна
                _connectionInfoWindow.Left = this.Left + this.ActualWidth + 2; // 2px отступ
                _connectionInfoWindow.Top = this.Top;

                // Подписываемся на закрытие окна для очистки ссылки и активации главного окна
                _connectionInfoWindow.Closed += (s, args) => 
                {
                    _connectionInfoWindow = null;
                    // Активируем главное окно при закрытии дочернего
                    this.Activate();
                };

                _connectionInfoWindow.Show();
            }
            catch (Exception ex)
            {
                ShowErrorToast($"Не удалось открыть окно информации о подключении: {ex.Message}");
            }
        }

        private void ShowWarningToast(string message)
        {
            _notificationService.ShowWarning(MainNotificationsChannel, message);
        }

        private void ShowErrorToast(string message)
        {
            _notificationService.ShowError(MainNotificationsChannel, message);
        }
    }
}