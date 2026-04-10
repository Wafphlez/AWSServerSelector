using System.Threading.Tasks;
using AWSServerSelector.Models;
using AWSServerSelector.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace AWSServerSelector.Services;

public sealed class UpdateService : IUpdateService
{
    private readonly UpdateOptions _updateOptions;
    public string? LastError { get; private set; }

    public UpdateService(IOptions<UpdateOptions> updateOptions)
    {
        _updateOptions = updateOptions.Value;
    }

    public async Task<GitHubReleaseInfo?> GetLatestReleaseAsync(string currentVersion)
    {
        using var checker = new GitHubReleaseChecker(currentVersion, _updateOptions);
        var release = await checker.GetLatestReleaseAsync();
        LastError = checker.LastError;
        return release;
    }

    public bool IsUpdateAvailable(string currentVersion, GitHubReleaseInfo? latestRelease)
    {
        using var checker = new GitHubReleaseChecker(currentVersion, _updateOptions);
        return checker.IsUpdateAvailable(latestRelease);
    }

    public GitHubAsset? GetDownloadAsset(GitHubReleaseInfo release)
    {
        using var checker = new GitHubReleaseChecker("0.0.0", _updateOptions);
        return checker.GetDownloadAsset(release);
    }
}
