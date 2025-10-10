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
using System.Management;

namespace AWSServerSelector
{
    public partial class ConnectionInfoWindow : Window, INotifyPropertyChanged
    {
        private DispatcherTimer? _monitoringTimer;
        private DispatcherTimer? _timeUpdateTimer;
        private DateTime _lastUpdate = DateTime.Now;
        
        // Отдельные поля для лобби и игры
        private string _lobbyIp = "";
        private string _lobbyServer = "";
        private string _gameIp = "";
        private string _gameServer = "";

        public ConnectionInfoWindow()
        {
            InitializeComponent();
            DataContext = this;
            
            // Инициализируем время последнего обновления
            UpdateLastUpdateDisplay();
            
            StartMonitoring();
            StartTimeUpdateTimer();
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

        private void StartTimeUpdateTimer()
        {
            _timeUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // Обновляем каждую секунду
            };
            _timeUpdateTimer.Tick += (s, e) => UpdateLastUpdateDisplay();
            _timeUpdateTimer.Start();
        }

        private void StopTimeUpdateTimer()
        {
            _timeUpdateTimer?.Stop();
            _timeUpdateTimer = null;
        }

        private void UpdateLastUpdateDisplay()
        {
            try
            {
                var timeSpan = DateTime.Now - _lastUpdate;
                var relativeTime = FormatRelativeTime(timeSpan);
                var fullTime = _lastUpdate.ToString("dd.MM.yyyy HH:mm:ss");
                
                Dispatcher.Invoke(() =>
                {
                    LastUpdateText.Text = $"{fullTime} ({relativeTime})";
                    LastUpdateText.Foreground = new SolidColorBrush(Colors.White);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при обновлении времени: {ex.Message}");
            }
        }

        private string FormatRelativeTime(TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 60)
            {
                var seconds = (int)timeSpan.TotalSeconds;
                return seconds <= 1 ? "только что" : $"{seconds} сек назад";
            }
            else if (timeSpan.TotalMinutes < 60)
            {
                var minutes = (int)timeSpan.TotalMinutes;
                return minutes == 1 ? "1 мин назад" : $"{minutes} мин назад";
            }
            else if (timeSpan.TotalHours < 24)
            {
                var hours = (int)timeSpan.TotalHours;
                return hours == 1 ? "1 час назад" : $"{hours} ч назад";
            }
            else
            {
                var days = (int)timeSpan.TotalDays;
                return days == 1 ? "1 день назад" : $"{days} дн назад";
            }
        }

        private async Task CheckConnection()
        {
            try
            {
                Debug.WriteLine("=== Начало проверки соединений ===");
                bool hasGameConnection = false;
                bool hasLobbyConnection = false;
                
                // Проверяем UDP соединения (игровые серверы)
                var udpConnections = GetActiveUdpConnections();
                Debug.WriteLine($"Найдено {udpConnections.Count} активных UDP соединений");
                
                // Логируем все UDP соединения
                foreach (var conn in udpConnections)
                {
                    Debug.WriteLine($"UDP: {conn.LocalEndPoint} -> {conn.RemoteEndPoint}");
                }
                
                var gameConnection = await FindGameConnectionAsync(udpConnections);
                
                if (gameConnection != null)
                {
                    var ip = gameConnection.RemoteEndPoint.Address.ToString();
                    var port = gameConnection.RemoteEndPoint.Port.ToString();
                    var serverName = await IdentifyServerAsync(ip);
                    
                    Debug.WriteLine($"Найдено игровое соединение: {ip}:{port} - {serverName}");
                    
                    // Обновляем игровое соединение
                    if (ip != _gameIp || serverName != _gameServer)
                    {
                        _gameIp = ip;
                        _gameServer = serverName;
                        hasGameConnection = true;
                    }
                }
                else
                {
                    // Не сбрасываем игровое соединение, если оно уже было определено
                    // Это позволяет сохранить информацию о последнем известном игровом сервере
                    Debug.WriteLine("Активное игровое соединение не найдено, но сохраняем последние известные данные");
                }
                
                // Проверяем TCP соединения (лобби и другие сервисы)
                var tcpConnections = GetActiveTcpConnections();
                Debug.WriteLine($"Найдено {tcpConnections.Count} активных TCP соединений");
                var establishedConnections = tcpConnections.Where(c => c.State == TcpState.Established).ToList();
                Debug.WriteLine($"Установленных TCP соединений: {establishedConnections.Count}");
                
                // Выводим все установленные TCP соединения для отладки
                foreach (var conn in establishedConnections)
                {
                    var ip = conn.RemoteEndPoint.Address.ToString();
                    var port = conn.RemoteEndPoint.Port;
                    var isAws = await IsAwsIpAsync(ip);
                    Debug.WriteLine($"TCP соединение: {ip}:{port} - AWS: {isAws}");
                }
                
                var lobbyConnection = await FindDbdConnectionAsync(tcpConnections);
                
                if (lobbyConnection != null)
                {
                    var ip = lobbyConnection.RemoteEndPoint.Address.ToString();
                    var port = lobbyConnection.RemoteEndPoint.Port.ToString();
                    var serverName = await IdentifyServerAsync(ip);
                    
                    Debug.WriteLine($"Найдено лобби соединение: {ip}:{port} - {serverName}");
                    
                    // Обновляем лобби соединение
                    if (ip != _lobbyIp || serverName != _lobbyServer)
                    {
                        _lobbyIp = ip;
                        _lobbyServer = serverName;
                        hasLobbyConnection = true;
                    }
                }
                else
                {
                    // Не сбрасываем лобби соединение, если оно уже было определено
                    // Это позволяет сохранить информацию о последнем известном лобби сервере
                    Debug.WriteLine("Активное лобби соединение не найдено, но сохраняем последние известные данные");
                }
                
                // Обновляем UI только если есть изменения
                if (hasGameConnection || hasLobbyConnection)
                {
                    _lastUpdate = DateTime.Now;
                    UpdateConnectionInfo();
                }
                else
                {
                    // Обновляем время последнего обновления, но не сбрасываем данные
                    _lastUpdate = DateTime.Now;
                    UpdateConnectionInfo();
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

        private List<UdpConnectionInfo> GetActiveUdpConnections()
        {
            var connections = new List<UdpConnectionInfo>();
            
            try
            {
                // Используем netstat для получения UDP соединений с удаленными адресами
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "netstat",
                        Arguments = "-an",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains("UDP") && !line.Contains("*:*") && !line.Contains("127.0.0.1") && !line.Contains("192.168.1.52"))
                    {
                        var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3)
                        {
                            var localAddress = parts[1];
                            var remoteAddress = parts[2];
                            
                            if (remoteAddress != "*:*" && !remoteAddress.StartsWith("127.0.0.1") && !remoteAddress.StartsWith("192.168.1.52"))
                            {
                                try
                                {
                                    var remoteParts = remoteAddress.Split(':');
                                    if (remoteParts.Length == 2 && int.TryParse(remoteParts[1], out int port))
                                    {
                                        if (IPAddress.TryParse(remoteParts[0], out var ip))
                                        {
                                            connections.Add(new UdpConnectionInfo
                                            {
                                                LocalEndPoint = new IPEndPoint(IPAddress.Any, 0),
                                                RemoteEndPoint = new IPEndPoint(ip, port)
                                            });
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Ошибка парсинга UDP адреса {remoteAddress}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при получении UDP соединений: {ex.Message}");
            }
            
            return connections;
        }

        private async Task<UdpConnectionInfo?> FindGameConnectionAsync(List<UdpConnectionInfo> connections)
        {
            // Ищем UDP соединения, которые могут быть игровыми серверами
            var commonPorts = new HashSet<int> { 53, 67, 68, 123, 161, 162, 500, 4500, 5353, 5355 };
            
            // DNS серверы и известные сервисы, которые не могут быть игровыми серверами
            var excludedServers = new HashSet<string> { 
                "8.8.8.8", "8.8.4.4",           // Google DNS
                "1.1.1.1", "1.0.0.1",           // Cloudflare DNS
                "208.67.222.222", "208.67.220.220", // OpenDNS
                "9.9.9.9", "149.112.112.112",   // Quad9 DNS
                "173.194.221.105",              // Google сервер
                "142.250.191.105",              // Google сервер
                "172.217.16.110",               // Google сервер
                "216.58.208.110"                // Google сервер
            };
            
            foreach (var connection in connections)
            {
                var ip = connection.RemoteEndPoint.Address.ToString();
                var port = connection.RemoteEndPoint.Port;
                var localPort = connection.LocalEndPoint.Port;
                var address = connection.RemoteEndPoint.Address;
                
                bool isLoopback = IPAddress.IsLoopback(address);
                bool isPrivate = IsPrivateIp(address);
                bool isCommonPort = commonPorts.Contains(port);
                bool isExcludedServer = excludedServers.Contains(ip);
                bool isFromDbd = IsDeadByDaylightProcess(localPort);
                
                Debug.WriteLine($"Проверяем UDP соединение {ip}:{port} - Loopback: {isLoopback}, Private: {isPrivate}, CommonPort: {isCommonPort}, ExcludedServer: {isExcludedServer}, от DeadByDaylight: {isFromDbd}");
                
                // Ищем внешние UDP соединения на нестандартных портах (игровые серверы) от DeadByDaylight
                // Исключаем известные сервисы (Google, DNS и т.д.)
                if (!isLoopback && !isPrivate && !isCommonPort && !isExcludedServer && isFromDbd)
                {
                    // Дополнительная проверка: приоритет AWS GameLift серверам
                    var isAws = await IsAwsIpAsync(ip);
                    if (isAws)
                    {
                        Debug.WriteLine($"Найдено AWS GameLift UDP соединение от DeadByDaylight: {ip}:{port}");
                        return connection;
                    }
                    
                    // Если не AWS, но все остальные условия выполнены, тоже считаем игровым
                    Debug.WriteLine($"Найдено игровое UDP соединение от DeadByDaylight: {ip}:{port}");
                    return connection;
                }
            }
            
            Debug.WriteLine("Игровое UDP соединение не найдено");
            return null;
        }

        private async Task<TcpConnectionInformation?> FindDbdConnectionAsync(List<TcpConnectionInformation> connections)
        {
            // Ищем соединения, которые могут быть связаны с Dead by Daylight
            var commonPorts = new HashSet<int> { 80, 443, 22, 21, 25, 53, 110, 143, 993, 995, 587, 465, 1433, 3306, 5432, 6379, 27017, 11211, 9200, 9300 };
            
            // Приоритет 1: GameLift серверы (внутриигровые) от DeadByDaylight
            var gameliftConnection = await FindGameLiftConnectionAsync(connections);
            if (gameliftConnection != null)
            {
                // Проверяем, что соединение идет от DeadByDaylight
                var localPort = gameliftConnection.LocalEndPoint.Port;
                if (IsDeadByDaylightProcess(localPort))
                {
                    Debug.WriteLine($"Найдено GameLift соединение от DeadByDaylight: {gameliftConnection.RemoteEndPoint}");
                    return gameliftConnection;
                }
                else
                {
                    Debug.WriteLine($"GameLift соединение не от DeadByDaylight (процесс: {GetProcessNameByPort(localPort)})");
                }
            }
            
            // Приоритет 2: Другие AWS серверы от DeadByDaylight
            foreach (var connection in connections)
            {
                if (connection.State == TcpState.Established)
                {
                    var ip = connection.RemoteEndPoint.Address.ToString();
                    var port = connection.RemoteEndPoint.Port;
                    var localPort = connection.LocalEndPoint.Port;
                    
                    var isAws = await IsAwsIpAsync(ip);
                    var isFromDbd = IsDeadByDaylightProcess(localPort);
                    
                    Debug.WriteLine($"Проверяем AWS соединение {ip}:{port} - AWS: {isAws}, от DeadByDaylight: {isFromDbd}");
                    
                    if (isAws && isFromDbd)
                    {
                        Debug.WriteLine($"Найдено AWS соединение от DeadByDaylight: {ip}:{port}");
                        return connection;
                    }
                }
            }
            
            // Приоритет 3: Внешние соединения на нестандартных портах от DeadByDaylight
            foreach (var connection in connections)
            {
                if (connection.State == TcpState.Established)
                {
                    var ip = connection.RemoteEndPoint.Address.ToString();
                    var port = connection.RemoteEndPoint.Port;
                    var localPort = connection.LocalEndPoint.Port;
                    var address = connection.RemoteEndPoint.Address;
                    
                    bool isLoopback = IPAddress.IsLoopback(address);
                    bool isPrivate = IsPrivateIp(address);
                    bool isCommonPort = commonPorts.Contains(port);
                    bool isFromDbd = IsDeadByDaylightProcess(localPort);
                    
                    Debug.WriteLine($"Проверяем не-AWS соединение {ip}:{port} - Loopback: {isLoopback}, Private: {isPrivate}, CommonPort: {isCommonPort}, от DeadByDaylight: {isFromDbd}");
                    
                    // Ищем внешние соединения на нестандартных портах от DeadByDaylight
                    if (!isLoopback && !isPrivate && !isCommonPort && isFromDbd)
                    {
                        Debug.WriteLine($"Найдено нестандартное соединение от DeadByDaylight: {ip}:{port}");
                        return connection;
                    }
                }
            }
            
            Debug.WriteLine("Не найдено подходящих соединений");
            return null;
        }

        private async Task<TcpConnectionInformation?> FindGameLiftConnectionAsync(List<TcpConnectionInformation> connections)
        {
            // Ищем GameLift серверы (внутриигровые серверы)
            foreach (var connection in connections)
            {
                if (connection.State == TcpState.Established)
                {
                    var ip = connection.RemoteEndPoint.Address.ToString();
                    var port = connection.RemoteEndPoint.Port;
                    
                    // Проверяем, является ли это GameLift сервером
                    var isGameLift = await IsGameLiftServerAsync(ip);
                    Debug.WriteLine($"Проверяем GameLift соединение {ip}:{port} - GameLift: {isGameLift}");
                    
                    if (isGameLift)
                    {
                        Debug.WriteLine($"Найдено GameLift соединение: {ip}:{port}");
                        return connection;
                    }
                }
            }
            
            return null;
        }

        private async Task<bool> IsGameLiftServerAsync(string ip)
        {
            try
            {
                var isAws = await AwsIpRangeManager.Instance.IsAwsIpAsync(ip);
                if (!isAws) return false;
                
                var service = await AwsIpRangeManager.Instance.GetAwsServiceAsync(ip);
                var region = await AwsIpRangeManager.Instance.GetAwsRegionAsync(ip);
                
                // GameLift серверы обычно находятся в определенных регионах
                var gameLiftRegions = new HashSet<string>
                {
                    "eu-central-1", // Центральная Европа (ваш приоритет)
                    "us-east-1",    // US East
                    "us-west-2",    // US West
                    "eu-west-1",    // Europe Ireland
                    "ap-northeast-1", // Asia Tokyo
                    "ap-southeast-1"  // Asia Singapore
                };
                
                // Проверяем, что это GameLift сервис в игровом регионе
                bool isGameLiftService = service.Contains("GAMELIFT", StringComparison.OrdinalIgnoreCase) ||
                                       service.Contains("EC2", StringComparison.OrdinalIgnoreCase); // GameLift может использовать EC2
                
                bool isGameLiftRegion = gameLiftRegions.Contains(region);
                
                Debug.WriteLine($"GameLift проверка для {ip}: Service={service}, Region={region}, IsGameLiftService={isGameLiftService}, IsGameLiftRegion={isGameLiftRegion}");
                
                return isGameLiftService && isGameLiftRegion;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при проверке GameLift сервера {ip}: {ex.Message}");
                return false;
            }
        }

        private bool IsPrivateIp(IPAddress address)
        {
            if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                return false;

            var bytes = address.GetAddressBytes();
            
            // 10.0.0.0/8
            if (bytes[0] == 10) return true;
            
            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            
            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            
            // 169.254.0.0/16 (link-local)
            if (bytes[0] == 169 && bytes[1] == 254) return true;
            
            return false;
        }

        private async Task<bool> IsAwsIpAsync(string ip)
        {
            try
            {
                return await AwsIpRangeManager.Instance.IsAwsIpAsync(ip);
            }
            catch
            {
                    return false;
            }
        }

        private async Task<string> IdentifyServerAsync(string ip)
        {
            try
            {
                var isAws = await AwsIpRangeManager.Instance.IsAwsIpAsync(ip);
                if (isAws)
                {
                    var region = await AwsIpRangeManager.Instance.GetAwsRegionAsync(ip);
                    var service = await AwsIpRangeManager.Instance.GetAwsServiceAsync(ip);
                    var isGameLift = await IsGameLiftServerAsync(ip);
                    
                    if (isGameLift)
                    {
                        return $"🎮 GameLift {GetRegionDisplayName(region)} (Игровой сервер)";
                    }
                    else
                    {
                        return $"AWS {GetRegionDisplayName(region)} ({service})";
                    }
                }
                
                // Проверяем другие известные провайдеры
                var nonAwsInfo = await IdentifyNonAwsServerAsync(ip);
                if (!string.IsNullOrEmpty(nonAwsInfo))
                {
                    return nonAwsInfo;
                }
                
                return "Неизвестный сервер";
            }
            catch
            {
                return "Неизвестный сервер";
            }
        }

        private async Task<string> IdentifyNonAwsServerAsync(string ip)
        {
            try
            {
                // Проверяем известные IP диапазоны
                if (IsGoogleIp(ip))
                {
                    return "🌐 Google Cloud (США, Калифорния)";
                }
                
                if (IsMicrosoftIp(ip))
                {
                    return "🌐 Microsoft Azure";
                }
                
                if (IsCloudflareIp(ip))
                {
                    return "🌐 Cloudflare";
                }
                
                // Попробуем определить по геолокации
                var geoInfo = await GetGeoLocationInfoAsync(ip);
                if (!string.IsNullOrEmpty(geoInfo))
                {
                    return geoInfo;
                }
                
                return string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при определении не-AWS сервера {ip}: {ex.Message}");
                return string.Empty;
            }
        }

        private bool IsGoogleIp(string ip)
        {
            // Google IP диапазоны (основные)
            var googleRanges = new[]
            {
                "108.177.0.0/16",    // Google
                "172.217.0.0/16",    // Google
                "74.125.0.0/16",     // Google
                "173.194.0.0/16",    // Google
                "209.85.0.0/16",     // Google
                "66.102.0.0/16",     // Google
                "66.249.0.0/16",     // Google
                "72.14.0.0/16",      // Google
                "216.58.0.0/16",     // Google
                "216.239.0.0/16"     // Google
            };
            
            return IsIpInRanges(ip, googleRanges);
        }

        private bool IsMicrosoftIp(string ip)
        {
            // Microsoft Azure IP диапазоны (основные)
            var microsoftRanges = new[]
            {
                "40.64.0.0/10",      // Microsoft Azure
                "52.160.0.0/11",     // Microsoft Azure
                "52.224.0.0/11",     // Microsoft Azure
                "104.40.0.0/13",     // Microsoft Azure
                "104.146.0.0/15",    // Microsoft Azure
                "104.208.0.0/12",    // Microsoft Azure
                "104.211.0.0/16",    // Microsoft Azure
                "104.214.0.0/15",    // Microsoft Azure
                "104.215.0.0/16",    // Microsoft Azure
                "104.40.0.0/13"      // Microsoft Azure
            };
            
            return IsIpInRanges(ip, microsoftRanges);
        }

        private bool IsCloudflareIp(string ip)
        {
            // Cloudflare IP диапазоны (основные)
            var cloudflareRanges = new[]
            {
                "173.245.48.0/20",   // Cloudflare
                "103.21.244.0/22",   // Cloudflare
                "103.22.200.0/22",   // Cloudflare
                "103.31.4.0/22",     // Cloudflare
                "141.101.64.0/18",   // Cloudflare
                "108.162.192.0/18",  // Cloudflare
                "190.93.240.0/20",   // Cloudflare
                "188.114.96.0/20",   // Cloudflare
                "197.234.240.0/22",  // Cloudflare
                "198.41.128.0/17"    // Cloudflare
            };
            
            return IsIpInRanges(ip, cloudflareRanges);
        }

        private bool IsIpInRanges(string ip, string[] ranges)
        {
            try
            {
                if (!IPAddress.TryParse(ip, out var address))
                    return false;
                
                foreach (var range in ranges)
                {
                    if (IsIpAddressInRange(address, range))
                        return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool IsIpAddressInRange(IPAddress address, string cidr)
        {
            try
            {
                var parts = cidr.Split('/');
                var networkAddress = IPAddress.Parse(parts[0]);
                var prefixLength = int.Parse(parts[1]);

                var ipBytes = address.GetAddressBytes();
                var networkBytes = networkAddress.GetAddressBytes();

                uint ipUint = BitConverter.ToUInt32(ipBytes.Reverse().ToArray(), 0);
                uint networkUint = BitConverter.ToUInt32(networkBytes.Reverse().ToArray(), 0);

                uint mask = ~(uint.MaxValue >> prefixLength);

                return (ipUint & mask) == (networkUint & mask);
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> GetGeoLocationInfoAsync(string ip)
        {
            try
            {
                // Простая геолокация по известным паттернам
                var bytes = IPAddress.Parse(ip).GetAddressBytes();
                
                // Google IP (108.177.x.x)
                if (bytes[0] == 108 && bytes[1] == 177)
                {
                    return "🌐 Google (США, Калифорния)";
                }
                
                // Другие известные паттерны можно добавить здесь
                
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string GetRegionDisplayName(string region)
        {
            return region switch
            {
                "us-east-1" => "US East (N. Virginia)",
                "us-east-2" => "US East (Ohio)",
                "us-west-1" => "US West (N. California)",
                "us-west-2" => "US West (Oregon)",
                "eu-west-1" => "Europe (Ireland)",
                "eu-west-2" => "Europe (London)",
                "eu-central-1" => "Europe (Frankfurt)",
                "ap-northeast-1" => "Asia Pacific (Tokyo)",
                "ap-northeast-2" => "Asia Pacific (Seoul)",
                "ap-south-1" => "Asia Pacific (Mumbai)",
                "ap-southeast-1" => "Asia Pacific (Singapore)",
                "ap-southeast-2" => "Asia Pacific (Sydney)",
                "ca-central-1" => "Canada (Central)",
                "sa-east-1" => "South America (São Paulo)",
                _ => region
            };
        }

        private void UpdateConnectionInfo()
        {
            Dispatcher.Invoke(() =>
            {
                bool hasGame = !string.IsNullOrEmpty(_gameIp);
                bool hasLobby = !string.IsNullOrEmpty(_lobbyIp);
                
                // Обновляем лобби
                if (hasLobby)
                {
                    LobbyStatusText.Text = "Подключено";
                    LobbyStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45)); // Green
                    
                    LobbyIpText.Text = _lobbyIp;
                    LobbyIpText.Foreground = new SolidColorBrush(Colors.White);
                    
                    LobbyServerText.Text = _lobbyServer;
                    LobbyServerText.Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xE2)); // Blue
                    
                    // Обновляем регион для лобби
                    LobbyRegionText.Text = DetermineRegion(_lobbyServer);
                    LobbyRegionText.Foreground = new SolidColorBrush(Colors.White);
                    
                    CopyLobbyIpButton.Visibility = Visibility.Visible;
                    
                    // Измеряем пинг лобби сервера
                    var lobbyIp = _lobbyIp.Split(':')[0];
                    _ = MeasureLobbyPing(lobbyIp);
                }
                else
                {
                    LobbyStatusText.Text = "Не подключено";
                    LobbyStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0x14, 0x3C)); // Red
                    
                    LobbyIpText.Text = "Не определен";
                    LobbyIpText.Foreground = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
                    
                    LobbyServerText.Text = "Не определен";
                    LobbyServerText.Foreground = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
                    
                    LobbyPingText.Text = "Не измерен";
                    LobbyPingText.Foreground = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
                    
                    LobbyRegionText.Text = "Не определен";
                    LobbyRegionText.Foreground = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
                    
                    CopyLobbyIpButton.Visibility = Visibility.Collapsed;
                }
                
                // Обновляем игру
                if (hasGame)
                {
                    GameStatusText.Text = "Подключено";
                    GameStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45)); // Green
                    
                    GameIpText.Text = _gameIp;
                    GameIpText.Foreground = new SolidColorBrush(Colors.White);
                    
                    GameServerText.Text = _gameServer;
                    GameServerText.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x35)); // Orange
                    
                    // Обновляем регион для игры
                    GameRegionText.Text = DetermineRegion(_gameServer);
                    GameRegionText.Foreground = new SolidColorBrush(Colors.White);
                    
                    CopyGameIpButton.Visibility = Visibility.Visible;
                    
                    // Измеряем пинг игрового сервера
                    var gameIp = _gameIp.Split(':')[0];
                    _ = MeasureGamePing(gameIp);
                }
                else
                {
                    GameStatusText.Text = "Не подключено";
                    GameStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0x14, 0x3C)); // Red
                    
                    GameIpText.Text = "Не определен";
                    GameIpText.Foreground = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
                    
                    GameServerText.Text = "Не определен";
                    GameServerText.Foreground = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
                    
                    GamePingText.Text = "Не измерен";
                    GamePingText.Foreground = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
                    
                    GameRegionText.Text = "Не определен";
                    GameRegionText.Foreground = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
                    
                    CopyGameIpButton.Visibility = Visibility.Collapsed;
                }
                
                // Обновляем время последнего обновления
                UpdateLastUpdateDisplay();
            });
        }

        private async Task MeasureLobbyPing(string ip)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ip, 3000);
                
                Dispatcher.Invoke(() =>
                {
                    if (reply.Status == IPStatus.Success)
                    {
                        LobbyPingText.Text = $"{reply.RoundtripTime} мс";
                        LobbyPingText.Foreground = GetPingColor(reply.RoundtripTime);
                    }
                    else
                    {
                        LobbyPingText.Text = "Недоступен";
                        LobbyPingText.Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0x14, 0x3C));
                    }
                });
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    LobbyPingText.Text = "Ошибка";
                    LobbyPingText.Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0x14, 0x3C));
                });
            }
        }
        
        private async Task MeasureGamePing(string ip)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ip, 3000);
                
                Dispatcher.Invoke(() =>
                {
                    if (reply.Status == IPStatus.Success)
                    {
                        GamePingText.Text = $"{reply.RoundtripTime} мс";
                        GamePingText.Foreground = GetPingColor(reply.RoundtripTime);
                    }
                    else
                    {
                        GamePingText.Text = "Недоступен";
                        GamePingText.Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0x14, 0x3C));
                    }
                });
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    GamePingText.Text = "Ошибка";
                    GamePingText.Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0x14, 0x3C));
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
            if (serverName.Contains("GameLift"))
            {
                if (serverName.Contains("Frankfurt") || serverName.Contains("eu-central-1"))
                    return "🎮 Центральная Европа (Игровой сервер)";
                if (serverName.Contains("Ireland") || serverName.Contains("eu-west-1"))
                    return "🎮 Европа (Ирландия) (Игровой сервер)";
                if (serverName.Contains("London") || serverName.Contains("eu-west-2"))
                    return "🎮 Европа (Лондон) (Игровой сервер)";
                if (serverName.Contains("US East") || serverName.Contains("N. Virginia"))
                    return "🎮 Северная Америка (Восток) (Игровой сервер)";
                if (serverName.Contains("US West") || serverName.Contains("California") || serverName.Contains("Oregon"))
                    return "🎮 Северная Америка (Запад) (Игровой сервер)";
                if (serverName.Contains("Tokyo") || serverName.Contains("ap-northeast-1"))
                    return "🎮 Азия (Токио) (Игровой сервер)";
                if (serverName.Contains("Singapore") || serverName.Contains("ap-southeast-1"))
                    return "🎮 Азия (Сингапур) (Игровой сервер)";
            }
            
            // Google серверы
            if (serverName.Contains("Google"))
            {
                if (serverName.Contains("Калифорния") || serverName.Contains("California"))
                    return "🌐 Google Cloud (США, Калифорния)";
                return "🌐 Google Cloud";
            }
            
            // Microsoft Azure
            if (serverName.Contains("Microsoft") || serverName.Contains("Azure"))
                return "🌐 Microsoft Azure";
            
            // Cloudflare
            if (serverName.Contains("Cloudflare"))
                return "🌐 Cloudflare";
            
            // Обычные серверы (лобби и другие)
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

        
        private void CopyLobbyIpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ipPort = LobbyIpText.Text;
                if (!string.IsNullOrEmpty(ipPort) && ipPort != "Не определен")
                {
                    Clipboard.SetText(ipPort);
                    MessageBox.Show($"IP:Порт лобби скопирован в буфер обмена:\n{ipPort}", 
                        "Скопировано", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при копировании: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void CopyGameIpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ipPort = GameIpText.Text;
                if (!string.IsNullOrEmpty(ipPort) && ipPort != "Не определен")
                {
                    Clipboard.SetText(ipPort);
                    MessageBox.Show($"IP:Порт матча скопирован в буфер обмена:\n{ipPort}", 
                        "Скопировано", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при копировании: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void DebugButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDebugInfo();
        }


        private async void ShowDebugInfo()
        {
            try
            {
                var connections = GetActiveTcpConnections();
                var establishedConnections = connections.Where(c => c.State == TcpState.Established).ToList();
                
                var debugInfo = new StringBuilder();
                debugInfo.AppendLine($"Всего соединений: {connections.Count}");
                debugInfo.AppendLine($"Установленных соединений: {establishedConnections.Count}");
                debugInfo.AppendLine();
                debugInfo.AppendLine("Активные соединения:");
                
                foreach (var conn in establishedConnections.Take(10))
                {
                    var ip = conn.RemoteEndPoint.Address.ToString();
                    var port = conn.RemoteEndPoint.Port;
                    var isAws = await IsAwsIpAsync(ip);
                    debugInfo.AppendLine($"{ip}:{port} - AWS: {isAws}");
                }
                
                MessageBox.Show(debugInfo.ToString(), "Отладочная информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            StopMonitoring();
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            StopMonitoring();
            StopTimeUpdateTimer();
            base.OnClosed(e);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private string GetProcessNameByPort(int port)
        {
            try
            {
                // Используем netstat для получения информации о процессе
                var startInfo = new ProcessStartInfo
                {
                    FileName = "netstat",
                    Arguments = "-ano",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null) return "Неизвестно";

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    // Ищем как LISTENING, так и ESTABLISHED соединения
                    if (line.Contains($":{port} ") && (line.Contains("LISTENING") || line.Contains("ESTABLISHED")))
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5 && int.TryParse(parts[4], out int pid))
                        {
                            try
                            {
                                var proc = Process.GetProcessById(pid);
                                Debug.WriteLine($"Найден процесс {proc.ProcessName} (PID: {pid}) для порта {port}");
                                return proc.ProcessName;
                            }
                            catch
                            {
                                Debug.WriteLine($"Не удалось получить процесс с PID {pid} для порта {port}");
                                return $"PID {pid}";
                            }
                        }
                    }
                }

                Debug.WriteLine($"Процесс для порта {port} не найден");
                return "Неизвестно";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при определении процесса по порту {port}: {ex.Message}");
                return "Ошибка";
            }
        }

        private bool IsDeadByDaylightProcess(int port)
        {
            var processName = GetProcessNameByPort(port);
            Debug.WriteLine($"Проверяем процесс для порта {port}: {processName}");
            
            // Проверяем, что соединение идет от DeadByDaylight
            bool isDbd = processName.Equals("DeadByDaylight", StringComparison.OrdinalIgnoreCase) ||
                        processName.Equals("DeadByDaylight-Win64-Shipping", StringComparison.OrdinalIgnoreCase);
            
            // Если процесс не определен или неизвестен, но это не исключенный сервер, 
            // то считаем что это может быть игровое соединение
            if (!isDbd && (processName == "Неизвестно" || processName == "Ошибка"))
            {
                Debug.WriteLine($"Процесс не определен для порта {port}, но не исключаем соединение");
                return true; // Временно разрешаем неизвестные процессы
            }
            
            return isDbd;
        }
    }

    public class UdpConnectionInfo
    {
        public IPEndPoint LocalEndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 0);
        public IPEndPoint RemoteEndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 0);
    }
}
