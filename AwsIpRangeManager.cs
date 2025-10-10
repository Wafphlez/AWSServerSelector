using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AWSServerSelector
{
    public class AwsIpRangeManager
    {
        private static readonly string AwsIpRangesUrl = "https://ip-ranges.amazonaws.com/ip-ranges.json";
        private static readonly string CacheFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Wafphlez",
            "PingByDaylight",
            "aws-ip-ranges.json");

        private static AwsIpRangeManager? _instance;
        private static readonly object _lock = new object();
        
        private List<AwsIpRange> _ipRanges = new();
        private DateTime _lastUpdate = DateTime.MinValue;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromHours(24); // Кэш на 24 часа

        public static AwsIpRangeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new AwsIpRangeManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private AwsIpRangeManager()
        {
            _ = LoadIpRangesAsync();
        }

        public async Task<bool> IsAwsIpAsync(string ip)
        {
            await EnsureIpRangesLoadedAsync();
            
            if (!IPAddress.TryParse(ip, out var address))
                return false;

            foreach (var range in _ipRanges)
            {
                if (IsIpInRange(address, range.IpPrefix))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<string> GetAwsRegionAsync(string ip)
        {
            await EnsureIpRangesLoadedAsync();
            
            if (!IPAddress.TryParse(ip, out var address))
                return "Неизвестный";

            foreach (var range in _ipRanges)
            {
                if (IsIpInRange(address, range.IpPrefix))
                {
                    return range.Region;
                }
            }

            return "Неизвестный";
        }

        public async Task<string> GetAwsServiceAsync(string ip)
        {
            await EnsureIpRangesLoadedAsync();
            
            if (!IPAddress.TryParse(ip, out var address))
                return "Неизвестный";

            foreach (var range in _ipRanges)
            {
                if (IsIpInRange(address, range.IpPrefix))
                {
                    return range.Service;
                }
            }

            return "Неизвестный";
        }

        private async Task EnsureIpRangesLoadedAsync()
        {
            if (_ipRanges.Count == 0 || DateTime.Now - _lastUpdate > _cacheExpiry)
            {
                await LoadIpRangesAsync();
            }
        }

        private async Task LoadIpRangesAsync()
        {
            try
            {
                // Сначала пытаемся загрузить из кэша
                if (File.Exists(CacheFilePath))
                {
                    var cacheInfo = new FileInfo(CacheFilePath);
                    if (DateTime.Now - cacheInfo.LastWriteTime < _cacheExpiry)
                    {
                        var cachedData = await File.ReadAllTextAsync(CacheFilePath);
                        var cachedResponse = JsonSerializer.Deserialize<AwsIpRangesResponse>(cachedData);
                        if (cachedResponse?.Prefixes != null)
                        {
                            _ipRanges = cachedResponse.Prefixes;
                            _lastUpdate = cacheInfo.LastWriteTime;
                            return;
                        }
                    }
                }

                // Загружаем с сервера AWS
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                
                var json = await httpClient.GetStringAsync(AwsIpRangesUrl);
                var response = JsonSerializer.Deserialize<AwsIpRangesResponse>(json);
                
                if (response?.Prefixes != null)
                {
                    _ipRanges = response.Prefixes;
                    _lastUpdate = DateTime.Now;

                    // Сохраняем в кэш
                    var cacheDir = Path.GetDirectoryName(CacheFilePath);
                    if (!Directory.Exists(cacheDir))
                    {
                        Directory.CreateDirectory(cacheDir!);
                    }
                    
                    await File.WriteAllTextAsync(CacheFilePath, json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке AWS IP диапазонов: {ex.Message}");
                
                // Если не удалось загрузить, используем статический список как fallback
                _ipRanges = GetFallbackIpRanges();
            }
        }

        private List<AwsIpRange> GetFallbackIpRanges()
        {
            // Fallback список основных AWS диапазонов
            return new List<AwsIpRange>
            {
                new AwsIpRange { IpPrefix = "52.0.0.0/8", Region = "us-east-1", Service = "EC2" },
                new AwsIpRange { IpPrefix = "54.0.0.0/8", Region = "eu-west-1", Service = "EC2" },
                new AwsIpRange { IpPrefix = "18.0.0.0/8", Region = "us-east-1", Service = "EC2" },
                new AwsIpRange { IpPrefix = "3.0.0.0/8", Region = "us-east-1", Service = "EC2" },
                new AwsIpRange { IpPrefix = "13.0.0.0/8", Region = "us-east-1", Service = "EC2" },
                new AwsIpRange { IpPrefix = "15.0.0.0/8", Region = "us-east-1", Service = "EC2" },
                new AwsIpRange { IpPrefix = "35.0.0.0/8", Region = "us-east-1", Service = "EC2" },
                new AwsIpRange { IpPrefix = "44.0.0.0/8", Region = "us-east-1", Service = "EC2" }
            };
        }

        private bool IsIpInRange(IPAddress ip, string cidr)
        {
            try
            {
                var parts = cidr.Split('/');
                if (parts.Length != 2) return false;

                var networkIp = IPAddress.Parse(parts[0]);
                var prefixLength = int.Parse(parts[1]);

                var ipBytes = ip.GetAddressBytes();
                var networkBytes = networkIp.GetAddressBytes();

                if (ipBytes.Length != networkBytes.Length) return false;

                var bytesToCheck = prefixLength / 8;
                var bitsToCheck = prefixLength % 8;

                // Проверяем полные байты
                for (int i = 0; i < bytesToCheck; i++)
                {
                    if (ipBytes[i] != networkBytes[i]) return false;
                }

                // Проверяем частичный байт
                if (bitsToCheck > 0)
                {
                    var mask = (byte)(0xFF << (8 - bitsToCheck));
                    if ((ipBytes[bytesToCheck] & mask) != (networkBytes[bytesToCheck] & mask))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class AwsIpRangesResponse
    {
        public string SyncToken { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
        public List<AwsIpRange> Prefixes { get; set; } = new();
    }

    public class AwsIpRange
    {
        public string IpPrefix { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string NetworkBorderGroup { get; set; } = string.Empty;
    }
}
