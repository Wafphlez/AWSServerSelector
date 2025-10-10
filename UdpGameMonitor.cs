using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SharpPcap;
using PacketDotNet;

namespace AWSServerSelector
{
    /// <summary>
    /// Мониторинг UDP трафика Dead by Daylight для определения IP сервера игры и пинга
    /// </summary>
    public class UdpGameMonitor : IDisposable
    {
        private ILiveDevice? _device;
        private bool _isCapturing = false;
        private CancellationTokenSource? _cts;
        
        // Хранилище обнаруженных игровых серверов
        private ConcurrentDictionary<string, GameServerInfo> _gameServers = new();
        
        // Порты DBD процесса для фильтрации
        private HashSet<int> _dbdPorts = new();
        
        public event EventHandler<GameServerInfo>? GameServerDetected;

        public class GameServerInfo
        {
            public string RemoteAddress { get; set; } = "";
            public int RemotePort { get; set; }
            public int LocalPort { get; set; }
            public DateTime LastSeen { get; set; }
            public long PacketCount { get; set; }
            public long BytesReceived { get; set; }
            public long BytesSent { get; set; }
        }

        /// <summary>
        /// Получить активный игровой сервер (с наибольшей активностью)
        /// </summary>
        public GameServerInfo? GetActiveGameServer()
        {
            // Очищаем старые записи (старше 10 секунд - быстрее определяем выход из матча)
            var cutoff = DateTime.Now.AddSeconds(-10);
            var activeServers = _gameServers
                .Where(kvp => kvp.Value.LastSeen > cutoff)
                .Select(kvp => kvp.Value)
                .ToList();

            if (!activeServers.Any())
                return null;

            // Возвращаем сервер с наибольшей активностью
            return activeServers
                .OrderByDescending(s => s.PacketCount)
                .ThenByDescending(s => s.BytesReceived + s.BytesSent)
                .FirstOrDefault();
        }

        /// <summary>
        /// Запустить мониторинг для указанных портов DBD
        /// </summary>
        public bool StartCapture(HashSet<int> dbdPorts)
        {
            try
            {
                _dbdPorts = dbdPorts;
                
                Debug.WriteLine($"🔍 Запуск мониторинга UDP для портов: {string.Join(", ", dbdPorts.Take(5))}...");

                // Получаем список устройств
                var devices = CaptureDeviceList.Instance;
                
                Debug.WriteLine($"📡 Найдено устройств: {devices.Count}");
                
                if (devices.Count == 0)
                {
                    Debug.WriteLine("❌ Не найдено сетевых устройств. Убедитесь, что установлен Npcap.");
                    Debug.WriteLine("💡 Скачайте Npcap: https://npcap.com/#download");
                    Debug.WriteLine("💡 При установке включите: 'Install in WinPcap API-compatible Mode'");
                    return false;
                }

                // Выводим список всех устройств для диагностики
                Debug.WriteLine("📋 Список доступных сетевых устройств:");
                for (int i = 0; i < devices.Count; i++)
                {
                    var dev = devices[i];
                    Debug.WriteLine($"  [{i}] {dev.Name}");
                    Debug.WriteLine($"      Описание: {dev.Description}");
                    if (dev is ILiveDevice liveDevice)
                    {
                        Debug.WriteLine($"      Started: {liveDevice.Started}");
                        // LinkType можно получить только после открытия устройства
                    }
                }

                // Ищем подходящее устройство - НЕ loopback, НЕ WAN Miniport
                Debug.WriteLine("🔍 Поиск подходящего устройства...");
                _device = devices
                    .OfType<ILiveDevice>()
                    .FirstOrDefault(d => d.Started == false && 
                                       !d.Name.Contains("Loopback") &&
                                       !d.Description.Contains("Miniport") &&
                                       !d.Description.Contains("Pseudo") &&
                                       (d.Description.Contains("Ethernet") || 
                                        d.Description.Contains("Wi-Fi") ||
                                        d.Description.Contains("Wireless") ||
                                        d.Description.Contains("802.11")));

                if (_device == null)
                {
                    Debug.WriteLine("⚠️ Не найдено Ethernet/WiFi устройство, пробуем любое физическое...");
                    // Берем первое доступное не-loopback, не-miniport устройство
                    _device = devices
                        .OfType<ILiveDevice>()
                        .FirstOrDefault(d => d.Started == false && 
                                           !d.Name.Contains("Loopback") &&
                                           !d.Description.Contains("Miniport") &&
                                           !d.Description.Contains("Pseudo"));
                }

                if (_device == null)
                {
                    Debug.WriteLine("❌ Не удалось найти подходящее сетевое устройство");
                    return false;
                }

                Debug.WriteLine($"✅ Выбрано устройство: {_device.Name}");
                Debug.WriteLine($"   Описание: {_device.Description}");

                // Настраиваем устройство
                _device.OnPacketArrival += Device_OnPacketArrival;
                
                Debug.WriteLine("🔓 Открываем устройство...");
                
                // Открываем устройство в promiscuous mode
                try
                {
                    _device.Open(new DeviceConfiguration
                    {
                        Mode = DeviceModes.Promiscuous,
                        ReadTimeout = 1000,
                        Snaplen = 65536
                    });
                    
                    Debug.WriteLine("✅ Устройство успешно открыто");
                }
                catch (SharpPcap.DeviceNotReadyException ex)
                {
                    Debug.WriteLine($"❌ Устройство не готово: {ex.Message}");
                    Debug.WriteLine("💡 Возможные причины:");
                    Debug.WriteLine("   1. Npcap сервис не запущен - перезагрузите компьютер");
                    Debug.WriteLine("   2. Устройство используется другой программой");
                    Debug.WriteLine("   3. Недостаточно прав - убедитесь что приложение запущено от администратора");
                    return false;
                }

                // Устанавливаем фильтр только на UDP трафик
                Debug.WriteLine("🔧 Установка фильтра UDP...");
                _device.Filter = "udp";
                Debug.WriteLine("✅ Фильтр установлен");

                _cts = new CancellationTokenSource();
                _isCapturing = true;

                // Запускаем захват в отдельном потоке
                Task.Run(() =>
                {
                    try
                    {
                        Debug.WriteLine("🎯 Начинаем захват пакетов...");
                        _device.Capture();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Ошибка захвата пакетов: {ex.Message}");
                        Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
                    }
                }, _cts.Token);

                Debug.WriteLine("✅ Мониторинг UDP трафика запущен успешно!");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Критическая ошибка запуска мониторинга: {ex.Message}");
                Debug.WriteLine($"   Тип: {ex.GetType().Name}");
                Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
                }
                
                return false;
            }
        }

