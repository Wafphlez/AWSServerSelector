using System.Threading.Tasks;
using System.Windows;
using AWSServerSelector.Services.Interfaces;
using AWSServerSelector.ViewModels;
using Xunit;

namespace AWSServerSelector.UnitTests;

public class UpdateDialogViewModelTests
{
    [Fact]
    public async Task CheckForUpdatesAsync_SetsDownloadUrl_WhenUpdateExists()
    {
        var vm = new UpdateDialogViewModel(new FakeUpdateService(true));

        await vm.CheckForUpdatesAsync("1.0.0");

        Assert.False(string.IsNullOrWhiteSpace(vm.DownloadUrl));
        Assert.Equal(Visibility.Visible, vm.UpdateInfoVisibility);
        Assert.Equal(Visibility.Visible, vm.DownloadButtonVisibility);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_LeavesDownloadUrlEmpty_WhenNoUpdate()
    {
        var vm = new UpdateDialogViewModel(new FakeUpdateService(false));

        await vm.CheckForUpdatesAsync("1.0.0");

        Assert.True(string.IsNullOrWhiteSpace(vm.DownloadUrl));
        Assert.Equal(Visibility.Collapsed, vm.UpdateInfoVisibility);
        Assert.Equal(Visibility.Collapsed, vm.DownloadButtonVisibility);
    }

    private sealed class FakeUpdateService : IUpdateService
    {
        private readonly bool _hasUpdate;

        public FakeUpdateService(bool hasUpdate)
        {
            _hasUpdate = hasUpdate;
        }

        public string? LastError => null;

        public Task<GitHubReleaseInfo?> GetLatestReleaseAsync(string currentVersion)
        {
            return Task.FromResult<GitHubReleaseInfo?>(new GitHubReleaseInfo
            {
                TagName = "v1.0.1",
                Body = "test",
                Assets = [new GitHubAsset { Name = "release.zip", BrowserDownloadUrl = "https://example.com/release.zip", Size = 100 }]
            });
        }

        public bool IsUpdateAvailable(string currentVersion, GitHubReleaseInfo? latestRelease) => _hasUpdate;

        public GitHubAsset? GetDownloadAsset(GitHubReleaseInfo release) => release.Assets[0];
    }
}
