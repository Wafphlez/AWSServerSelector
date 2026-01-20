using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
        #region Fields

        private DispatcherTimer? _monitoringTimer;
        private const string DBD_PROCESS_NAME = "DeadByDaylight-Win64-Shipping";
        private readonly Ping _pinger = new();
        private ConnectionInfo? _currentLobbyConnection;
        private ConnectionInfo? _currentGameConnection;
        private UdpGameMonitor? _udpMonitor;
        private bool _udpMonitorStarted = false;
        private HashSet<int> _monitoredUdpPorts = new();
        
        // Фоновый мониторинг пинга (через DispatcherTimer)
        private DispatcherTimer? _pingTimer;
        private Ping? _backgroundPinger; // Отдельный Ping объект для фонового мониторинга!
        private string? _currentGameServerIp;
        private int _currentGameServerPort;
        
        // Запоминаем выбранные IP для стабильности
        private string? _lastLobbyIp;
        private int _lastLobbyPort;
        private string? _lastGameIp;
        private int _lastGamePort;

        #endregion

        #region Constructor

        public ConnectionInfoWindow()
        {
            InitializeComponent();
            Loaded += ConnectionInfoWindow_Loaded;
            Closing += ConnectionInfoWindow_Closing;
            LocalizationManager.LanguageChanged += OnLanguageChanged;
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Lifecycle Methods

        private void ConnectionInfoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartMonitoring();
        }

        private void ConnectionInfoWindow_Closing(object? sender, CancelEventArgs e)
        {
            StopMonitoring();
            StopPingMonitoring();
            _udpMonitor?.Dispose();
            _udpMonitor = null;
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
        }

        #endregion

        #region Monitoring

        private void StartMonitoring()
        {
            _monitoringTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _monitoringTimer.Tick += async (s, e) => await MonitorConnectionsAsync();
            _monitoringTimer.Start();

            // Первый запуск сразу
            _ = MonitorConnectionsAsync();
        }

        private void StopMonitoring()
        {
            _monitoringTimer?.Stop();
            _monitoringTimer = null;
        }

        private void StartUdpMonitor(int processId)
        {
            try
            {
                Debug.WriteLine("========================================");
                Debug.WriteLine("🚀 Запуск UDP монитора для игры...");
                Debug.WriteLine("========================================");

                // Получаем все UDP порты процесса DBD
                var udpConnections = GetActiveUdpConnections();
                var dbdPorts = udpConnections
                    .Where(c => c.ProcessId == processId)
                    .Select(c => c.LocalPort)
                    .Where(p => p > 0)
                    .ToHashSet();

                if (!dbdPorts.Any())
                {
                    Debug.WriteLine("⚠️ Не найдено UDP портов для DBD");
                    Debug.WriteLine("   Игра еще не начала матч, UDP порты появятся позже");
                    return;
                }

                Debug.WriteLine($"📡 Найдено {dbdPorts.Count} UDP портов DBD");
                Debug.WriteLine($"   Порты: {string.Join(", ", dbdPorts.Take(10))}");

                _udpMonitor = new UdpGameMonitor();
                _udpMonitor.GameServerDetected += UdpMonitor_GameServerDetected;
                
                Debug.WriteLine("🔧 Попытка запуска захвата пакетов...");
                if (_udpMonitor.StartCapture(dbdPorts))
                {
                    _udpMonitorStarted = true;
                    _monitoredUdpPorts = dbdPorts;
                    Debug.WriteLine("========================================");
                    Debug.WriteLine("✅✅✅ UDP монитор успешно запущен! ✅✅✅");
                    Debug.WriteLine("========================================");
                }
                else
                {
                    Debug.WriteLine("========================================");
                    Debug.WriteLine("❌ НЕ УДАЛОСЬ ЗАПУСТИТЬ UDP МОНИТОР");
                    Debug.WriteLine("========================================");
                    Debug.WriteLine("💡 Возможные решения:");
                    Debug.WriteLine("   1. Убедитесь что Npcap установлен с опцией 'WinPcap API-compatible Mode'");
                    Debug.WriteLine("   2. Закройте другие программы (Wireshark, VPN)");
                    Debug.WriteLine("   3. Попробуйте переустановить Npcap");
                    Debug.WriteLine("");
                    Debug.WriteLine("⚠️ TCP мониторинг (лобби) будет работать нормально!");
                    Debug.WriteLine("⚠️ UDP мониторинг (матч) недоступен");
                    
                    // Показываем сообщение пользователю
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            "UDP мониторинг недоступен.\n\n" +
                            "Будет работать только отображение TCP соединений (лобби).\n\n" +
                            "Для мониторинга UDP (матча) убедитесь что:\n" +
                            "• Npcap установлен с опцией 'WinPcap API-compatible Mode'\n" +
                            "• Закройте Wireshark или другие программы перехвата пакетов\n" +
                            "• Попробуйте переустановить Npcap\n\n" +
                            "Приложение продолжит работу без UDP мониторинга.",
                            "UDP мониторинг недоступен",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("========================================");
                Debug.WriteLine($"❌ КРИТИЧЕСКАЯ ОШИБКА UDP МОНИТОРА");
                Debug.WriteLine("========================================");
                Debug.WriteLine($"Тип: {ex.GetType().Name}");
                Debug.WriteLine($"Сообщение: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }
        }

        private void UdpMonitor_GameServerDetected(object? sender, UdpGameMonitor.GameServerInfo e)
        {
            Debug.WriteLine($"🎮 Обнаружен игровой сервер: {e.RemoteAddress}:{e.RemotePort}");
        }

        private async Task EnsureUdpMonitorRunningAsync(int processId)
        {
            // Получаем текущие UDP порты процесса DBD
            var udpConnections = GetActiveUdpConnections();
            var currentPorts = udpConnections
                .Where(c => c.ProcessId == processId)
                .Select(c => c.LocalPort)
                .Where(p => p > 0)
                .ToHashSet();

            // Если нет портов - останавливаем монитор если он был запущен
            if (!currentPorts.Any())
            {
                if (_udpMonitorStarted)
                {
                    Debug.WriteLine("========================================");
                    Debug.WriteLine("🚪 UDP порты исчезли - игрок вышел из матча");
                    Debug.WriteLine("========================================");
                    
                    // Останавливаем монитор
                    if (_udpMonitor != null)
                    {
                        Debug.WriteLine("🛑 Остановка UDP монитора...");
                        _udpMonitor.Dispose();
                        _udpMonitor = null;
                    }
                    
                    _udpMonitorStarted = false;
                    _monitoredUdpPorts.Clear();
                    
                    // Сбрасываем сохраненный IP игры для корректного обновления UI
                    _lastGameIp = null;
                    _lastGamePort = 0;
                    
                    // Останавливаем фоновый пингер
                    StopPingMonitoring();
                    
                    Debug.WriteLine("✅ UDP монитор остановлен");
                }
                else
                {
                    Debug.WriteLine("⚠️ UDP порты не найдены (игра еще не в матче)");
                }
                return;
            }

            // Проверяем, изменились ли порты
            bool portsChanged = !_monitoredUdpPorts.SetEquals(currentPorts);

            if (portsChanged)
            {
                Debug.WriteLine("========================================");
                Debug.WriteLine($"🔄 Обнаружены новые/измененные UDP порты");
                Debug.WriteLine($"   Старые порты ({_monitoredUdpPorts.Count}): {string.Join(", ", _monitoredUdpPorts.Take(5))}");
                Debug.WriteLine($"   Новые порты ({currentPorts.Count}): {string.Join(", ", currentPorts.Take(5))}");
                Debug.WriteLine("========================================");

                // Останавливаем старый монитор
                if (_udpMonitor != null)
                {
                    Debug.WriteLine("🛑 Остановка старого UDP монитора...");
                    _udpMonitor.Dispose();
                    _udpMonitor = null;
                    _udpMonitorStarted = false;
                }

                // Обновляем список отслеживаемых портов
                _monitoredUdpPorts = currentPorts;

                // Запускаем новый монитор
                StartUdpMonitor(processId);
            }
            else if (_udpMonitorStarted)
            {
                Debug.WriteLine($"✅ UDP монитор уже работает с {_monitoredUdpPorts.Count} портами");
            }

            await Task.CompletedTask;
        }

        private async Task<ConnectionInfo?> GetUdpConnectionFromMonitorAsync()
        {
            if (_udpMonitor == null)
                return null;

            var gameServer = _udpMonitor.GetActiveGameServer();
            if (gameServer == null)
                return null;

            Debug.WriteLine($"📊 Активный игровой сервер из монитора: {gameServer.RemoteAddress}:{gameServer.RemotePort}");

            var connectionInfo = new ConnectionInfo
            {
                Protocol = "UDP",
                RemoteAddress = gameServer.RemoteAddress,
                RemotePort = gameServer.RemotePort,
                LocalPort = gameServer.LocalPort,
                Ping = -1, // Пинг будет измерен фоновым мониторингом к GameLift хосту
                ProcessId = -1 // Не важно для отображения
            };

            // Обогащаем информацией о регионе AWS
            return await EnrichConnectionInfoAsync(connectionInfo);
        }


        private async Task MonitorConnectionsAsync()
        {
            try
            {
                var dbdProcess = GetDbdProcess();
                
                if (dbdProcess == null)
                {
                    Debug.WriteLine("DBD процесс не найден");
                    UpdateNoConnection();
                    return;
                }

                Debug.WriteLine($"DBD процесс найден: PID {dbdProcess.Id}");

                // Проверяем UDP порты и запускаем/перезапускаем UDP монитор при необходимости
                await EnsureUdpMonitorRunningAsync(dbdProcess.Id);

                // Получаем TCP соединения (для лобби)
                var tcpConnection = await GetTcpConnectionAsync(dbdProcess.Id);
                
                if (tcpConnection != null)
                {
                    Debug.WriteLine($"✅ TCP соединение найдено: {tcpConnection.RemoteAddress}:{tcpConnection.RemotePort}");
                }
                else
                {
                    Debug.WriteLine("⚠️ TCP соединение не найдено");
                }
                
                // Получаем UDP соединения (для игры) - только если монитор запущен
                ConnectionInfo? udpConnection = null;
                
                if (_udpMonitorStarted && _udpMonitor != null)
                {
                    // Монитор работает - пробуем получить соединение
                    udpConnection = await GetUdpConnectionFromMonitorAsync();
                }
                
                // Если не нашли через монитор, пробуем старый метод
                if (udpConnection == null)
                {
                    udpConnection = await GetUdpConnectionAsync(dbdProcess.Id);
                }
                
                if (udpConnection != null)
                {
                    Debug.WriteLine($"✅ UDP соединение найдено: {udpConnection.RemoteAddress}:{udpConnection.RemotePort}");
                }
                else
                {
                    Debug.WriteLine("⚠️ UDP соединение не найдено");
                }

                // Обновляем UI
                await UpdateUIAsync(tcpConnection, udpConnection);
                
                UpdateLastUpdateTime();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка мониторинга: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private Process? GetDbdProcess()
        {
            try
            {
                var processes = Process.GetProcessesByName(DBD_PROCESS_NAME);
                return processes.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private async Task<ConnectionInfo?> GetTcpConnectionAsync(int processId)
        {
            try
            {
                var connections = GetActiveTcpConnections();
                Debug.WriteLine($"Всего TCP соединений: {connections.Count}");
                
                // DBD использует HTTPS (порт 443) для связи с серверами лобби/матчмейкинга
                var dbdConnections = connections
                    .Where(c => c.ProcessId == processId)
                    .Where(c => c.State == TcpState.Established)
                    .Where(c => !IsLocalAddress(c.RemoteAddress))
                    .Where(c => c.RemotePort == 443) // DBD использует HTTPS
                    .ToList();

                Debug.WriteLine($"TCP соединений DBD на порту 443: {dbdConnections.Count}");

                if (dbdConnections.Any())
                {
                    // Проверяем, существует ли ещё предыдущее выбранное соединение
                    if (!string.IsNullOrEmpty(_lastLobbyIp) && _lastLobbyPort > 0)
                    {
                        var existingConn = dbdConnections.FirstOrDefault(c => 
                            c.RemoteAddress == _lastLobbyIp && c.RemotePort == _lastLobbyPort);
                        
                        if (existingConn != null)
                        {
                            Debug.WriteLine($"✅ Продолжаем использовать существующее TCP соединение: {existingConn.RemoteAddress}:{existingConn.RemotePort}");
                            return await EnrichConnectionInfoAsync(existingConn);
                        }
                        else
                        {
                            Debug.WriteLine($"⚠️ Предыдущее TCP соединение ({_lastLobbyIp}:{_lastLobbyPort}) больше не активно");
                            _lastLobbyIp = null;
                            _lastLobbyPort = 0;
                        }
                    }
                    
                    Debug.WriteLine($"Проверяем {dbdConnections.Count} соединений на принадлежность к AWS...");
                    
                    // Проверяем, есть ли соединения к AWS серверам
                    foreach (var conn in dbdConnections)
                    {
                        // Проверяем, является ли IP адресом AWS
                        var isAws = await AwsIpRangeManager.Instance.IsAwsIpAsync(conn.RemoteAddress);
                        Debug.WriteLine($"  {conn.RemoteAddress}:{conn.RemotePort} - AWS: {isAws}");
                        
                        if (isAws)
                        {
                            Debug.WriteLine($"✅ Выбрано новое TCP соединение к AWS: {conn.RemoteAddress}:{conn.RemotePort}");
                            _lastLobbyIp = conn.RemoteAddress;
                            _lastLobbyPort = conn.RemotePort;
                            var enriched = await EnrichConnectionInfoAsync(conn);
                            Debug.WriteLine($"   Обогащенная информация: Region={enriched.Region}, Ping={enriched.Ping}ms");
                            return enriched;
                        }
                    }
                    
                    // Если не нашли AWS, берем первое доступное соединение на 443
                    var firstConn = dbdConnections.First();
                    Debug.WriteLine($"⚠️ AWS не найден, выбрано первое соединение: {firstConn.RemoteAddress}:{firstConn.RemotePort}");
                    _lastLobbyIp = firstConn.RemoteAddress;
                    _lastLobbyPort = firstConn.RemotePort;
                    return await EnrichConnectionInfoAsync(firstConn);
                }

                Debug.WriteLine("⚠️ Нет подходящих TCP соединений");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка получения TCP соединений: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        private async Task<ConnectionInfo?> GetUdpConnectionAsync(int processId)
        {
            try
            {
                // Пробуем получить UDP соединения через PowerShell
                var udpFromPowerShell = await GetUdpConnectionsViaPowerShellAsync(processId);
                if (udpFromPowerShell != null)
                {
                    Debug.WriteLine($"Найдено UDP соединение через PowerShell: {udpFromPowerShell.RemoteAddress}:{udpFromPowerShell.RemotePort}");
                    return await EnrichConnectionInfoAsync(udpFromPowerShell);
                }

                var connections = GetActiveUdpConnections();
                
                // Сначала пытаемся найти UDP с удаленным адресом (если есть)
                var dbdConnection = connections
                    .Where(c => c.ProcessId == processId)
                    .Where(c => !string.IsNullOrEmpty(c.RemoteAddress) && c.RemoteAddress != "0.0.0.0")
                    .Where(c => !IsLocalAddress(c.RemoteAddress))
                    .OrderByDescending(c => c.RemotePort) // Предпочитаем порты выше
                    .FirstOrDefault();

                if (dbdConnection != null)
                {
                    Debug.WriteLine($"Найдено UDP соединение: {dbdConnection.RemoteAddress}:{dbdConnection.RemotePort}");
                    return await EnrichConnectionInfoAsync(dbdConnection);
                }

                // Если не нашли с удаленным адресом
                Debug.WriteLine("UDP соединения с удаленным адресом не найдены (это нормально для Windows netstat)");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка получения UDP соединений: {ex.Message}");
                return null;
            }
        }

        private async Task<ConnectionInfo?> GetUdpConnectionsViaPowerShellAsync(int processId)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"Get-NetUDPEndpoint | Where-Object {{ $_.OwningProcess -eq {processId} }} | Select-Object LocalAddress, LocalPort, RemoteAddress, RemotePort | ConvertTo-Json\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (!string.IsNullOrWhiteSpace(output) && output.Contains("LocalAddress"))
                    {
                        // Парсим JSON (простая обработка, можно улучшить)
                        // Пока что PowerShell тоже не даст нам удаленные адреса для UDP
                        //Debug.WriteLine($"PowerShell UDP output: {output}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка получения UDP через PowerShell: {ex.Message}");
            }

            return null;
        }

        private bool IsLocalAddress(string address)
        {
            if (string.IsNullOrEmpty(address)) return true;
            
            if (address.StartsWith("127.") || 
                address.StartsWith("192.168.") || 
                address.StartsWith("10.") ||
                address.StartsWith("172.16.") ||
                address.StartsWith("169.254.") ||
                address == "0.0.0.0" ||
                address == "::" ||
                address == "::1")
            {
                return true;
            }

            return false;
        }

        private async Task<ConnectionInfo> EnrichConnectionInfoAsync(ConnectionInfo connection)
        {
            // Измеряем пинг (только если еще не измерен)
            if (connection.Ping < 0)
            {
                try
                {
                    var reply = await _pinger.SendPingAsync(connection.RemoteAddress, 2000);
                    connection.Ping = reply.Status == IPStatus.Success ? reply.RoundtripTime : -1;
                }
                catch
                {
                    connection.Ping = -1;
                }
            }

            // Определяем регион AWS
            try
            {
                var region = await AwsIpRangeManager.Instance.GetAwsRegionAsync(connection.RemoteAddress);
                var service = await AwsIpRangeManager.Instance.GetAwsServiceAsync(connection.RemoteAddress);
                
                Debug.WriteLine($"🌍 AWS Region lookup для {connection.RemoteAddress}: region={region}, service={service}");
                
                if (!string.IsNullOrEmpty(region))
                {
                    connection.Region = FormatRegion(region);
                    connection.ServerName = $"{service} - {region}";
                }
                else
                {
                    // Fallback: пробуем определить через ip-api.com
                    Debug.WriteLine("⚠️ AWS region не найден, пробуем ip-api.com...");
                    var (regionName, countryCode) = await GetRegionViaIpApiAsync(connection.RemoteAddress);
                    
                    if (!string.IsNullOrEmpty(regionName))
                    {
                        connection.Region = regionName;
                        connection.ServerName = $"{LocalizationManager.ServerPrefix}{regionName}";
                        Debug.WriteLine($"✅ Регион определен через ip-api: {regionName}");
                    }
                    else
                    {
                        connection.Region = LocalizationManager.UnknownRegion;
                        connection.ServerName = LocalizationManager.GetString("ServerIP", connection.RemoteAddress);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка определения региона: {ex.Message}");
                connection.Region = LocalizationManager.UnknownRegion;
                connection.ServerName = LocalizationManager.GetString("ServerIP", connection.RemoteAddress);
            }

            return connection;
        }

        private async Task<(string regionName, string countryCode)> GetRegionViaIpApiAsync(string ip)
        {
            try
            {
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                
                var response = await httpClient.GetStringAsync($"http://ip-api.com/json/{ip}?fields=status,country,countryCode,regionName,city");
                
                // Простой парсинг JSON (без зависимостей)
                if (response.Contains("\"status\":\"success\""))
                {
                    var country = ExtractJsonValue(response, "country");
                    var countryCode = ExtractJsonValue(response, "countryCode");
                    var regionName = ExtractJsonValue(response, "regionName");
                    var city = ExtractJsonValue(response, "city");
                    
                    var fullName = "";
                    if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(regionName))
                        fullName = $"{GetFlag(countryCode)} {city}, {regionName}, {country}";
                    else if (!string.IsNullOrEmpty(regionName))
                        fullName = $"{GetFlag(countryCode)} {regionName}, {country}";
                    else
                        fullName = $"{GetFlag(countryCode)} {country}";
                    
                    return (fullName, countryCode);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка ip-api: {ex.Message}");
            }
            
            return ("", "");
        }

        private string ExtractJsonValue(string json, string key)
        {
            try
            {
                var searchKey = $"\"{key}\":\"";
                var startIndex = json.IndexOf(searchKey);
                if (startIndex < 0) return "";
                
                startIndex += searchKey.Length;
                var endIndex = json.IndexOf("\"", startIndex);
                if (endIndex < 0) return "";
                
                return json.Substring(startIndex, endIndex - startIndex);
            }
            catch
            {
                return "";
            }
        }

        private string GetFlag(string countryCode)
        {
            var flags = new Dictionary<string, string>
            {
                { "US", "🇺🇸" }, { "GB", "🇬🇧" }, { "DE", "🇩🇪" }, { "FR", "🇫🇷" },
                { "IE", "🇮🇪" }, { "SE", "🇸🇪" }, { "IT", "🇮🇹" }, { "JP", "🇯🇵" },
                { "KR", "🇰🇷" }, { "SG", "🇸🇬" }, { "AU", "🇦🇺" }, { "IN", "🇮🇳" },
                { "BR", "🇧🇷" }, { "CA", "🇨🇦" }, { "BH", "🇧🇭" }, { "ZA", "🇿🇦" },
                { "HK", "🇭🇰" }, { "CN", "🇨🇳" }
            };
            
            return flags.TryGetValue(countryCode, out var flag) ? flag : "🌍";
        }

        private string FormatRegion(string region)
        {
            var regionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "us-east-1", "🇺🇸 US East (N. Virginia)" },
                { "us-east-2", "🇺🇸 US East (Ohio)" },
                { "us-west-1", "🇺🇸 US West (N. California)" },
                { "us-west-2", "🇺🇸 US West (Oregon)" },
                { "eu-west-1", "🇮🇪 EU West (Ireland)" },
                { "eu-west-2", "🇬🇧 EU West (London)" },
                { "eu-west-3", "🇫🇷 EU West (Paris)" },
                { "eu-central-1", "🇩🇪 EU Central (Frankfurt)" },
                { "eu-north-1", "🇸🇪 EU North (Stockholm)" },
                { "eu-south-1", "🇮🇹 EU South (Milan)" },
                { "ap-northeast-1", "🇯🇵 Asia Pacific (Tokyo)" },
                { "ap-northeast-2", "🇰🇷 Asia Pacific (Seoul)" },
                { "ap-northeast-3", "🇯🇵 Asia Pacific (Osaka)" },
                { "ap-southeast-1", "🇸🇬 Asia Pacific (Singapore)" },
                { "ap-southeast-2", "🇦🇺 Asia Pacific (Sydney)" },
                { "ap-south-1", "🇮🇳 Asia Pacific (Mumbai)" },
                { "sa-east-1", "🇧🇷 South America (São Paulo)" },
                { "ca-central-1", "🇨🇦 Canada (Central)" },
                { "me-south-1", "🇧🇭 Middle East (Bahrain)" },
                { "af-south-1", "🇿🇦 Africa (Cape Town)" },
                { "ap-east-1", "🇭🇰 Asia Pacific (Hong Kong)" },
                { "cn-north-1", "🇨🇳 China (Beijing)" },
                { "cn-northwest-1", "🇨🇳 China (Ningxia)" }
            };

            return regionMap.TryGetValue(region, out var formatted) ? formatted : region;
        }

        #endregion

        #region Network Info Retrieval

        private List<ConnectionInfo> GetActiveTcpConnections()
        {
            var connections = new List<ConnectionInfo>();

            // Пытаемся через WMI
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "root\\StandardCimv2",
                    "SELECT * FROM MSFT_NetTCPConnection WHERE State = 5"); // 5 = Established
                
                using var results = searcher.Get();
                
                foreach (ManagementObject obj in results)
                {
                    try
                    {
                        var connection = new ConnectionInfo
                        {
                            Protocol = "TCP",
                            LocalAddress = obj["LocalAddress"]?.ToString() ?? "",
                            LocalPort = Convert.ToInt32(obj["LocalPort"]),
                            RemoteAddress = obj["RemoteAddress"]?.ToString() ?? "",
                            RemotePort = Convert.ToInt32(obj["RemotePort"]),
                            ProcessId = Convert.ToInt32(obj["OwningProcess"]),
                            State = TcpState.Established
                        };
                        connections.Add(connection);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка получения TCP соединений через WMI: {ex.Message}");
            }

            // Если WMI не сработал, пробуем через netstat
            if (connections.Count == 0)
            {
                try
                {
                    connections = GetTcpConnectionsFromNetstat();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка получения TCP соединений через netstat: {ex.Message}");
                }
            }

            return connections;
        }

        private List<ConnectionInfo> GetTcpConnectionsFromNetstat()
        {
            var connections = new List<ConnectionInfo>();
            
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netstat",
                    Arguments = "-ano -p TCP",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    var lines = output.Split('\n');
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        
                        // Формат: TCP  LocalAddress:Port  RemoteAddress:Port  State  PID
                        if (parts.Length >= 5 && parts[0] == "TCP")
                        {
                            try
                            {
                                var state = parts[3];
                                if (state != "ESTABLISHED") continue;

                                var processId = int.Parse(parts[4]);
                                
                                var localParts = parts[1].Split(':');
                                var remoteParts = parts[2].Split(':');

                                var localAddress = localParts.Length > 1 
                                    ? string.Join(":", localParts.Take(localParts.Length - 1))
                                    : localParts[0];
                                var remoteAddress = remoteParts.Length > 1
                                    ? string.Join(":", remoteParts.Take(remoteParts.Length - 1))
                                    : remoteParts[0];

                                // Очищаем IPv6 скобки если есть
                                localAddress = localAddress.Trim('[', ']');
                                remoteAddress = remoteAddress.Trim('[', ']');

                                var connection = new ConnectionInfo
                                {
                                    Protocol = "TCP",
                                    LocalAddress = localAddress,
                                    LocalPort = int.Parse(localParts[^1]),
                                    RemoteAddress = remoteAddress,
                                    RemotePort = int.Parse(remoteParts[^1]),
                                    ProcessId = processId,
                                    State = TcpState.Established
                                };

                                connections.Add(connection);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Ошибка парсинга строки netstat TCP: {line}, {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка выполнения netstat TCP: {ex.Message}");
            }

            return connections;
        }

        private List<ConnectionInfo> GetActiveUdpConnections()
        {
            var connections = new List<ConnectionInfo>();

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM MSFT_NetUDPEndpoint");
                
                using var results = searcher.Get();
                
                foreach (ManagementObject obj in results)
                {
                    try
                    {
                        var connection = new ConnectionInfo
                        {
                            Protocol = "UDP",
                            LocalAddress = obj["LocalAddress"]?.ToString() ?? "",
                            LocalPort = Convert.ToInt32(obj["LocalPort"]),
                            RemoteAddress = "", // UDP не имеет удаленного адреса в статике
                            RemotePort = 0,
                            ProcessId = Convert.ToInt32(obj["OwningProcess"])
                        };
                        connections.Add(connection);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка получения UDP соединений через WMI: {ex.Message}");
            }

            // Для UDP пытаемся получить дополнительную информацию через netstat
            try
            {
                var processId = Process.GetProcessesByName(DBD_PROCESS_NAME).FirstOrDefault()?.Id;
                if (processId.HasValue)
                {
                    var udpConnections = GetUdpConnectionsFromNetstat(processId.Value);
                    connections.AddRange(udpConnections);
                }
            }
            catch { }

            return connections;
        }

        private List<ConnectionInfo> GetUdpConnectionsFromNetstat(int targetProcessId)
        {
            var connections = new List<ConnectionInfo>();
            
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netstat",
                    Arguments = "-ano -p UDP",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    var lines = output.Split('\n');
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        
                        // Формат UDP в netstat: UDP  LocalAddress:Port  *:*  PID
                        // Или: UDP  LocalAddress:Port  RemoteAddress:Port  PID (редко)
                        if (parts.Length >= 4 && parts[0] == "UDP")
                        {
                            try
                            {
                                var processId = int.Parse(parts[^1]);
                                if (processId == targetProcessId)
                                {
                                    var localParts = parts[1].Split(':');
                                    
                                    var localAddress = localParts.Length > 1 
                                        ? string.Join(":", localParts.Take(localParts.Length - 1))
                                        : localParts[0];
                                    
                                    // Очищаем IPv6 скобки если есть
                                    localAddress = localAddress.Trim('[', ']');

                                    // Проверяем есть ли удаленный адрес (не *:*)
                                    string remoteAddress = "";
                                    int remotePort = 0;
                                    
                                    if (parts.Length > 2 && parts[2] != "*:*" && parts[2].Contains(':'))
                                    {
                                        var remoteParts = parts[2].Split(':');
                                        remoteAddress = remoteParts.Length > 1 
                                            ? string.Join(":", remoteParts.Take(remoteParts.Length - 1))
                                            : remoteParts[0];
                                        remoteAddress = remoteAddress.Trim('[', ']');
                                        
                                        if (int.TryParse(remoteParts[^1], out var port))
                                        {
                                            remotePort = port;
                                        }
                                    }

                                    var connection = new ConnectionInfo
                                    {
                                        Protocol = "UDP",
                                        LocalAddress = localAddress,
                                        LocalPort = int.Parse(localParts[^1]),
                                        RemoteAddress = remoteAddress,
                                        RemotePort = remotePort,
                                        ProcessId = processId
                                    };

                                    // Для UDP добавляем все соединения, даже без удаленного адреса
                                    // Потому что UDP может показывать только локальные порты
                                    connections.Add(connection);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Ошибка парсинга строки netstat UDP: {line}, {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка получения UDP соединений из netstat: {ex.Message}");
            }

            return connections;
        }

        #endregion

        #region UI Updates

        private Task UpdateUIAsync(ConnectionInfo? tcpConnection, ConnectionInfo? udpConnection)
        {
            Debug.WriteLine($"🔄 Обновление UI: TCP={tcpConnection != null}, UDP={udpConnection != null}");
            
            _currentLobbyConnection = tcpConnection;
            _currentGameConnection = udpConnection;

            // Обновляем TCP (лобби)
            if (tcpConnection != null)
            {
                Debug.WriteLine($"   TCP: {tcpConnection.RemoteAddress}:{tcpConnection.RemotePort}, Region={tcpConnection.Region}, Ping={tcpConnection.Ping}ms");
                
                LobbyStatusText.Text = LocalizationManager.Connected;
                LobbyStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45)); // Green
                
                LobbyIpText.Text = $"{tcpConnection.RemoteAddress}:{tcpConnection.RemotePort}";
                CopyLobbyIpButton.Visibility = Visibility.Visible;
                
                LobbyServerText.Text = tcpConnection.ServerName;
                LobbyRegionText.Text = tcpConnection.Region;
                
                LobbyPingText.Text = tcpConnection.Ping >= 0 
                    ? $"{tcpConnection.Ping} ms" 
                    : LocalizationManager.NotMeasured;
                LobbyPingText.Foreground = GetPingColor(tcpConnection.Ping);
                
                Debug.WriteLine($"   ✅ UI лобби обновлен");
            }
            else
            {
                LobbyStatusText.Text = LocalizationManager.NotConnected;
                LobbyStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0x14, 0x3C)); // Red
                
                LobbyIpText.Text = LocalizationManager.NotDetermined;
                CopyLobbyIpButton.Visibility = Visibility.Collapsed;
                
                LobbyServerText.Text = LocalizationManager.NotDetermined;
                LobbyRegionText.Text = LocalizationManager.NotDetermined;
                LobbyPingText.Text = LocalizationManager.NotMeasured;
                LobbyPingText.Foreground = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
                
                Debug.WriteLine($"   ⚠️ TCP отсутствует, показываем 'Не подключено'");
                
                // Сбрасываем сохраненный IP лобби
                _lastLobbyIp = null;
                _lastLobbyPort = 0;
            }

            // Обновляем UDP (игра)
            if (udpConnection != null)
            {
                GameStatusText.Text = LocalizationManager.Connected;
                GameStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45)); // Green
                
                GameIpText.Text = $"{udpConnection.RemoteAddress}:{udpConnection.RemotePort}";
                CopyGameIpButton.Visibility = Visibility.Visible;
                
                GameServerText.Text = udpConnection.ServerName;
                GameRegionText.Text = udpConnection.Region;
                
                // Проверяем, изменился ли IP адрес игрового сервера
                // Сравниваем с последним сохраненным IP игры (не с GameLift хостом!)
                bool ipChanged = _lastGameIp != udpConnection.RemoteAddress || 
                                 _lastGamePort != udpConnection.RemotePort;
                
                Debug.WriteLine($"🔍 Проверка изменения IP: last={_lastGameIp}:{_lastGamePort}, current={udpConnection.RemoteAddress}:{udpConnection.RemotePort}, changed={ipChanged}");
                
                // Обновляем пинг ТОЛЬКО если IP изменился (чтобы избежать моргания)
                // Иначе фоновый мониторинг уже обновляет пинг каждую секунду
                if (ipChanged)
                {
                    Debug.WriteLine($"🔄 IP игрового сервера изменился, перезапускаем пингер");
                    
                    // Сохраняем новый IP ДО запуска пингера
                    _lastGameIp = udpConnection.RemoteAddress;
                    _lastGamePort = udpConnection.RemotePort;
                    
                    // Показываем начальное значение пинга или "Измеряется..."
                    if (udpConnection.Ping >= 0)
                    {
                        GamePingText.Text = $"{udpConnection.Ping} ms";
                        GamePingText.Foreground = GetPingColor(udpConnection.Ping);
                    }
                    else
                    {
                        GamePingText.Text = LocalizationManager.Measuring;
                        GamePingText.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)); // Yellow
                    }
                    
                    // Запускаем DispatcherTimer для мониторинга пинга (остановит старый если был)
                    StartPingMonitoring(udpConnection.RemoteAddress, udpConnection.RemotePort);
                }
                else
                {
                    Debug.WriteLine($"✅ IP игрового сервера не изменился, продолжаем использовать текущий пинг");
                }
                // Если IP не изменился, просто продолжаем мониторинг (ничего не делаем)
                // Фоновый таймер пинга уже обновляет GamePingText каждую секунду
            }
            else
            {
                // Нет UDP соединения - сбрасываем статус игры
                Debug.WriteLine("⚠️ UDP соединение не найдено - сбрасываем статус игры");
                
                GameStatusText.Text = LocalizationManager.NotConnected;
                GameStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0x14, 0x3C)); // Red
                
                GameIpText.Text = LocalizationManager.NotDetermined;
                CopyGameIpButton.Visibility = Visibility.Collapsed;
                
                GameServerText.Text = LocalizationManager.NotDetermined;
                GameRegionText.Text = LocalizationManager.NotDetermined;
                GamePingText.Text = LocalizationManager.NotMeasured;
                GamePingText.Foreground = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
                
                // Сбрасываем сохраненный IP игры
                _lastGameIp = null;
                _lastGamePort = 0;
                
                // Останавливаем мониторинг пинга
                StopPingMonitoring();
            }
            
            return Task.CompletedTask;
        }

        private void UpdateNoConnection()
        {
            LobbyStatusText.Text = LocalizationManager.GameNotRunning;
            LobbyStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)); // Gray
            LobbyIpText.Text = LocalizationManager.NotDetermined;
            CopyLobbyIpButton.Visibility = Visibility.Collapsed;
            LobbyServerText.Text = LocalizationManager.NotDetermined;
            LobbyRegionText.Text = LocalizationManager.NotDetermined;
            LobbyPingText.Text = LocalizationManager.NotMeasured;

            GameStatusText.Text = LocalizationManager.GameNotRunning;
            GameStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)); // Gray
            GameIpText.Text = LocalizationManager.NotDetermined;
            CopyGameIpButton.Visibility = Visibility.Collapsed;
            GameServerText.Text = LocalizationManager.NotDetermined;
            GameRegionText.Text = LocalizationManager.NotDetermined;
            GamePingText.Text = LocalizationManager.NotMeasured;
            
            // Сбрасываем сохраненные IP
            _lastLobbyIp = null;
            _lastLobbyPort = 0;
            _lastGameIp = null;
            _lastGamePort = 0;
            
            // Очищаем отслеживаемые порты
            _monitoredUdpPorts.Clear();
            _udpMonitorStarted = false;
            
            // Останавливаем фоновый мониторинг пинга
            StopPingMonitoring();
        }

        private void UpdateLastUpdateTime()
        {
            LastUpdateText.Text = $"{DateTime.Now:dd.MM.yyyy HH:mm:ss}";
        }

        private SolidColorBrush GetPingColor(long ping)
        {
            if (ping < 0) return new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)); // Gray
            if (ping < 80) return new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45)); // Green
            if (ping < 130) return new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)); // Yellow
            if (ping < 250) return new SolidColorBrush(Color.FromRgb(0xDC, 0x14, 0x3C)); // Red
            return new SolidColorBrush(Color.FromRgb(0x6F, 0x42, 0xC1)); // Purple
        }

        #endregion

        #region Background Ping Monitoring

        // Маппинг AWS регионов на GameLift хосты (точно как на главной странице)
        // hosts[0] - основной endpoint, hosts[1] - специальный ping endpoint
        private readonly Dictionary<string, string[]> _awsRegionToGameLiftHosts = new()
        {
            // Europe
            { "eu-west-2", new[]{ "gamelift.eu-west-2.amazonaws.com", "gamelift-ping.eu-west-2.api.aws" } },      // London
            { "eu-west-1", new[]{ "gamelift.eu-west-1.amazonaws.com", "gamelift-ping.eu-west-1.api.aws" } },      // Ireland
            { "eu-central-1", new[]{ "gamelift.eu-central-1.amazonaws.com", "gamelift-ping.eu-central-1.api.aws" } }, // Frankfurt
            
            // Americas
            { "us-east-1", new[]{ "gamelift.us-east-1.amazonaws.com", "gamelift-ping.us-east-1.api.aws" } },      // N. Virginia
            { "us-east-2", new[]{ "gamelift.us-east-2.amazonaws.com", "gamelift-ping.us-east-2.api.aws" } },      // Ohio
            { "us-west-1", new[]{ "gamelift.us-west-1.amazonaws.com", "gamelift-ping.us-west-1.api.aws" } },      // N. California
            { "us-west-2", new[]{ "gamelift.us-west-2.amazonaws.com", "gamelift-ping.us-west-2.api.aws" } },      // Oregon
            { "ca-central-1", new[]{ "gamelift.ca-central-1.amazonaws.com", "gamelift-ping.ca-central-1.api.aws" } }, // Canada
            { "sa-east-1", new[]{ "gamelift.sa-east-1.amazonaws.com", "gamelift-ping.sa-east-1.api.aws" } },      // São Paulo
            
            // Asia Pacific
            { "ap-northeast-1", new[]{ "gamelift.ap-northeast-1.amazonaws.com", "gamelift-ping.ap-northeast-1.api.aws" } }, // Tokyo
            { "ap-northeast-2", new[]{ "gamelift.ap-northeast-2.amazonaws.com", "gamelift-ping.ap-northeast-2.api.aws" } }, // Seoul
            { "ap-south-1", new[]{ "gamelift.ap-south-1.amazonaws.com", "gamelift-ping.ap-south-1.api.aws" } },        // Mumbai
            { "ap-southeast-1", new[]{ "gamelift.ap-southeast-1.amazonaws.com", "gamelift-ping.ap-southeast-1.api.aws" } }, // Singapore
            { "ap-east-1", new[]{ "ec2.ap-east-1.amazonaws.com", "gamelift-ping.ap-east-1.api.aws" } },               // Hong Kong
            { "ap-southeast-2", new[]{ "gamelift.ap-southeast-2.amazonaws.com", "gamelift-ping.ap-southeast-2.api.aws" } }, // Sydney
            
            // China
            { "cn-north-1", new[]{ "gamelift.cn-north-1.amazonaws.com.cn" } },     // Beijing
            { "cn-northwest-1", new[]{ "gamelift.cn-northwest-1.amazonaws.com.cn" } }, // Ningxia
        };

        /// <summary>
        /// Запускает фоновый мониторинг пинга для игрового сервера
        /// Пингует GameLift хост того же AWS региона (определяет регион через DNS hostname)
        /// Использует DispatcherTimer для гарантии синхронности
        /// </summary>
        private async void StartPingMonitoring(string ipAddress, int port)
        {
            Debug.WriteLine($"🎬 Запрос на запуск пингера для {ipAddress}:{port}");
            
            // Останавливаем старый таймер если был
            StopPingMonitoring();
            
            Debug.WriteLine($"✅ Старый пингер остановлен, запускаем новый");

            _currentGameServerIp = ipAddress;
            _currentGameServerPort = port;

            // Определяем AWS регион через Reverse DNS (PTR запись)
            string awsRegion = await GetAwsRegionFromDnsAsync(ipAddress);
            
            Debug.WriteLine($"🌍 Игровой сервер {ipAddress} → регион: {awsRegion}");

            // Ищем соответствующий GameLift хост (точно как на главной странице)
            if (string.IsNullOrEmpty(awsRegion) || 
                !_awsRegionToGameLiftHosts.TryGetValue(awsRegion, out var hosts) || hosts.Length == 0)
            {
                Debug.WriteLine($"⚠️ Не найден GameLift хост для региона '{awsRegion}', используем прямой пинг");
                _currentGameServerIp = ipAddress; // Fallback - пингуем сам IP
            }
            else
            {
                // Пингуем hosts[0] как на главной странице
                _currentGameServerIp = hosts[0];
                Debug.WriteLine($"✅ Будем пинговать GameLift хост: {_currentGameServerIp}");
            }

            Debug.WriteLine($"🏓 Запуск DispatcherTimer пинга к {_currentGameServerIp} (строго 1 раз/сек)");

            // Создаём отдельный Ping объект для фонового мониторинга
            _backgroundPinger = new Ping();

            // Создаём DispatcherTimer (работает в UI потоке, гарантирует синхронность)
            _pingTimer = new DispatcherTimer 
            { 
                Interval = TimeSpan.FromSeconds(1) 
            };
            _pingTimer.Tick += async (_, __) => await UpdatePingAsync();
            _pingTimer.Start();
            
            Debug.WriteLine($"✅ DispatcherTimer успешно запущен!");
            
            // Выполняем первый пинг сразу (не ждём 1 секунду)
            _ = UpdatePingAsync();
        }

        /// <summary>
        /// Определяет AWS регион по hostname через Reverse DNS
        /// Например: ec2-63-176-61-172.eu-central-1.compute.amazonaws.com → eu-central-1
        /// </summary>
        private async Task<string> GetAwsRegionFromDnsAsync(string ipAddress)
        {
            try
            {
                Debug.WriteLine($"🔍 Reverse DNS lookup для {ipAddress}...");
                
                var hostEntry = await Dns.GetHostEntryAsync(ipAddress);
                var hostname = hostEntry.HostName;
                
                Debug.WriteLine($"   Hostname: {hostname}");
                
                // Парсим AWS регион из hostname
                // Формат: ec2-X-X-X-X.REGION.compute.amazonaws.com
                // Или: X-X-X-X.REGION.elb.amazonaws.com
                if (hostname.Contains(".amazonaws.com"))
                {
                    var parts = hostname.Split('.');
                    
                    // Ищем часть которая выглядит как AWS регион (содержит дефисы и цифру)
                    foreach (var part in parts)
                    {
                        // AWS регион формат: us-east-1, eu-central-1, ap-northeast-2 и т.д.
                        if (part.Contains('-') && System.Text.RegularExpressions.Regex.IsMatch(part, @"^[a-z]{2}-[a-z]+-\d+$"))
                        {
                            Debug.WriteLine($"✅ Найден AWS регион из DNS: {part}");
                            return part;
                        }
                    }
                    
                    Debug.WriteLine("⚠️ AWS hostname найден, но регион не распознан");
                }
                else
                {
                    Debug.WriteLine($"⚠️ Hostname не AWS ({hostname})");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Reverse DNS ошибка: {ex.Message}");
            }
            
            return string.Empty;
        }

        /// <summary>
        /// Останавливает фоновый мониторинг пинга
        /// Просто и надёжно: останавливает DispatcherTimer
        /// </summary>
        private void StopPingMonitoring()
        {
            if (_pingTimer == null)
            {
                return; // Таймер не работает
            }
            
            Debug.WriteLine("🛑 Остановка DispatcherTimer пинга");
            
            // Останавливаем таймер
            _pingTimer.Stop();
            _pingTimer = null;
            
            // Освобождаем ресурсы
            if (_backgroundPinger != null)
            {
                _backgroundPinger.Dispose();
                _backgroundPinger = null;
            }
            
            _currentGameServerIp = null;
            _currentGameServerPort = 0;
            
            Debug.WriteLine("✅ DispatcherTimer остановлен");
        }

        /// <summary>
        /// Обновляет пинг к игровому серверу (вызывается DispatcherTimer каждую секунду)
        /// Работает в UI потоке, гарантирует синхронность
        /// </summary>
        private async Task UpdatePingAsync()
        {
            if (string.IsNullOrEmpty(_currentGameServerIp) || _backgroundPinger == null)
            {
                Debug.WriteLine("⚠️ UpdatePingAsync: пингер не инициализирован или IP пуст");
                return;
            }

            long ping = -1;
            
            try
            {
                Debug.WriteLine($"🏓 Попытка пинга к {_currentGameServerIp}...");
                
                // Простой ICMP ping к GameLift хосту (как на главной странице)
                var reply = await _backgroundPinger.SendPingAsync(_currentGameServerIp, 2000);
                
                if (reply.Status == IPStatus.Success)
                {
                    ping = reply.RoundtripTime;
                    Debug.WriteLine($"✅ Пинг успешен: {ping}ms");
                }
                else
                {
                    Debug.WriteLine($"❌ Хост {_currentGameServerIp} не отвечает: {reply.Status}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка пинга: {ex.Message}");
                Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
            }

            // Обновляем UI (уже в UI потоке благодаря DispatcherTimer!)
            // ВАЖНО: обновляем только при успешном пинге, чтобы избежать моргания при временных неудачах
            if (ping >= 0)
            {
                GamePingText.Text = $"{ping} ms";
                GamePingText.Foreground = GetPingColor(ping);
                Debug.WriteLine($"   ✅ UI обновлен: {ping}ms");
            }
            else
            {
                // Если пинг не удалось измерить - оставляем предыдущее значение
                // Это предотвращает моргание "50ms" -> "Не измерен" -> "50ms"
                Debug.WriteLine("   ⚠️ Пинг не удался, оставляем предыдущее значение");
            }
        }

        #endregion

        #region Event Handlers

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await MonitorConnectionsAsync();
        }

        private void CopyLobbyIpButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLobbyConnection != null)
            {
                var text = $"{_currentLobbyConnection.RemoteAddress}:{_currentLobbyConnection.RemotePort}";
                Clipboard.SetText(text);
                ShowCopyNotification(LocalizationManager.LobbyCopied);
            }
        }

        private void CopyGameIpButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentGameConnection != null)
            {
                var text = $"{_currentGameConnection.RemoteAddress}:{_currentGameConnection.RemotePort}";
                Clipboard.SetText(text);
                ShowCopyNotification(LocalizationManager.MatchCopied);
            }
        }

        private void ShowCopyNotification(string message)
        {
            // Можно добавить всплывающее уведомление, пока просто меняем текст временно
            var originalText = LastUpdateText.Text;
            LastUpdateText.Text = message;
            LastUpdateText.Foreground = new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45));
            
            Task.Delay(2000).ContinueWith(_ => 
            {
                Dispatcher.Invoke(() => 
                {
                    LastUpdateText.Text = originalText;
                    LastUpdateText.Foreground = new SolidColorBrush(Colors.White);
                });
            });
        }

        #endregion

        #region Language Change Handler

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            // Update window title
            Title = LocalizationManager.ConnectionInfoTitle;
            
            // Note: Static bindings {x:Static} in XAML don't auto-update when language changes.
            // The dynamic text elements (status, IP, etc.) will be updated on the next monitoring cycle
            // through UpdateUIAsync, UpdateNoConnection, etc.
            // For full language switching support, the window would need to be recreated.
        }

        #endregion

        #region Helper Classes

        private class ConnectionInfo
        {
            public string Protocol { get; set; } = "";
            public string LocalAddress { get; set; } = "";
            public int LocalPort { get; set; }
            public string RemoteAddress { get; set; } = "";
            public int RemotePort { get; set; }
            public int ProcessId { get; set; }
            public TcpState State { get; set; }
            public long Ping { get; set; } = -1;
            public string Region { get; set; } = "";
            public string ServerName { get; set; } = "";
        }

        #endregion
    }
}
