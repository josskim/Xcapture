using System.IO;
using System.Text.Json;
using System.Windows.Input;
using XCapture.Models;

namespace XCapture.Services;

public static class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "XCapture",
        "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), JsonOptions);
            if (settings is null ||
                !IsValid(settings.RegionCaptureHotKey) ||
                !IsValid(settings.FullScreenCaptureHotKey) ||
                settings.RegionCaptureHotKey == settings.FullScreenCaptureHotKey)
            {
                return new AppSettings();
            }

            return settings;
        }
        catch (Exception exc)
        {
            LogService.Error(exc, "Settings load failed");
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(directory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    public static bool IsValid(HotKeyGesture gesture)
    {
        return gesture.HasModifier &&
               gesture.Key is not Key.None and
               not Key.LeftCtrl and not Key.RightCtrl and
               not Key.LeftShift and not Key.RightShift and
               not Key.LeftAlt and not Key.RightAlt and
               not Key.LWin and not Key.RWin and
               not Key.System;
    }
}
