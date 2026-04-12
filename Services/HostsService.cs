using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using AWSServerSelector.Models;
using AWSServerSelector.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace AWSServerSelector.Services;

public sealed class HostsService : IHostsFileService
{
    private readonly HostsOptions _hostsOptions;
    private static string HostsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.System),
        "drivers\\etc\\hosts");

    public HostsService(IOptions<HostsOptions> hostsOptions)
    {
        _hostsOptions = hostsOptions.Value;
    }

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

    public string ReadDefaultTemplate()
    {
        var configuredPath = _hostsOptions.DefaultHostsTemplatePath;
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new InvalidOperationException("Hosts.DefaultHostsTemplatePath must not be empty.");
        }

        var fullPath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Default hosts template file was not found: {fullPath}", fullPath);
        }

        try
        {
            var content = File.ReadAllText(fullPath);
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException($"Default hosts template file is empty: {fullPath}");
            }

            return content;
        }
        catch (Exception ex)
        {
            AppLogger.Error("Default hosts template read failed.", ex);
            throw;
        }
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
