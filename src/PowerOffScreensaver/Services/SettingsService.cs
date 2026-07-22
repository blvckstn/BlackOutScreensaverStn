using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

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

            var powerOffMode = ParsePowerOffMode(root, ddcCiEnabled);

            var language = root.TryGetProperty("language", out var langElem)
                ? (langElem.GetString() ?? "en")
                : "en";

            var initialized = root.TryGetProperty("initialized", out var initElem)
                && initElem.GetBoolean();

            return new AppSettings
            {
                LockOnExit = lockOnExit,
                DdcCiEnabled = ddcCiEnabled,
                PowerOffMode = powerOffMode,
                PerMonitorModes = ParsePerMonitorModes(root),
                PowerOffDelayMs = powerOffDelayMs,
                Language = language,
                Initialized = initialized
            };
        }
        catch
        {
            return AppSettings.CreateDefaults();
        }
    }

    /// <summary>
    /// Reads <c>powerOffMode</c> (string or int). Falls back to the legacy
    /// <c>ddcCiEnabled</c> flag (true → Both) when the mode is absent.
    /// </summary>
    internal static PowerOffMode ParsePowerOffMode(JsonElement root, bool legacyDdcCiEnabled)
    {
        if (root.TryGetProperty("powerOffMode", out var modeElem))
        {
            if (modeElem.ValueKind == JsonValueKind.String &&
                Enum.TryParse<PowerOffMode>(modeElem.GetString(), ignoreCase: true, out var m))
                return m;
            if (modeElem.ValueKind == JsonValueKind.Number &&
                Enum.IsDefined(typeof(PowerOffMode), modeElem.GetInt32()))
                return (PowerOffMode)modeElem.GetInt32();
        }
        // Absent → DPMS (wake-safe default). Legacy ddcCiEnabled=true keeps DDC (Both).
        return legacyDdcCiEnabled ? PowerOffMode.Both : PowerOffMode.Dpms;
    }

    /// <summary>Reads the optional per-monitor override map (index → mode).</summary>
    internal static IReadOnlyDictionary<int, PowerOffMode> ParsePerMonitorModes(JsonElement root)
    {
        var dict = new Dictionary<int, PowerOffMode>();
        if (root.TryGetProperty("perMonitorModes", out var el) && el.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in el.EnumerateObject())
            {
                if (!int.TryParse(prop.Name, out var idx)) continue;
                if (prop.Value.ValueKind == JsonValueKind.String &&
                    Enum.TryParse<PowerOffMode>(prop.Value.GetString(), ignoreCase: true, out var m))
                    dict[idx] = m;
                else if (prop.Value.ValueKind == JsonValueKind.Number &&
                         Enum.IsDefined(typeof(PowerOffMode), prop.Value.GetInt32()))
                    dict[idx] = (PowerOffMode)prop.Value.GetInt32();
            }
        }
        return dict;
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
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
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
