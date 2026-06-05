using System;
using System.IO;
using System.Text.Json;

namespace PowerOffScreensaver.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;

    public SettingsService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PowerOffScreensaver");

        _settingsPath = Path.Combine(appDataPath, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return AppSettings.CreateDefaults();
            }

            var json = File.ReadAllText(_settingsPath);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var lockOnExit = root.TryGetProperty("lockOnExit", out var lockElem)
                ? lockElem.GetBoolean()
                : true;
            var ddcCiEnabled = root.TryGetProperty("ddcCiEnabled", out var ddcElem)
                ? ddcElem.GetBoolean()
                : false;
            var powerOffDelayMs = root.TryGetProperty("powerOffDelayMs", out var delayElem)
                ? delayElem.GetInt32()
                : 500;

            var language = root.TryGetProperty("language", out var langElem)
                ? (langElem.GetString() ?? "en")
                : "en";

            return new AppSettings
            {
                LockOnExit = lockOnExit,
                DdcCiEnabled = ddcCiEnabled,
                PowerOffDelayMs = powerOffDelayMs,
                Language = language
            };
        }
        catch
        {
            return AppSettings.CreateDefaults();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir!);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }
}
