using System.Threading.Tasks;

namespace AWSServerSelector.Services.Interfaces;

public interface IUpdateService
{
    Task<GitHubReleaseInfo?> GetLatestReleaseAsync(string currentVersion);
    bool IsUpdateAvailable(string currentVersion, GitHubReleaseInfo? latestRelease);
    GitHubAsset? GetDownloadAsset(GitHubReleaseInfo release);
    string? LastError { get; }
}
