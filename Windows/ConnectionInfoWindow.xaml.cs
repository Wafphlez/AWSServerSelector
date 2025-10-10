using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace AWSServerSelector
{
    public partial class ConnectionInfoWindow : Window, INotifyPropertyChanged
    {
        private DispatcherTimer? _monitoringTimer;
        private string _lastDetectedIp = "";
        private string _lastDetectedServer = "";
        private DateTime _lastUpdate = DateTime.MinValue;

        public ConnectionInfoWindow()
        {
            InitializeComponent();
            DataContext = this;
            StartMonitoring();
        }

        private void StartMonitoring()
        {
            _monitoringTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2) // Проверяем каждые 2 секунды
            };
            _monitoringTimer.Tick += async (s, e) => await CheckConnection();
            _monitoringTimer.Start();
        }

        private void StopMonitoring()
        {
            _monitoringTimer?.Stop();
            _monitoringTimer = null;
        }

        private async Task CheckConnection()
        {
            try
            {
                // Получаем активные TCP соединения
                var connections = GetActiveTcpConnections();
                
                // Ищем соединения, которые могут быть связаны с Dead by Daylight
                var dbdConnection = FindDbdConnection(connections);
                
                if (dbdConnection != null)
                {
                    var ip = dbdConnection.RemoteEndPoint.Address.ToString();
                    var port = dbdConnection.RemoteEndPoint.Port.ToString();
                    var serverName = IdentifyServer(ip);
                    
                    // Обновляем UI только если данные изменились
                    if (ip != _lastDetectedIp || serverName != _lastDetectedServer)
                    {
                        _lastDetectedIp = ip;
                        _lastDetectedServer = serverName;
                        _lastUpdate = DateTime.Now;
                        
                        UpdateConnectionInfo(ip, port, serverName, true);
                    }
                }
                else
                {
                    // Нет активного соединения
                    if (_lastDetectedIp != "")
                    {
                        _lastDetectedIp = "";
                        _lastDetectedServer = "";
                        _lastUpdate = DateTime.Now;
                        UpdateConnectionInfo("", "", "", false);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при проверке соединения: {ex.Message}");
            }
        }

        private List<TcpConnectionInformation> GetActiveTcpConnections()
        {
            var connections = new List<TcpConnectionInformation>();
            
            try
            {
                var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                var tcpConnections = ipGlobalProperties.GetActiveTcpConnections();
                connections.AddRange(tcpConnections);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при получении TCP соединений: {ex.Message}");
            }
            
            return connections;
        }

        private TcpConnectionInformation? FindDbdConnection(List<TcpConnectionInformation> connections)
        {
            // Ищем соединения, которые могут быть связаны с Dead by Daylight
            var commonPorts = new HashSet<int> { 80, 443, 22, 21, 25, 53, 110, 143, 993, 995, 587, 465, 993, 995, 1433, 3306, 5432, 6379, 27017, 11211, 9200, 9300 };
            
            foreach (var connection in connections)
            {
                // Исключаем локальные адреса и известные системные порты
                bool isLoopback = IPAddress.IsLoopback(connection.RemoteEndPoint.Address);
                bool isCommonPort = commonPorts.Contains(connection.RemoteEndPoint.Port);
                if (isLoopback || isCommonPort)
                    continue;
                
                // Ищем соединения с состоянием Established
                if (connection.State == TcpState.Established)
                {
                    // Проверяем, не является ли это AWS IP
                    var ip = connection.RemoteEndPoint.Address.ToString();
                    if (IsAwsIp(ip))
                    {
                        return connection;
                    }
                }
            }
            
            return null;
        }

        private bool IsAwsIp(string ip)
        {
            try
            {
                var address = IPAddress.Parse(ip);
                
                // Проверяем известные диапазоны AWS
                var awsRanges = new[]
                {
                    (IPAddress.Parse("52.0.0.0"), IPAddress.Parse("52.255.255.255")),
                    (IPAddress.Parse("54.0.0.0"), IPAddress.Parse("54.255.255.255")),
                    (IPAddress.Parse("18.0.0.0"), IPAddress.Parse("18.255.255.255")),
                    (IPAddress.Parse("3.0.0.0"), IPAddress.Parse("3.255.255.255")),
                    (IPAddress.Parse("13.0.0.0"), IPAddress.Parse("13.255.255.255")),
                    (IPAddress.Parse("15.0.0.0"), IPAddress.Parse("15.255.255.255")),
                    (IPAddress.Parse("35.0.0.0"), IPAddress.Parse("35.255.255.255")),
                    (IPAddress.Parse("44.0.0.0"), IPAddress.Parse("44.255.255.255")),
                    (IPAddress.Parse("107.20.0.0"), IPAddress.Parse("107.20.255.255")),
                    (IPAddress.Parse("174.129.0.0"), IPAddress.Parse("174.129.255.255")),
                    (IPAddress.Parse("184.72.0.0"), IPAddress.Parse("184.72.255.255")),
                    (IPAddress.Parse("184.73.0.0"), IPAddress.Parse("184.73.255.255")),
                    (IPAddress.Parse("204.236.0.0"), IPAddress.Parse("204.236.255.255")),
                    (IPAddress.Parse("205.251.0.0"), IPAddress.Parse("205.251.255.255")),
                    (IPAddress.Parse("207.171.0.0"), IPAddress.Parse("207.171.255.255")),
                    (IPAddress.Parse("216.137.0.0"), IPAddress.Parse("216.137.255.255")),
                    (IPAddress.Parse("216.182.0.0"), IPAddress.Parse("216.182.255.255")),
                    (IPAddress.Parse("216.239.0.0"), IPAddress.Parse("216.239.255.255")),
                    (IPAddress.Parse("216.240.0.0"), IPAddress.Parse("216.240.255.255")),
                    (IPAddress.Parse("216.241.0.0"), IPAddress.Parse("216.241.255.255")),
                    (IPAddress.Parse("216.242.0.0"), IPAddress.Parse("216.242.255.255")),
                    (IPAddress.Parse("216.243.0.0"), IPAddress.Parse("216.243.255.255")),
                    (IPAddress.Parse("216.244.0.0"), IPAddress.Parse("216.244.255.255")),
                    (IPAddress.Parse("216.245.0.0"), IPAddress.Parse("216.245.255.255")),
                    (IPAddress.Parse("216.246.0.0"), IPAddress.Parse("216.246.255.255")),
                    (IPAddress.Parse("216.247.0.0"), IPAddress.Parse("216.247.255.255")),
                    (IPAddress.Parse("216.248.0.0"), IPAddress.Parse("216.248.255.255")),
                    (IPAddress.Parse("216.249.0.0"), IPAddress.Parse("216.249.255.255")),
                    (IPAddress.Parse("216.250.0.0"), IPAddress.Parse("216.250.255.255")),
                    (IPAddress.Parse("216.251.0.0"), IPAddress.Parse("216.251.255.255")),
                    (IPAddress.Parse("216.252.0.0"), IPAddress.Parse("216.252.255.255")),
                    (IPAddress.Parse("216.253.0.0"), IPAddress.Parse("216.253.255.255")),
                    (IPAddress.Parse("216.254.0.0"), IPAddress.Parse("216.254.255.255")),
                    (IPAddress.Parse("216.255.0.0"), IPAddress.Parse("216.255.255.255"))
                };
                
                foreach (var (start, end) in awsRanges)
                {
                    if (IsInRange(address, start, end))
                        return true;
                }
            }
            catch
            {
                return false;
            }
            
            return false;
        }

        private bool IsInRange(IPAddress address, IPAddress start, IPAddress end)
        {
            var addressBytes = address.GetAddressBytes();
            var startBytes = start.GetAddressBytes();
            var endBytes = end.GetAddressBytes();
            
            for (int i = 0; i < 4; i++)
            {
                if (addressBytes[i] < startBytes[i] || addressBytes[i] > endBytes[i])
                    return false;
            }
            
            return true;
        }

        private string IdentifyServer(string ip)
        {
            try
            {
                var address = IPAddress.Parse(ip);
                
                // Простая эвристика для определения региона по IP
                var parts = ip.Split('.');
                if (parts.Length != 4) return "Неизвестный сервер";
                
                var firstOctet = int.Parse(parts[0]);
                var secondOctet = int.Parse(parts[1]);
                
                // AWS US East (N. Virginia) - 52.x.x.x
                if (firstOctet == 52)
                {
                    if (secondOctet >= 0 && secondOctet <= 95)
                        return "AWS US East (N. Virginia)";
                    else if (secondOctet >= 96 && secondOctet <= 127)
                        return "AWS US East (N. Virginia)";
                    else if (secondOctet >= 128 && secondOctet <= 159)
                        return "AWS US East (N. Virginia)";
                    else if (secondOctet >= 160 && secondOctet <= 191)
                        return "AWS US East (N. Virginia)";
                    else if (secondOctet >= 192 && secondOctet <= 223)
                        return "AWS US East (N. Virginia)";
                    else if (secondOctet >= 224 && secondOctet <= 255)
                        return "AWS US East (N. Virginia)";
                }
                
                // AWS Europe (Ireland) - 54.x.x.x
                if (firstOctet == 54)
                {
                    if (secondOctet >= 0 && secondOctet <= 95)
                        return "AWS Europe (Ireland)";
                    else if (secondOctet >= 96 && secondOctet <= 127)
                        return "AWS Europe (Ireland)";
                    else if (secondOctet >= 128 && secondOctet <= 159)
                        return "AWS Europe (Ireland)";
                    else if (secondOctet >= 160 && secondOctet <= 191)
                        return "AWS Europe (Ireland)";
                    else if (secondOctet >= 192 && secondOctet <= 223)
                        return "AWS Europe (Ireland)";
                    else if (secondOctet >= 224 && secondOctet <= 255)
                        return "AWS Europe (Ireland)";
                }
                
                return "Неизвестный сервер";
            }
            catch
            {
                return "Неизвестный сервер";
            }
        }

        private void UpdateConnectionInfo(string ip, string port, string serverName, bool isConnected)
        {
            Dispatcher.Invoke(() =>
            {
                if (isConnected)
                {
                    ConnectionStatusText.Text = "Подключено";
                    ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45)); // Green
                    
                    IpAddressText.Text = ip;
                    IpAddressText.Foreground = new SolidColorBrush(Colors.White);
                    
                    ServerNameText.Text = serverName;
                    ServerNameText.Foreground = new SolidColorBrush(Colors.White);
                    
                    PortText.Text = port;
                    PortText.Foreground = new SolidColorBrush(Colors.White);
                    
                    // Измеряем пинг
                    _ = MeasurePing(ip);
                }
                else
                {
                    ConnectionStatusText.Text = "Не подключено";
                    ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0x14, 0x3C)); // Red
                    
                    IpAddressText.Text = "Не определен";
                    IpAddressText.Foreground = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
                    
                    ServerNameText.Text = "Не определен";
                    ServerNameText.Foreground = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
                    
                    PortText.Text = "Не определен";
                    PortText.Foreground = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
                    
                    PingText.Text = "Не измерен";
                    PingText.Foreground = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
                }
                
                // Обновляем время последнего обновления
                LastUpdateText.Text = _lastUpdate.ToString("HH:mm:ss");
                LastUpdateText.Foreground = new SolidColorBrush(Colors.White);
                
                // Определяем регион
                RegionText.Text = DetermineRegion(serverName);
                RegionText.Foreground = new SolidColorBrush(Colors.White);
            });
        }

        private async Task MeasurePing(string ip)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ip, 3000);
                
                Dispatcher.Invoke(() =>
                {
                    if (reply.Status == IPStatus.Success)
                    {
                        PingText.Text = $"{reply.RoundtripTime} ms";
                        PingText.Foreground = GetPingColor(reply.RoundtripTime);
                    }
                    else
                    {
                        PingText.Text = "Недоступен";
                        PingText.Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0x14, 0x3C));
                    }
                });
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    PingText.Text = "Ошибка";
                    PingText.Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0x14, 0x3C));
                });
            }
        }

        private SolidColorBrush GetPingColor(long ms)
        {
            if (ms < 50) return new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45)); // Green
            if (ms < 100) return new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)); // Yellow
            if (ms < 200) return new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x00)); // Orange
            return new SolidColorBrush(Color.FromRgb(0xDC, 0x14, 0x3C)); // Red
        }

        private string DetermineRegion(string serverName)
        {
            if (serverName.Contains("US East") || serverName.Contains("N. Virginia"))
                return "Северная Америка (Восток)";
            if (serverName.Contains("US West") || serverName.Contains("California") || serverName.Contains("Oregon"))
                return "Северная Америка (Запад)";
            if (serverName.Contains("Europe") || serverName.Contains("Ireland") || serverName.Contains("Frankfurt") || serverName.Contains("London"))
                return "Европа";
            if (serverName.Contains("Asia") || serverName.Contains("Tokyo") || serverName.Contains("Seoul") || serverName.Contains("Mumbai") || serverName.Contains("Singapore"))
                return "Азия";
            if (serverName.Contains("Sydney"))
                return "Океания";
            if (serverName.Contains("China") || serverName.Contains("Beijing") || serverName.Contains("Ningxia"))
                return "Китай";
            if (serverName.Contains("Canada"))
                return "Канада";
            if (serverName.Contains("South America") || serverName.Contains("São Paulo"))
                return "Южная Америка";
            
            return "Не определен";
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _ = CheckConnection();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            StopMonitoring();
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            StopMonitoring();
            base.OnClosed(e);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
