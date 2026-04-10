using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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
            typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "1.0.3";
        
        private readonly IReadOnlyDictionary<string, RegionDefinition> _regions;

        private DispatcherTimer? _pingTimer;
        private ConnectionInfoWindow? _connectionInfoWindow;

        private readonly ISettingsService _settingsService;
        private readonly IHostsService _hostsService;
        private readonly ILatencyService _latencyService;
        private readonly IDialogService _dialogService;
        private readonly IRegionCatalogService _regionCatalogService;
        private readonly IMessageService _messageService;
        private readonly IExternalNavigationService _externalNavigationService;
        private readonly AppLinksOptions _appLinksOptions;
        private readonly MonitoringOptions _monitoringOptions;
        
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
        public MainWindowViewModel ViewModel { get; }

        #endregion

        #region Constructor and Initialization

        public MainWindow(
            ISettingsService settingsService,
            IHostsService hostsService,
            ILatencyService latencyService,
            IDialogService dialogService,
            IRegionCatalogService regionCatalogService,
            IMessageService messageService,
            IExternalNavigationService externalNavigationService,
            IOptions<AppLinksOptions> appLinksOptions,
            IOptions<MonitoringOptions> monitoringOptions)
        {
            _settingsService = settingsService;
            _hostsService = hostsService;
            _latencyService = latencyService;
            _dialogService = dialogService;
            _regionCatalogService = regionCatalogService;
            _messageService = messageService;
            _externalNavigationService = externalNavigationService;
            _appLinksOptions = appLinksOptions.Value;
            _monitoringOptions = monitoringOptions.Value;
            _regions = _regionCatalogService.Regions;

            ViewModel = new MainWindowViewModel(
                ServerItems,
                ServerGroups,
                () => ApplyButton_Click(this, new RoutedEventArgs()),
                () => RevertButton_Click(this, new RoutedEventArgs()),
                () => Settings_Click(this, new RoutedEventArgs()),
                () => About_Click(this, new RoutedEventArgs()),
                () => CheckUpdates_Click(this, new RoutedEventArgs()),
                () => OpenHostsButton_Click(this, new RoutedEventArgs()),
                () => ConnectionInfo_Click(this, new RoutedEventArgs()));
            InitializeComponent();
            DataContext = ViewModel;
            LoadSettings();
            
            // Force update UI after loading settings to ensure proper language display
            UpdateUI();
            UpdateStaticBindingElements();
            
            InitializeApplication();
            
            // Subscribe to language change events
            LocalizationManager.LanguageChanged += OnLanguageChanged;
            LocalizationManager.PropertyChanged += OnLocalizationPropertyChanged;
        }

        private void InitializeApplication()
        {
            InitializeServerList();
            StartPingTimer();
            
            UpdateRegionListViewAppearance();
            UpdateUI();
            
            // Final UI update to ensure all static bindings are properly updated
            UpdateStaticBindingElements();
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
                    GroupName = LocalizationManager.GetGroupDisplayName(group.Key),
                    IsExpanded = true
                };
                
                foreach (var kv in group.OrderBy(x => x.Key))
                {
                    var regionKey = kv.Key;
                    var translatedName = LocalizationManager.GetServerDisplayName(regionKey, kv.Value.DisplayNameKey);
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
                LocalizationManager.SetLanguage(_currentLanguage);

                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateUI();
                    UpdateStaticBindingElements();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                // Set default language to English on any error
                AppLogger.Error("LoadSettings failed", ex);
                _currentLanguage = "en";
                LocalizationManager.SetLanguage(_currentLanguage);
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
            _pingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_monitoringOptions.MainPingIntervalSeconds) };
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
                        ms = await _latencyService.PingAsync(hosts[0], _monitoringOptions.MainPingTimeoutMs);
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
                    if (!_hostsService.IsHostBlocked(host))
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
                    _messageService.Show(
                        "Пожалуйста, выберите только один сервер при использовании режима Universal Redirect.",
                        "Universal Redirect",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
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
                    var svcAddrs = Dns.GetHostAddresses(serviceHost);
                    var pingAddrs = Dns.GetHostAddresses(pingHost);
                    if (svcAddrs.Length == 0 || pingAddrs.Length == 0)
                        throw new Exception("DNS lookup returned no addresses");

                    svcIp = svcAddrs[0].ToString();
                    pingIp = pingAddrs[0].ToString();
                }
                catch (Exception ex)
                {
                    _messageService.Show(
                        "Не удалось разрешить IP-адреса для режима Universal Redirect через DNS:\n" + ex.Message,
                        "Ошибка Universal Redirect",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                try
                {
                    _hostsService.Backup();

                    var sb = new StringBuilder();
                    sb.AppendLine("# Edited by Ping by Daylight");
                    sb.AppendLine("# Universal Redirect mode: redirect all GameLift endpoints to selected region");
                    sb.AppendLine($"# Need help? Discord: {_appLinksOptions.DiscordUrl}");
                    sb.AppendLine();

                    string currentGroup = "";
                    foreach (var kv in _regions)
                    {
                        var groupName = GetGroupName(kv.Key);
                        if (groupName != currentGroup)
                        {
                            sb.AppendLine($"# {GetGroupDisplayName(groupName)}");
                            currentGroup = groupName;
                        }
                        
                        var regionHosts = kv.Value.Hosts;
                        foreach (var h in regionHosts)
                        {
                            bool isPing = h.Contains("ping", StringComparison.OrdinalIgnoreCase);
                            var ip = isPing ? pingIp : svcIp;
                            sb.AppendLine($"{ip} {h}");
                        }
                        sb.AppendLine();
                    }

                    WriteWrappedHostsSection(sb.ToString());
                    FlushDns();
                    _messageService.Show(
                        "Файл hosts был успешно обновлен (Universal Redirect).\n\nПожалуйста, перезапустите игру, чтобы изменения вступили в силу.",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (UnauthorizedAccessException)
                {
                    _messageService.Show(
                        "Пожалуйста, запустите программу от имени администратора для изменения файла hosts.",
                        "Доступ запрещен",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    _messageService.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }

            // Gatekeep mode
            if (selectedItems.Count == 0)
            {
                _messageService.Show(
                    "Пожалуйста, выберите хотя бы один сервер для разрешения.",
                    "Сервер не выбран",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                _hostsService.Backup();

                var selectedRegions = selectedItems.Select(item => item.RegionKey).ToList();
                bool anyStableSelected = selectedRegions.Any(regionKey => _regions[regionKey].Stable);

                // Merge unstable servers logic
                var allowedSet = new HashSet<string>(selectedRegions);
                if (_mergeUnstable && !anyStableSelected)
                {
                    var missing = new List<string>();
                    foreach (var region in selectedRegions)
                    {
                        if (!_regions[region].Stable)
                        {
                            var group = GetGroupName(region);
                            bool stableExists = _regions.Any(kv => GetGroupName(kv.Key) == group && kv.Value.Stable);
                            if (!stableExists)
                                missing.Add(region);
                        }
                    }
                    if (missing.Count > 0)
                    {
                        _messageService.Show(
                            "Опция объединения нестабильных серверов включена, но стабильные серверы не найдены для: " +
                            string.Join(", ", missing) + ".\nОтключите объединение нестабильных серверов в меню настроек или выберите стабильный сервер вручную.",
                            "Стабильные серверы не найдены",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }
                }

                if (_mergeUnstable && !anyStableSelected)
                {
                    var additional = new List<string>();
                    foreach (var region in allowedSet.ToList())
                    {
                        if (!_regions[region].Stable)
                        {
                            var group = GetGroupName(region);
                            var alternative = _regions.FirstOrDefault(kv => GetGroupName(kv.Key) == group && kv.Value.Stable);
                            if (!string.IsNullOrEmpty(alternative.Key))
                                additional.Add(alternative.Key);
                        }
                    }
                    foreach (var extra in additional)
                        allowedSet.Add(extra);
                }

                var sb = new StringBuilder();
                sb.AppendLine("# Edited by Ping by Daylight");
                sb.AppendLine("# Unselected servers are blocked (Gatekeep Mode); selected servers are commented out.");
                sb.AppendLine($"# Need help? Discord: {_appLinksOptions.DiscordUrl}");
                sb.AppendLine();

                string currentGroup = "";
                foreach (var group in ServerGroups)
                {
                    foreach (var item in group.Servers)
                    {
                        var regionKey = item.RegionKey;
                        var groupName = GetGroupName(regionKey);
                        if (groupName != currentGroup)
                        {
                            sb.AppendLine($"# {GetGroupDisplayName(groupName)}");
                            currentGroup = groupName;
                        }
                        
                        bool allow = allowedSet.Contains(regionKey);
                        var hosts = _regions[regionKey].Hosts;
                        foreach (var h in hosts)
                        {
                            bool isPing = h.Contains("ping", StringComparison.OrdinalIgnoreCase);
                            bool include = _blockMode == BlockMode.Both
                                           || (_blockMode == BlockMode.OnlyPing && isPing)
                                           || (_blockMode == BlockMode.OnlyService && !isPing);
                            if (!include)
                                continue;
                            var prefix = allow ? "#" : "0.0.0.0".PadRight(9);
                            sb.AppendLine($"{prefix} {h}");
                        }
                        sb.AppendLine();
                    }
                }

                WriteWrappedHostsSection(sb.ToString());
                FlushDns();
                _messageService.Show(
                    "Файл hosts был успешно обновлен (Gatekeep).\n\nПожалуйста, перезапустите игру, чтобы изменения вступили в силу.",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (UnauthorizedAccessException)
            {
                _messageService.Show(
                    "Пожалуйста, запустите программу от имени администратора для изменения файла hosts.",
                    "Доступ запрещен",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                _messageService.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RevertButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _hostsService.Backup();
                WriteWrappedHostsSection(string.Empty);
                FlushDns();
                _messageService.Show(
                    "Записи AWS Realms очищены. Ваши существующие строки hosts остались нетронутыми.",
                    "Отменено",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (UnauthorizedAccessException)
            {
                _messageService.Show(
                    "Пожалуйста, запустите программу от имени администратора для изменения файла hosts.",
                    "Доступ запрещен",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                _messageService.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        item.DisplayName = LocalizationManager.GetServerDisplayName(regionKey, _regions[regionKey].DisplayNameKey);
                        item.TextColor = new SolidColorBrush(Colors.White);
                        item.ToolTipText = string.Empty;
                    }
                    else if (!_regions[regionKey].Stable)
                    {
                        item.DisplayName = LocalizationManager.GetServerDisplayName(regionKey, _regions[regionKey].DisplayNameKey) + " ⚠︎";
                        item.TextColor = new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)); // Warning yellow
                        item.ToolTipText = "Unstable server: latency issues may occur.";
                    }
                }
            }
        }

        private void WriteWrappedHostsSection(string innerContent)
        {
            var original = _hostsService.Read();
            _hostsService.Backup();
            var updated = HostsSectionBuilder.Build(original, SectionMarker, innerContent);
            _hostsService.Write(updated);
        }

        private void FlushDns()
        {
            _hostsService.FlushDns();
        }

        private void UpdateUI()
        {
            Title = LocalizationManager.GetString("AppTitle");
            
            // Update XAML elements that use static bindings
            var statusTextElement = this.FindName("StatusText") as System.Windows.Controls.TextBlock;
            if (statusTextElement != null)
                statusTextElement.Text = LocalizationManager.GetString("StatusText");
                
            // Force update of window title
            OnPropertyChanged(nameof(Title));
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            // Update UI elements that are not bound to static properties
            Dispatcher.Invoke(() =>
            {
                UpdateUI();
                UpdateServerNames();
                UpdateXAMLBindings();
                // Force refresh of XAML bindings
                CommandManager.InvalidateRequerySuggested();
                
                // Force update of all server groups and items
                foreach (var group in ServerGroups)
                {
                    group.OnPropertyChanged(nameof(ServerGroupItem.GroupName));
                    foreach (var server in group.Servers)
                    {
                        server.OnPropertyChanged(nameof(ServerItem.DisplayName));
                    }
                }
            });
        }

        private void OnLocalizationPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Force refresh of all XAML bindings when localization properties change
            Dispatcher.Invoke(() =>
            {
                UpdateUI();
                UpdateServerNames();
                UpdateXAMLBindings();
                CommandManager.InvalidateRequerySuggested();
                
                // Force update of all server groups and items
                foreach (var group in ServerGroups)
                {
                    group.OnPropertyChanged(nameof(ServerGroupItem.GroupName));
                    foreach (var server in group.Servers)
                    {
                        server.OnPropertyChanged(nameof(ServerItem.DisplayName));
                    }
                }
            });
        }

        private void UpdateXAMLBindings()
        {
            // Force update of XAML bindings by refreshing the DataContext
            var currentContext = DataContext;
            DataContext = null;
            DataContext = currentContext;
            
            // Force update of all static bindings by invalidating them
            CommandManager.InvalidateRequerySuggested();
            
            // Force refresh of all XAML elements that use static bindings
            this.InvalidateVisual();
            
            // Force update of specific XAML elements that use static bindings
            UpdateStaticBindingElements();
        }
        
        private void UpdateStaticBindingElements()
        {
            // Find and update elements that use static bindings
            var selectServersText = this.FindName("SelectServersText") as System.Windows.Controls.TextBlock;
            if (selectServersText == null)
            {
                // Try to find the TextBlock in the header
                var headerBorder = this.FindName("ServerListHeader") as Border;
                if (headerBorder != null)
                {
                    var grid = headerBorder.Child as Grid;
                    if (grid != null && grid.Children.Count > 0)
                    {
                        selectServersText = grid.Children[0] as System.Windows.Controls.TextBlock;
                    }
                }
            }
            
            if (selectServersText != null)
            {
                selectServersText.Text = LocalizationManager.GetString("SelectServers");
            }
            
            // Update latency text
            var latencyText = this.FindName("LatencyText") as System.Windows.Controls.TextBlock;
            if (latencyText == null)
            {
                // Try to find the TextBlock in the header
                var headerBorder = this.FindName("ServerListHeader") as Border;
                if (headerBorder != null)
                {
                    var grid = headerBorder.Child as Grid;
                    if (grid != null && grid.Children.Count > 2)
                    {
                        latencyText = grid.Children[2] as System.Windows.Controls.TextBlock;
                    }
                }
            }
            
            if (latencyText != null)
            {
                latencyText.Text = LocalizationManager.GetString("Latency");
            }
            
            // Update menu items
            var settingsMenuItem = this.FindName("SettingsMenuItem") as MenuItem;
            if (settingsMenuItem != null)
            {
                settingsMenuItem.Header = LocalizationManager.GetString("Settings");
            }
            
            var aboutMenuItem = this.FindName("AboutMenuItem") as MenuItem;
            if (aboutMenuItem != null)
            {
                aboutMenuItem.Header = LocalizationManager.GetString("About");
            }
            
            var checkUpdatesMenuItem = this.FindName("CheckUpdatesMenuItem") as MenuItem;
            if (checkUpdatesMenuItem != null)
            {
                checkUpdatesMenuItem.Header = LocalizationManager.GetString("CheckUpdates");
            }
            
            // Update menu items
            var openHostsMenuItem = this.FindName("OpenHostsMenuItem") as MenuItem;
            if (openHostsMenuItem != null)
            {
                openHostsMenuItem.Header = LocalizationManager.GetString("OpenHosts");
            }
            
            var connectionInfoMenuItem = this.FindName("ConnectionInfoMenuItem") as MenuItem;
            if (connectionInfoMenuItem != null)
            {
                connectionInfoMenuItem.Header = LocalizationManager.GetString("ConnectionInfo");
            }
            
            var revertButton = this.FindName("RevertButton") as Button;
            if (revertButton != null)
            {
                revertButton.Content = LocalizationManager.GetString("ResetToDefault");
            }
            
            var applyButton = this.FindName("ApplyButton") as Button;
            if (applyButton != null)
            {
                applyButton.Content = LocalizationManager.GetString("ApplySelection");
            }
        }

        private void UpdateServerNames()
        {
            // Update group names
            foreach (var group in ServerGroups)
            {
                var originalGroupName = GetGroupName(group.Servers.FirstOrDefault()?.RegionKey ?? "");
                group.GroupName = LocalizationManager.GetGroupDisplayName(originalGroupName);
            }

            // Update server names
            foreach (var group in ServerGroups)
            {
                foreach (var item in group.Servers)
                {
                    var translatedName = LocalizationManager.GetServerDisplayName(item.RegionKey, _regions[item.RegionKey].DisplayNameKey);
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
            updateDialog.StartUpdateCheck(CurrentVersion);
            
            _dialogService.ShowUpdateDialog(updateDialog);
        }

        private void ShowAboutDialog()
        {
            var about = App.Services.GetRequiredService<AboutDialog>();
            about.Owner = this;
            about.Title = LocalizationManager.GetString("AboutTitle");
            about.AboutText = LocalizationManager.GetString("AboutText");
            about.Developer = LocalizationManager.GetString("Developer");
            about.VersionText = LocalizationManager.GetString("Version", CurrentVersion);
            about.AwesomeText = LocalizationManager.GetString("Awesome");
            
            _dialogService.ShowAboutDialog(about);
        }

        private void ShowSettingsDialog()
        {
            var dialog = App.Services.GetRequiredService<SettingsDialog>();
            dialog.Owner = this;
            dialog.InitializeFromSettings(
                _currentLanguage,
                _applyMode == ApplyMode.UniversalRedirect ? "service" : "hosts",
                _blockMode == BlockMode.Both,
                _blockMode == BlockMode.OnlyPing,
                _blockMode == BlockMode.OnlyService,
                _mergeUnstable);
            
            if (_dialogService.ShowSettingsDialog(dialog) == true)
            {
                // Check if language changed
                bool languageChanged = dialog.SelectedLanguage != _currentLanguage;
                
                // Check if mode changed
                bool modeChanged = dialog.SelectedMode != (_applyMode == ApplyMode.UniversalRedirect ? "service" : "hosts");
                
                // Check if block mode changed
                BlockMode newBlockMode = BlockMode.Both;
                if (dialog.IsBlockPing) newBlockMode = BlockMode.OnlyPing;
                else if (dialog.IsBlockService) newBlockMode = BlockMode.OnlyService;
                bool blockModeChanged = newBlockMode != _blockMode;
                
                // Check if merge unstable changed
                bool mergeUnstableChanged = dialog.IsMergeUnstable != _mergeUnstable;
                
                // Apply changes
                if (languageChanged)
                {
                    _currentLanguage = dialog.SelectedLanguage;
                    LocalizationManager.SetLanguageAndNotify(_currentLanguage);
                }
                
                if (modeChanged)
                {
                    _applyMode = dialog.SelectedMode == "service" ? ApplyMode.UniversalRedirect : ApplyMode.Gatekeep;
                    OnPropertyChanged(nameof(ApplyMode));
                }
                
                if (blockModeChanged)
                {
                    _blockMode = newBlockMode;
                    OnPropertyChanged(nameof(BlockMode));
                }
                
                if (mergeUnstableChanged)
                {
                    _mergeUnstable = dialog.IsMergeUnstable;
                    OnPropertyChanged(nameof(_mergeUnstable));
                }
                
                // Save settings
                SaveSettings();
                
                // Update UI if language changed
                if (languageChanged)
                {
                    UpdateUI();
                }
                
                SaveSettings();
                UpdateRegionListViewAppearance();
                UpdateUI();
            }
        }

        private void RestoreWindowsDefaultHostsFile()
        {
            var confirm = _messageService.Show(
                "Если у вас возникли проблемы или программа не работает корректно, попробуйте сбросить файл hosts.\n\nЭто перезапишет весь ваш файл hosts значениями по умолчанию Windows.\n\nРезервная копия будет сохранена как hosts.bak. Продолжить?",
                "Восстановить файл hosts по умолчанию Windows",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                _hostsService.Backup();

                var defaultHosts = _hostsService.ReadDefaultTemplate();
                _hostsService.Write(defaultHosts);
                FlushDns();

                _messageService.Show(
                    "Файл hosts восстановлен до шаблона по умолчанию Windows.",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (UnauthorizedAccessException)
            {
                _messageService.Show(
                    "Пожалуйста, запустите программу от имени администратора для изменения файла hosts.",
                    "Доступ запрещен",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                _messageService.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
            LocalizationManager.PropertyChanged -= OnLocalizationPropertyChanged;
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
                _messageService.Show(
                    $"Не удалось открыть файл hosts: {ex.Message}\n\nПуть: {HostsPath}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
                _messageService.Show(
                    $"Не удалось открыть окно информации о подключении: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}