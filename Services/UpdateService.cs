using System.Threading.Tasks;
using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.Services;

public sealed class UpdateService : IUpdateService
{
    public string? LastError { get; private set; }

    public async Task<GitHubReleaseInfo?> GetLatestReleaseAsync(string currentVersion)
    {
        using var checker = new GitHubReleaseChecker(currentVersion);
        var release = await checker.GetLatestReleaseAsync();
        LastError = checker.LastError;
        return release;
    }

    public bool IsUpdateAvailable(string currentVersion, GitHubReleaseInfo? latestRelease)
    {
        using var checker = new GitHubReleaseChecker(currentVersion);
        return checker.IsUpdateAvailable(latestRelease);
    }

    public GitHubAsset? GetDownloadAsset(GitHubReleaseInfo release)
    {
        using var checker = new GitHubReleaseChecker("0.0.0");
        return checker.GetDownloadAsset(release);
    }
}
