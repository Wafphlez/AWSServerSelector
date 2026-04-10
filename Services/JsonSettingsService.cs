using System;
using System.IO;
using System.Text.Json;
using AWSServerSelector.Models;
using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.Services;

public sealed class JsonSettingsService : ISettingsService
{
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Wafphlez",
        "PingByDaylight",
        "settings.json");

    public UserSettings Load()
    {
        try
        {
            var folder = Path.GetDirectoryName(SettingsFilePath);
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder) || !File.Exists(SettingsFilePath))
            {
                return new UserSettings();
            }

            var json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
        }
        catch (Exception ex)
        {
            AppLogger.Error("Settings load failed", ex);
            return new UserSettings();
        }
    }

    public void Save(UserSettings settings)
    {
        try
        {
            var folder = Path.GetDirectoryName(SettingsFilePath);
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            Directory.CreateDirectory(folder);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            AppLogger.Error("Settings save failed", ex);
        }
    }
}
