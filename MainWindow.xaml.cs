using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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

namespace AWSServerSelector
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Constants and Fields
        
        private const string RepoUrl = "https://codeberg.org/ky/make-your-choice";
        private const string WebsiteUrl = "https://kurocat.net";
        private const string DiscordUrl = "https://discord.gg/gnvtATeVc4";
        private const string CurrentVersion = "1.0.0";
        
        // Holds endpoint list and stability flag for each region
        private record RegionInfo(string[] Hosts, bool Stable);
        private readonly Dictionary<string, RegionInfo> _regions = new()
        {
            // Europe
            { "Europe (London)",            new RegionInfo(new[]{ "gamelift.eu-west-2.amazonaws.com",    "gamelift-ping.eu-west-2.api.aws" }, false) },
            { "Europe (Ireland)",           new RegionInfo(new[]{ "gamelift.eu-west-1.amazonaws.com",    "gamelift-ping.eu-west-1.api.aws" }, true) },
            { "Europe (Frankfurt am Main)", new RegionInfo(new[]{ "gamelift.eu-central-1.amazonaws.com", "gamelift-ping.eu-central-1.api.aws" }, true) },

            // The Americas
            { "US East (N. Virginia)",      new RegionInfo(new[]{ "gamelift.us-east-1.amazonaws.com",    "gamelift-ping.us-east-1.api.aws" }, true) },
            { "US East (Ohio)",             new RegionInfo(new[]{ "gamelift.us-east-2.amazonaws.com",    "gamelift-ping.us-east-2.api.aws" }, false) },
            { "US West (N. California)",    new RegionInfo(new[]{ "gamelift.us-west-1.amazonaws.com",    "gamelift-ping.us-west-1.api.aws" }, true) },
            { "US West (Oregon)",           new RegionInfo(new[]{ "gamelift.us-west-2.amazonaws.com",    "gamelift-ping.us-west-2.api.aws" }, true) },
            { "Canada (Central)",           new RegionInfo(new[]{ "gamelift.ca-central-1.amazonaws.com", "gamelift-ping.ca-central-1.api.aws" }, false) },
            { "South America (São Paulo)",  new RegionInfo(new[]{ "gamelift.sa-east-1.amazonaws.com",   "gamelift-ping.sa-east-1.api.aws" }, true) },

            // Asia (excluding Mainland China)
            { "Asia Pacific (Tokyo)",       new RegionInfo(new[]{ "gamelift.ap-northeast-1.amazonaws.com","gamelift-ping.ap-northeast-1.api.aws" }, true) },
            { "Asia Pacific (Seoul)",       new RegionInfo(new[]{ "gamelift.ap-northeast-2.amazonaws.com","gamelift-ping.ap-northeast-2.api.aws" }, true) },
            { "Asia Pacific (Mumbai)",      new RegionInfo(new[]{ "gamelift.ap-south-1.amazonaws.com",   "gamelift-ping.ap-south-1.api.aws" }, true) },
            { "Asia Pacific (Singapore)",   new RegionInfo(new[]{ "gamelift.ap-southeast-1.amazonaws.com","gamelift-ping.ap-southeast-1.api.aws" }, true) },
            { "Asia Pacific (Hong Kong)",   new RegionInfo(new[]{ "ec2.ap-east-1.amazonaws.com","gamelift-ping.ap-east-1.api.aws" }, true) },

            // Oceania
            { "Asia Pacific (Sydney)",      new RegionInfo(new[]{ "gamelift.ap-southeast-2.amazonaws.com","gamelift-ping.ap-southeast-2.api.aws" }, true) },

            // Mainland China
            { "China (Beijing)",            new RegionInfo(new[]{ "gamelift.cn-north-1.amazonaws.com.cn" }, true) },
            { "China (Ningxia)",            new RegionInfo(new[]{ "gamelift.cn-northwest-1.amazonaws.com.cn" }, true) },
        };

        private DispatcherTimer? _pingTimer;
        private Ping? _pinger;
        
        public enum ApplyMode { Gatekeep, UniversalRedirect }
        public enum BlockMode { Both, OnlyPing, OnlyService }
        
        private ApplyMode _applyMode = ApplyMode.Gatekeep;
        private BlockMode _blockMode = BlockMode.Both;
        private bool _mergeUnstable = true;

        // Path for saving user settings
        private static string SettingsFilePath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Tigerbyte",
                "MakeYourChoice",
                "settings.json");

        // Hosts file section marker and path
        private const string SectionMarker = "# --+ Make Your Choice +--";
        private static string HostsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "drivers\\etc\\hosts");

        private class UserSettings
        {
            public ApplyMode ApplyMode { get; set; }
            public BlockMode BlockMode { get; set; }
            public bool MergeUnstable { get; set; } = true;
        }


        public ObservableCollection<ServerItem> ServerItems { get; set; } = new();
        public ObservableCollection<ServerGroupItem> ServerGroups { get; set; } = new();

        #endregion

        #region Constructor and Initialization

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            InitializeApplication();
        }

        private async void InitializeApplication()
        {
            LoadSettings();
            InitializeServerList();
            StartPingTimer();
            
            UpdateRegionListViewAppearance();
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
                    GroupName = GetGroupDisplayName(group.Key),
                    IsExpanded = true
                };
                
                foreach (var kv in group.OrderBy(x => x.Key))
                {
                    var regionKey = kv.Key;
                    var displayName = regionKey + (kv.Value.Stable ? string.Empty : " ⚠︎");
                    var isDisabled = IsServerDisabledInHosts(regionKey);
                    
                    var item = new ServerItem
                    {
                        RegionKey = regionKey,
                        DisplayName = displayName,
                        IsSelected = !isDisabled, // Выбираем все серверы, кроме отключенных в hosts
                        LatencyText = "…",
                        IsStable = kv.Value.Stable
                    };
                    
                    // Проверяем, отключен ли сервер в hosts файле при инициализации
                    if (isDisabled)
                    {
                        item.LatencyText = "disabled";
                        item.LatencyColor = new SolidColorBrush(Colors.DarkRed);
                    }
                    
                    if (!kv.Value.Stable)
                    {
                        item.TextColor = new SolidColorBrush(Colors.Orange);
                        item.ToolTipText = "Unstable server: latency issues may occur.";
                    }
                    else
                    {
                        item.TextColor = new SolidColorBrush(Colors.White);
                    }
                    
                    ServerItems.Add(item);
                    groupItem.Servers.Add(item);
                }
                
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
                var folder = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(folder))
                    return;
                if (!File.Exists(SettingsFilePath))
                    return;
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<UserSettings>(json);
                if (settings != null)
                {
                    _applyMode = settings.ApplyMode;
                    _blockMode = settings.BlockMode;
                    _mergeUnstable = settings.MergeUnstable;
                }
            }
            catch
            {
                // ignore load errors
            }
        }

        private void SaveSettings()
        {
            try
            {
                var folder = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                var settings = new UserSettings
                {
                    ApplyMode = _applyMode,
                    BlockMode = _blockMode,
                    MergeUnstable = _mergeUnstable,
                };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // ignore save errors
            }
        }

        #endregion

        #region Ping Management

        private void StartPingTimer()
        {
            _pinger = new Ping();
            _pingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
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
                        var reply = await _pinger.SendPingAsync(hosts[0], 2000);
                        ms = reply.Status == IPStatus.Success ? reply.RoundtripTime : -1;
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
                            item.LatencyColor = new SolidColorBrush(Colors.DarkRed);
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
            if (ms == -2) return new SolidColorBrush(Colors.DarkRed); // disabled
            if (ms < 0) return new SolidColorBrush(Colors.Gray); // disconnected
            if (ms < 80) return new SolidColorBrush(Colors.Green);
            if (ms < 130) return new SolidColorBrush(Colors.Orange);
            if (ms < 250) return new SolidColorBrush(Colors.Crimson);
            return new SolidColorBrush(Colors.Purple);
        }

        private bool IsServerDisabledInHosts(string regionKey)
        {
            try
            {
                if (!File.Exists(HostsPath))
                    return false;

                var hostsContent = File.ReadAllText(HostsPath);
                var hosts = _regions[regionKey].Hosts;
                
                // Проверяем, есть ли записи 0.0.0.0 для всех хостов региона
                foreach (var host in hosts)
                {
                    // Ищем строки вида "0.0.0.0 hostname" (не закомментированные)
                    var pattern = $"^0\\.0\\.0\\.0\\s+{Regex.Escape(host)}\\s*$";
                    if (!Regex.IsMatch(hostsContent, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase))
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

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = ServerGroups.SelectMany(g => g.Servers).Where(x => x.IsSelected).ToList();
            
            // Universal Redirect mode
            if (_applyMode == ApplyMode.UniversalRedirect)
            {
                if (selectedItems.Count != 1)
                {
                    MessageBox.Show(
                        "Please select only one server when using Universal Redirect mode.",
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
                    MessageBox.Show(
                        "Failed to resolve IP addresses for Universal Redirect mode via DNS:\n" + ex.Message,
                        "Universal Redirect Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                try
                {
                    File.Copy(HostsPath, HostsPath + ".bak", true);

                    var sb = new StringBuilder();
                    sb.AppendLine("# Edited by AWS Realms (AWS Server Selector)");
                    sb.AppendLine("# Universal Redirect mode: redirect all GameLift endpoints to selected region");
                    sb.AppendLine($"# Need help? Discord: {DiscordUrl}");
                    sb.AppendLine();

                    foreach (var kv in _regions)
                    {
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
                    MessageBox.Show(
                        "The hosts file was updated successfully (Universal Redirect).\n\nPlease restart the game in order for changes to take effect.",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show(
                        "Please run as Administrator to modify the hosts file.",
                        "Permission Denied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }

            // Gatekeep mode
            if (selectedItems.Count == 0)
            {
                MessageBox.Show(
                    "Please select at least one server to allow.",
                    "No Server Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                File.Copy(HostsPath, HostsPath + ".bak", true);

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
                        MessageBox.Show(
                            "Merge unstable servers option is enabled, but no stable servers found for: " +
                            string.Join(", ", missing) + ".\nDisable merging unstable servers in the options menu or select a stable server manually.",
                            "No Stable Servers Found",
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
                sb.AppendLine("# Edited by AWS Realms (AWS Server Selector)");
                sb.AppendLine("# Unselected servers are blocked (Gatekeep Mode); selected servers are commented out.");
                sb.AppendLine($"# Need help? Discord: {DiscordUrl}");
                sb.AppendLine();

                foreach (var group in ServerGroups)
                {
                    foreach (var item in group.Servers)
                    {
                        var regionKey = item.RegionKey;
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
                MessageBox.Show(
                    "The hosts file was updated successfully (Gatekeep).\n\nPlease restart the game in order for changes to take effect.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Please run as Administrator to modify the hosts file.",
                    "Permission Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RevertButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                File.Copy(HostsPath, HostsPath + ".bak", true);
                WriteWrappedHostsSection(string.Empty);
                FlushDns();
                MessageBox.Show(
                    "Cleared AWS Realms entries. Your existing hosts lines were left untouched.",
                    "Reverted",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Please run as Administrator to modify the hosts file.",
                    "Permission Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Menu Event Handlers


        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            ShowSettingsDialog();
        }

        private void Repository_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(RepoUrl);
        }

        private void Website_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(WebsiteUrl);
        }

        private void Discord_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(DiscordUrl);
        }

        private void OpenHostsLocation_Click(object sender, RoutedEventArgs e)
        {
            var hostsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "drivers\\etc");
            Process.Start(new ProcessStartInfo("explorer.exe", hostsFolder)
            {
                UseShellExecute = true
            });
        }

        private void ResetHosts_Click(object sender, RoutedEventArgs e)
        {
            RestoreWindowsDefaultHostsFile();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            ShowAboutDialog();
        }

        #endregion

        #region Helper Methods

        private string GetGroupName(string region)
        {
            if (region.StartsWith("Europe")) return "Europe";
            if (region.StartsWith("US") || region.StartsWith("Canada") || region.StartsWith("South America"))
                return "Americas";
            if (region.Contains("Sydney")) return "Oceania";
            if (region.Contains("China")) return "China";
            return "Asia";
        }
        
        private string GetGroupDisplayName(string groupName)
        {
            return groupName switch
            {
                "Europe" => "🌍 Europe",
                "Americas" => "🌎 The Americas", 
                "Asia" => "🌏 Asia (excluding Mainland China)",
                "Oceania" => "🌊 Oceania",
                "China" => "🇨🇳 Mainland China",
                _ => groupName
            };
        }
        
        private int GetGroupOrder(string groupName)
        {
            return groupName switch
            {
                "Europe" => 1,
                "Americas" => 2,
                "Asia" => 3,
                "Oceania" => 4,
                "China" => 5,
                _ => 6
            };
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
                        item.DisplayName = regionKey;
                        item.TextColor = new SolidColorBrush(Colors.White);
                        item.ToolTipText = string.Empty;
                    }
                    else if (!_regions[regionKey].Stable)
                    {
                        item.DisplayName = regionKey + " ⚠︎";
                        item.TextColor = new SolidColorBrush(Colors.Orange);
                        item.ToolTipText = "Unstable server: latency issues may occur.";
                    }
                }
            }
        }

        private void WriteWrappedHostsSection(string innerContent)
        {
            string NormalizeToLf(string s) => s.Replace("\r\n", "\n").Replace("\r", "\n");

            string original = string.Empty;
            try { original = File.ReadAllText(HostsPath); } catch { /* ignore */ }

            string lf = NormalizeToLf(original);
            int first = lf.IndexOf(SectionMarker, StringComparison.Ordinal);
            int last = first >= 0 ? lf.IndexOf(SectionMarker, first + SectionMarker.Length, StringComparison.Ordinal) : -1;

            string innerLf = NormalizeToLf(innerContent ?? string.Empty);
            if (innerLf.Length > 0 && !innerLf.EndsWith("\n")) innerLf += "\n";
            string wrapped = SectionMarker + "\n" + innerLf + SectionMarker + "\n";

            string newLf;
            if (first >= 0 && last >= 0)
            {
                int afterLast = last + SectionMarker.Length;
                newLf = lf.Substring(0, first) + wrapped + lf.Substring(afterLast);
            }
            else if (first >= 0 && last < 0)
            {
                newLf = lf.Substring(0, first) + wrapped;
            }
            else
            {
                string suffix = (lf.EndsWith("\n") ? "\n" : "\n") + "\n" + wrapped;
                newLf = lf + suffix;
            }

            try { File.Copy(HostsPath, HostsPath + ".bak", true); } catch { /* ignore */ }
            try { File.WriteAllText(HostsPath, newLf.Replace("\n", "\r\n")); } catch { throw; }
        }

        private void FlushDns()
        {
            try
            {
                var psi = new ProcessStartInfo("ipconfig", "/flushdns")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (var proc = Process.Start(psi))
                {
                    proc.WaitForExit();
                }
            }
            catch { /* ignore */ }
        }

        private void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }


        private void ShowAboutDialog()
        {
            var about = new Window
            {
                Title = "About AWS Realms",
                Width = 500,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(0x0D, 0x11, 0x17))
            };

            var panel = new StackPanel { Margin = new Thickness(20) };
            
            var title = new TextBlock
            {
                Text = "AWS Realms - AWS Server Selector",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            var developer = new TextBlock
            {
                Text = "Developer: Ky",
                FontSize = 12,
                Foreground = Brushes.LightGray,
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            var version = new TextBlock
            {
                Text = $"Version {CurrentVersion}\nModern WPF interface with AWS server selection\nWindows 10 or higher recommended.",
                FontSize = 11,
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.LightGray,
                Margin = new Thickness(0, 0, 0, 20)
            };
            
            var okButton = new Button
            {
                Content = "Awesome!",
                Width = 100,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            okButton.Click += (s, e) => about.Close();
            
            panel.Children.Add(title);
            panel.Children.Add(developer);
            panel.Children.Add(version);
            panel.Children.Add(okButton);
            
            about.Content = panel;
            about.Owner = this;
            about.ShowDialog();
        }

        private void ShowSettingsDialog()
        {
            var dialog = new Window
            {
                Title = "Program Settings",
                Width = 400,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30))
            };

            var panel = new StackPanel { Margin = new Thickness(20) };
            
            // Mode selection
            var modeGroup = new GroupBox
            {
                Header = "Method",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            var modeCombo = new ComboBox
            {
                ItemsSource = new[] { "Gatekeep (default)", "Universal Redirect" },
                SelectedIndex = _applyMode == ApplyMode.UniversalRedirect ? 1 : 0,
                Margin = new Thickness(10)
            };
            
            var modePanel = new StackPanel();
            modePanel.Children.Add(modeCombo);
            modeGroup.Content = modePanel;
            
            // Block options
            var blockGroup = new GroupBox
            {
                Header = "Gatekeep Options",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            var rbBoth = new RadioButton { Content = "Block both (default)", IsChecked = _blockMode == BlockMode.Both };
            var rbPing = new RadioButton { Content = "Block UDP ping beacon endpoints", IsChecked = _blockMode == BlockMode.OnlyPing };
            var rbService = new RadioButton { Content = "Block service endpoints", IsChecked = _blockMode == BlockMode.OnlyService };
            
            var blockPanel = new StackPanel { Margin = new Thickness(10) };
            blockPanel.Children.Add(rbBoth);
            blockPanel.Children.Add(rbPing);
            blockPanel.Children.Add(rbService);
            blockGroup.Content = blockPanel;
            
            // Merge option
            var mergeCheck = new CheckBox
            {
                Content = "Merge unstable servers (recommended)",
                IsChecked = _mergeUnstable,
                Margin = new Thickness(0, 0, 0, 20)
            };
            
            // Buttons
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okButton = new Button
            {
                Content = "Apply Changes",
                Width = 120,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            okButton.Click += (s, e) => dialog.Close();
            
            var defaultButton = new Button
            {
                Content = "Default Options",
                Width = 120,
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(0x3E, 0x3E, 0x42)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            defaultButton.Click += (s, e) =>
            {
                modeCombo.SelectedIndex = 0;
                rbBoth.IsChecked = true;
                mergeCheck.IsChecked = true;
            };
            
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(defaultButton);
            
            panel.Children.Add(modeGroup);
            panel.Children.Add(blockGroup);
            panel.Children.Add(mergeCheck);
            panel.Children.Add(buttonPanel);
            
            dialog.Content = panel;
            dialog.Owner = this;
            
            if (dialog.ShowDialog() == true)
            {
                _applyMode = modeCombo.SelectedIndex == 1 ? ApplyMode.UniversalRedirect : ApplyMode.Gatekeep;
                if (_applyMode == ApplyMode.Gatekeep)
                {
                    if (rbBoth.IsChecked == true) _blockMode = BlockMode.Both;
                    else if (rbPing.IsChecked == true) _blockMode = BlockMode.OnlyPing;
                    else _blockMode = BlockMode.OnlyService;
                }
                _mergeUnstable = mergeCheck.IsChecked == true;
                SaveSettings();
                UpdateRegionListViewAppearance();
            }
        }

        private void RestoreWindowsDefaultHostsFile()
        {
            var confirm = MessageBox.Show(
                "If you are having problems, or the program doesn't seem to work correctly, try resetting your hosts file.\n\nThis will overwrite your entire hosts file with the Windows default.\n\nA backup will be saved as hosts.bak. Continue?",
                "Restore Windows default hosts file",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                try { File.Copy(HostsPath, HostsPath + ".bak", true); } catch { /* ignore backup errors */ }

                var defaultHosts =
                    "# Copyright (c) 1993-2009 Microsoft Corp.\r\n" +
                    "#\r\n" +
                    "# This is a sample HOSTS file used by Microsoft TCP/IP for Windows.\r\n" +
                    "#\r\n" +
                    "# This file contains the mappings of IP addresses to host names. Each\r\n" +
                    "# entry should be kept on an individual line. The IP address should\r\n" +
                    "# be placed in the first column followed by the corresponding host name.\r\n" +
                    "# The IP address and the host name should be separated by at least one\r\n" +
                    "# space.\r\n" +
                    "#\r\n" +
                    "# Additionally, comments (such as these) may be inserted on individual\r\n" +
                    "# lines or following the machine name denoted by a '#' symbol.\r\n" +
                    "#\r\n" +
                    "# For example:\r\n" +
                    "#\r\n" +
                    "#       102.54.94.97     rhino.acme.com          # source server\r\n" +
                    "#        38.25.63.10     x.acme.com              # x client host\r\n" +
                    "#\r\n" +
                    "# localhost name resolution is handled within DNS itself.\r\n" +
                    "#       127.0.0.1       localhost\r\n" +
                    "#       ::1             localhost\r\n";

                File.WriteAllText(HostsPath, defaultHosts);
                FlushDns();

                MessageBox.Show(
                    "Hosts file restored to Windows default template.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Please run as Administrator to modify the hosts file.",
                    "Permission Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Cleanup

        protected override void OnClosed(EventArgs e)
        {
            _pingTimer?.Stop();
            _pinger?.Dispose();
            base.OnClosed(e);
        }

        #endregion
    }

    public class ServerGroupItem : INotifyPropertyChanged
    {
        public string GroupName { get; set; } = string.Empty;
        public ObservableCollection<ServerItem> Servers { get; set; } = new();
        public bool IsExpanded { get; set; } = true;
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ServerItem : INotifyPropertyChanged
    {
        public string RegionKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ToolTipText { get; set; } = string.Empty;
        public bool IsStable { get; set; }
        
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
        
        private string _latencyText = "…";
        public string LatencyText
        {
            get => _latencyText;
            set
            {
                _latencyText = value;
                OnPropertyChanged(nameof(LatencyText));
            }
        }
        
        private SolidColorBrush _textColor = new SolidColorBrush(Colors.White);
        public SolidColorBrush TextColor
        {
            get => _textColor;
            set
            {
                _textColor = value;
                OnPropertyChanged(nameof(TextColor));
            }
        }
        
        private SolidColorBrush _latencyColor = new SolidColorBrush(Colors.Gray);
        public SolidColorBrush LatencyColor
        {
            get => _latencyColor;
            set
            {
                _latencyColor = value;
                OnPropertyChanged(nameof(LatencyColor));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"{DisplayName} - {LatencyText}";
        }

        private ICommand? _toggleSelectionCommand;
        public ICommand ToggleSelectionCommand
        {
            get
            {
                if (_toggleSelectionCommand == null)
                {
                    _toggleSelectionCommand = new RelayCommand(() => IsSelected = !IsSelected);
                }
                return _toggleSelectionCommand;
            }
        }
    }

    // Simple RelayCommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute();
        }
    }

    // Open hosts file button handler
    public partial class MainWindow
    {
        private void OpenHostsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open hosts file in default text editor
                Process.Start(new ProcessStartInfo
                {
                    FileName = HostsPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to open hosts file: {ex.Message}\n\nPath: {HostsPath}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}