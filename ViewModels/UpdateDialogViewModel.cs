using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.ViewModels;

public partial class UpdateDialogViewModel : ObservableObject
{
    private readonly IUpdateService _updateService;
    private string _downloadUrl = string.Empty;

    [ObservableProperty]
    private string statusText = "Проверяем наличие обновлений...";

    [ObservableProperty]
    private string updateTitle = string.Empty;

    [ObservableProperty]
    private string updateDescription = string.Empty;

    [ObservableProperty]
    private string updateSize = string.Empty;

    [ObservableProperty]
    private string yourVersionText = string.Empty;

    [ObservableProperty]
    private string latestVersionText = string.Empty;

    public string DownloadUrl => _downloadUrl;

    public UpdateDialogViewModel(IUpdateService updateService)
    {
        _updateService = updateService;
    }

    public async Task CheckForUpdatesAsync(string currentVersion)
    {
        var latestVersionLabel = LocalizationManager.GetString("LatestVersion") ?? "Latest version:";
        var checkingText = LocalizationManager.GetString("CheckingUpdates") ?? "Checking for updates...";
        LatestVersionText = $"{latestVersionLabel} {checkingText}";
        YourVersionText = $"{LocalizationManager.GetString("YourVersion")} {currentVersion}";

        try
        {
            var latestRelease = await _updateService.GetLatestReleaseAsync(currentVersion);
            if (latestRelease == null)
            {
                var errorText = LocalizationManager.GetString("UpdateCheckFailed") ?? "Failed to get update information.";
                LatestVersionText = $"{latestVersionLabel} {errorText}";
                StatusText = string.IsNullOrWhiteSpace(_updateService.LastError)
                    ? errorText
                    : $"{errorText} {_updateService.LastError}";
                return;
            }

            var versionNumber = latestRelease.TagName?.TrimStart('v') ?? "unknown";
            LatestVersionText = $"{latestVersionLabel} {versionNumber}";

            if (_updateService.IsUpdateAvailable(currentVersion, latestRelease))
            {
                var downloadAsset = _updateService.GetDownloadAsset(latestRelease);
                if (downloadAsset != null)
                {
                    SetUpdateInfo(latestRelease, downloadAsset);
                    return;
                }

                StatusText = LocalizationManager.GetString("UpdateDownloadUnavailable");
                return;
            }

            SetNoUpdateInfo();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            var errorText = LocalizationManager.GetString("UpdateCheckError") ?? "Error checking for updates:";
            LatestVersionText = $"{latestVersionLabel} {errorText}";
            StatusText = $"{errorText} {ex.Message}";
        }
    }

    private void SetUpdateInfo(GitHubReleaseInfo release, GitHubAsset downloadAsset)
    {
        var cleanVersion = release.TagName?.TrimStart('v') ?? "unknown";
        UpdateTitle = $"{LocalizationManager.GetString("UpdateAvailable")} {cleanVersion}";
        UpdateDescription = release.Body.Length > 200 ? release.Body[..200] + "..." : release.Body;
        UpdateSize = $"{LocalizationManager.GetString("UpdateSize")} {FormatFileSize(downloadAsset.Size)}";
        _downloadUrl = downloadAsset.BrowserDownloadUrl;
    }

    private void SetNoUpdateInfo()
    {
        StatusText = LocalizationManager.GetString("NoUpdatesAvailable");
        _downloadUrl = string.Empty;
    }

    private static string FormatFileSize(long bytes)
    {
        var sizes = new[] { "B", "KB", "MB", "GB" };
        double len = bytes;
        var order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
