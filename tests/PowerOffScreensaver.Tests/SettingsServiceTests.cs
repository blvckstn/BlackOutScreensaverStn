using System;
using System.IO;
using System.Text.Json;
using PowerOffScreensaver;
using PowerOffScreensaver.Services;
using Xunit;

namespace PowerOffScreensaver.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testSettingsFile;

    public SettingsServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "PowerOffScreensaverTests", Guid.NewGuid().ToString());
        _testSettingsFile = Path.Combine(_testDir, "settings.json");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, true);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    private SettingsService CreateServiceWithPath(string path)
    {
        var service = new SettingsService();

        // Use reflection to set the private _settingsPath field for testing
        var field = typeof(SettingsService).GetField("_settingsPath",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(service, path);

        return service;
    }

    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsDefaults()
    {
        var service = CreateServiceWithPath(_testSettingsFile);

        var settings = service.Load();

        Assert.True(settings.LockOnExit);
        Assert.False(settings.DdcCiEnabled);
        Assert.Equal(500, settings.PowerOffDelayMs);
    }

    [Fact]
    public void Save_CreatesDirectoryIfNotExists()
    {
        var service = CreateServiceWithPath(_testSettingsFile);
        var settings = new AppSettings { LockOnExit = false, PowerOffDelayMs = 1000 };

        service.Save(settings);

        Assert.True(Directory.Exists(_testDir));
        Assert.True(File.Exists(_testSettingsFile));
    }

    [Fact]
    public void Save_PersistsSettingsToJson()
    {
        var service = CreateServiceWithPath(_testSettingsFile);
        var settings = new AppSettings
        {
            LockOnExit = false,
            DdcCiEnabled = true,
            PowerOffDelayMs = 2000
        };

        service.Save(settings);

        var json = File.ReadAllText(_testSettingsFile);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("lockOnExit", out var lockElem));
        Assert.False(lockElem.GetBoolean());

        Assert.True(root.TryGetProperty("ddcCiEnabled", out var ddcElem));
        Assert.True(ddcElem.GetBoolean());

        Assert.True(root.TryGetProperty("powerOffDelayMs", out var delayElem));
        Assert.Equal(2000, delayElem.GetInt32());
    }

    [Fact]
    public void Load_ReadsPersistedSettings()
    {
        var service = CreateServiceWithPath(_testSettingsFile);
        var originalSettings = new AppSettings
        {
            LockOnExit = false,
            DdcCiEnabled = true,
            PowerOffDelayMs = 3000
        };
        service.Save(originalSettings);

        var loadedSettings = service.Load();

        Assert.False(loadedSettings.LockOnExit);
        Assert.True(loadedSettings.DdcCiEnabled);
        Assert.Equal(3000, loadedSettings.PowerOffDelayMs);
    }

    [Fact]
    public void Load_HandlesCorruptedJson_ReturnsDefaults()
    {
        var service = CreateServiceWithPath(_testSettingsFile);
        Directory.CreateDirectory(_testDir);
        File.WriteAllText(_testSettingsFile, "{ invalid json content ]]");

        var settings = service.Load();

        Assert.True(settings.LockOnExit);
        Assert.False(settings.DdcCiEnabled);
        Assert.Equal(500, settings.PowerOffDelayMs);
    }

    [Fact]
    public void Load_HandlesEmptyJson_ReturnsDefaults()
    {
        var service = CreateServiceWithPath(_testSettingsFile);
        Directory.CreateDirectory(_testDir);
        File.WriteAllText(_testSettingsFile, "{}");

        var settings = service.Load();

        Assert.True(settings.LockOnExit);
        Assert.False(settings.DdcCiEnabled);
        Assert.Equal(500, settings.PowerOffDelayMs);
    }

    [Fact]
    public void Load_PartialJson_UsesDefaultsForMissingProperties()
    {
        var service = CreateServiceWithPath(_testSettingsFile);
        Directory.CreateDirectory(_testDir);
        File.WriteAllText(_testSettingsFile, "{ \"lockOnExit\": false }");

        var settings = service.Load();

        Assert.False(settings.LockOnExit);
        Assert.False(settings.DdcCiEnabled);
        Assert.Equal(500, settings.PowerOffDelayMs);
    }

    [Fact]
    public void Save_Idempotent_MultipleSavesPreserveSettings()
    {
        var service = CreateServiceWithPath(_testSettingsFile);
        var settings = new AppSettings { PowerOffDelayMs = 1500 };

        service.Save(settings);
        var loaded1 = service.Load();

        service.Save(loaded1);
        var loaded2 = service.Load();

        Assert.Equal(loaded1, loaded2);
        Assert.Equal(1500, loaded2.PowerOffDelayMs);
    }

    [Fact]
    public void RoundTrip_SaveThenLoad_PreservesAllValues()
    {
        var service = CreateServiceWithPath(_testSettingsFile);
        var original = new AppSettings
        {
            LockOnExit = false,
            DdcCiEnabled = true,
            PowerOffDelayMs = 4500
        };

        service.Save(original);
        var loaded = service.Load();

        Assert.Equal(original, loaded);
    }
}
