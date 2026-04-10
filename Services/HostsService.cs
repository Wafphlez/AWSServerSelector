using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using AWSServerSelector.Models;
using AWSServerSelector.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace AWSServerSelector.Services;

public sealed class HostsService : IHostsService
{
    private readonly HostsOptions _hostsOptions;
    private static string HostsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.System),
        "drivers\\etc\\hosts");

    private const string BuiltInDefaultHostsTemplate =
        "# Copyright (c) 1993-2009 Microsoft Corp.\r\n" +
        "#\r\n" +
        "# This is a sample HOSTS file used by Microsoft TCP/IP for Windows.\r\n" +
        "#\r\n" +
        "# This file contains the mappings of IP addresses to host names. Each\r\n" +
        "# entry should be kept on an individual line. The IP address should\r\n" +
        "# be placed in the first column followed by the corresponding host name.\r\n" +
        "# The IP address and the host name should be separated by at least one\r\n" +
        "# space.\r\n" +
        "#\r\n" +
        "# Additionally, comments (such as these) may be inserted on individual\r\n" +
        "# lines or following the machine name denoted by a '#' symbol.\r\n" +
        "#\r\n" +
        "# For example:\r\n" +
        "#\r\n" +
        "#       102.54.94.97     rhino.acme.com          # source server\r\n" +
        "#        38.25.63.10     x.acme.com              # x client host\r\n" +
        "#\r\n" +
        "# localhost name resolution is handled within DNS itself.\r\n" +
        "#       127.0.0.1       localhost\r\n" +
        "#       ::1             localhost\r\n";

    public HostsService(IOptions<HostsOptions>? hostsOptions = null)
    {
        _hostsOptions = hostsOptions?.Value ?? new HostsOptions();
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
        try
        {
            var configuredPath = _hostsOptions.DefaultHostsTemplatePath;
            var fullPath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(AppContext.BaseDirectory, configuredPath);

            if (File.Exists(fullPath))
            {
                return File.ReadAllText(fullPath);
            }

            AppLogger.Error($"Default hosts template was not found at '{fullPath}'. Using built-in fallback.");
            return BuiltInDefaultHostsTemplate;
        }
        catch (Exception ex)
        {
            AppLogger.Error("Default hosts template read failed. Using built-in fallback.", ex);
            return BuiltInDefaultHostsTemplate;
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
