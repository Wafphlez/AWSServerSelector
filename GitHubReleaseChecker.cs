using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace AWSServerSelector
{
    public class GitHubReleaseInfo
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;
        
        [JsonPropertyName("published_at")]
        public DateTime PublishedAt { get; set; }
        
        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;
        
        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; } = new();
    }

    public class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
        
        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    public class GitHubReleaseChecker : IDisposable
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/Wafphlez/AWSServerSelector/releases/latest";
        private readonly HttpClient _httpClient;
        private readonly string _currentVersion;

        public GitHubReleaseChecker(string currentVersion)
        {
            _currentVersion = currentVersion;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AWSServerSelector-UpdateChecker/1.0");
        }

        public async Task<GitHubReleaseInfo?> GetLatestReleaseAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(GitHubApiUrl);
                var release = JsonSerializer.Deserialize<GitHubReleaseInfo>(response);
                
                // Отладочная информация
                if (release != null)
                {
                    System.Diagnostics.Debug.WriteLine($"GitHub Release Info:");
                    System.Diagnostics.Debug.WriteLine($"  TagName: '{release.TagName}'");
                    System.Diagnostics.Debug.WriteLine($"  Name: '{release.Name}'");
                    System.Diagnostics.Debug.WriteLine($"  Assets Count: {release.Assets?.Count ?? 0}");
                }
                
                return release;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GitHub API Error: {ex.Message}");
                MessageBox.Show($"Ошибка при получении информации об обновлениях: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        public bool IsUpdateAvailable(GitHubReleaseInfo? latestRelease)
        {
            if (latestRelease == null || string.IsNullOrEmpty(latestRelease.TagName)) 
                return false;
            
            var latestVersion = latestRelease.TagName.TrimStart('v');
            if (string.IsNullOrEmpty(latestVersion))
                return false;
            
            System.Diagnostics.Debug.WriteLine($"Version Comparison:");
            System.Diagnostics.Debug.WriteLine($"  Current: '{_currentVersion}'");
            System.Diagnostics.Debug.WriteLine($"  Latest: '{latestVersion}'");
            
            var result = CompareVersions(latestVersion, _currentVersion) > 0;
            System.Diagnostics.Debug.WriteLine($"  Update Available: {result}");
            
            return result;
        }

        private int CompareVersions(string version1, string version2)
        {
            if (string.IsNullOrEmpty(version1) || string.IsNullOrEmpty(version2))
                return 0;

            var v1Parts = version1.Split('.');
            var v2Parts = version2.Split('.');

            var maxLength = Math.Max(v1Parts.Length, v2Parts.Length);

            for (int i = 0; i < maxLength; i++)
            {
                var v1Part = i < v1Parts.Length ? ParseVersionPart(v1Parts[i]) : 0;
                var v2Part = i < v2Parts.Length ? ParseVersionPart(v2Parts[i]) : 0;

                if (v1Part > v2Part) return 1;
                if (v1Part < v2Part) return -1;
            }

            return 0;
        }

        private int ParseVersionPart(string part)
        {
            if (string.IsNullOrEmpty(part))
                return 0;

            // Удаляем все нечисловые символы
            var cleanPart = new string(part.Where(char.IsDigit).ToArray());
            
            if (string.IsNullOrEmpty(cleanPart))
                return 0;

            return int.TryParse(cleanPart, out int result) ? result : 0;
        }

        public GitHubAsset? GetDownloadAsset(GitHubReleaseInfo release)
        {
            // Ищем zip файл для скачивания
            return release.Assets.Find(asset => 
                asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
