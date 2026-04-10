using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.Services;

public sealed class HostsService : IHostsService
{
    private static string HostsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.System),
        "drivers\\etc\\hosts");

    public string Read()
    {
        try
        {
            return File.Exists(HostsPath) ? File.ReadAllText(HostsPath) : string.Empty;
        }
        catch (Exception ex)
        {
            AppLogger.Error("Hosts read failed", ex);
            return string.Empty;
        }
    }

    public void Write(string content)
    {
        File.WriteAllText(HostsPath, content);
    }

    public void Backup()
    {
        try
        {
            File.Copy(HostsPath, HostsPath + ".bak", true);
        }
        catch (Exception ex)
        {
            AppLogger.Error("Hosts backup failed", ex);
        }
    }

    public void FlushDns()
    {
        try
        {
            var psi = new ProcessStartInfo("ipconfig", "/flushdns")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            AppLogger.Error("Flush DNS failed", ex);
        }
    }

    public bool IsHostBlocked(string host)
    {
        var content = Read();
        var pattern = $"^0\\.0\\.0\\.0\\s+{Regex.Escape(host)}\\s*$";
        return Regex.IsMatch(content, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
    }
}
