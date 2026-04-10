using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AWSServerSelector.Services;

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
        public string? LastError { get; private set; }

        public GitHubReleaseChecker(string currentVersion)
        {
            _currentVersion = currentVersion;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AWSServerSelector-UpdateChecker/1.0");
        }

        public async Task<GitHubReleaseInfo?> GetLatestReleaseAsync()
        {
            try
            {
                LastError = null;
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
                AppLogger.Error("GitHub API request failed", ex);
                LastError = ex.Message;
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
            
            System.Diagnostics.Debug.WriteLine("Version Comparison:");
            System.Diagnostics.Debug.WriteLine($"  Current: '{_currentVersion}'");
            System.Diagnostics.Debug.WriteLine($"  Latest: '{latestVersion}'");
            
            var result = VersionComparer.Compare(latestVersion, _currentVersion) > 0;
            System.Diagnostics.Debug.WriteLine($"  Update Available: {result}");
            
            return result;
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
