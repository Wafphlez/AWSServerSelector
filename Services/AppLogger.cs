using System;
using System.Diagnostics;
using System.IO;

namespace AWSServerSelector.Services;

public static class AppLogger
{
    private static readonly string LogFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Wafphlez",
        "PingByDaylight",
        "logs",
        "app.log");

    public static void Info(string message) => Write("INFO", message);

    public static void Error(string message, Exception? exception = null)
    {
        var text = exception == null ? message : $"{message}. {exception.GetType().Name}: {exception.Message}";
        Write("ERROR", text);
    }

    private static void Write(string level, string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
        Debug.WriteLine(line);

        try
        {
            var dir = Path.GetDirectoryName(LogFilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.AppendAllText(LogFilePath, line + Environment.NewLine);
        }
        catch
        {
            // never throw from logger
        }
    }
}