        private void Device_OnPacketArrival(object sender, PacketCapture e)
        {
            try
            {
                var rawPacket = e.GetPacket();
                var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);

                var ipPacket = packet.Extract<IPPacket>();
                var udpPacket = packet.Extract<UdpPacket>();

                if (ipPacket == null || udpPacket == null)
                    return;

                var sourcePort = udpPacket.SourcePort;
                var destPort = udpPacket.DestinationPort;

                // Проверяем, относится ли пакет к портам DBD
                bool isDbdTraffic = _dbdPorts.Contains(sourcePort) || _dbdPorts.Contains(destPort);

                if (!isDbdTraffic)
                    return;

                // Определяем направление пакета (исходящий или входящий)
                bool isOutgoing = _dbdPorts.Contains(sourcePort);
                bool isIncoming = _dbdPorts.Contains(destPort);

                string remoteIp;
                int remotePort;
                int localPort;

                if (isOutgoing)
                {
                    // Пакет от нас к серверу
                    remoteIp = ipPacket.DestinationAddress.ToString();
                    remotePort = destPort;
                    localPort = sourcePort;
                }
                else
                {
                    // Пакет от сервера к нам
                    remoteIp = ipPacket.SourceAddress.ToString();
                    remotePort = sourcePort;
                    localPort = destPort;
                }

                // Игнорируем локальные адреса
                if (IsLocalAddress(remoteIp))
                    return;

                var key = $"{remoteIp}:{remotePort}:{localPort}";
                var now = DateTime.Now;

                var serverInfo = _gameServers.GetOrAdd(key, k => new GameServerInfo
                {
                    RemoteAddress = remoteIp,
                    RemotePort = remotePort,
                    LocalPort = localPort,
                    LastSeen = now
                });

                serverInfo.LastSeen = now;
                serverInfo.PacketCount++;

                if (isIncoming)
                {
                    serverInfo.BytesReceived += udpPacket.PayloadData?.Length ?? 0;
                }
                else
                {
                    serverInfo.BytesSent += udpPacket.PayloadData?.Length ?? 0;
                }

                // Уведомляем о новом/обновленном сервере
                if (serverInfo.PacketCount == 1)
                {
                    Debug.WriteLine($"🎮 Игровой сервер: {remoteIp}:{remotePort} | Первое обнаружение");
                    GameServerDetected?.Invoke(this, serverInfo);
                }
                else if (serverInfo.PacketCount % 1000 == 0)
                {
                    Debug.WriteLine($"🎮 Игровой сервер: {remoteIp}:{remotePort} | Пакетов: {serverInfo.PacketCount}");
                    GameServerDetected?.Invoke(this, serverInfo);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка обработки пакета: {ex.Message}");
            }
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

        public void StopCapture()
        {
            try
            {
                if (_device != null && _isCapturing)
                {
                    _cts?.Cancel();
                    _device.StopCapture();
                    _device.Close();
                    _isCapturing = false;
                    Debug.WriteLine("⏹️ Мониторинг UDP остановлен");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка остановки мониторинга: {ex.Message}");
            }
        }

        public void Dispose()
        {
            StopCapture();
            _device?.Dispose();
            _cts?.Dispose();
        }
    }
}

