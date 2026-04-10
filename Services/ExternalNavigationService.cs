using System.Diagnostics;
using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.Services;

public sealed class ExternalNavigationService : IExternalNavigationService
{
    public void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    public void OpenFile(string filePath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true,
            Verb = "open"
        });
    }

    public void OpenFolder(string folderPath)
    {
        Process.Start(new ProcessStartInfo("explorer.exe", folderPath) { UseShellExecute = true });
    }
}
