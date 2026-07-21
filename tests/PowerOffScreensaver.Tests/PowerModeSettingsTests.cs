using System.Text.Json;
using PowerOffScreensaver;
using PowerOffScreensaver.Services;
using Xunit;

namespace PowerOffScreensaver.Tests;

public class PowerModeSettingsTests
{
    private static JsonElement Json(string body)
        => JsonDocument.Parse(body).RootElement;

    [Theory]
    [InlineData("\"auto\"", PowerOffMode.Auto)]
    [InlineData("\"ddcCi\"", PowerOffMode.DdcCi)]
    [InlineData("\"DdcCi\"", PowerOffMode.DdcCi)]   // case-insensitive
    [InlineData("\"dpms\"", PowerOffMode.Dpms)]
    [InlineData("\"both\"", PowerOffMode.Both)]
    public void ParsePowerOffMode_ReadsStringValues(string modeJson, PowerOffMode expected)
    {
        var root = Json($"{{\"powerOffMode\": {modeJson}}}");
        Assert.Equal(expected, SettingsService.ParsePowerOffMode(root, legacyDdcCiEnabled: false));
    }

    [Theory]
    [InlineData(0, PowerOffMode.Auto)]
    [InlineData(1, PowerOffMode.DdcCi)]
    [InlineData(3, PowerOffMode.Both)]
    public void ParsePowerOffMode_ReadsNumericValues(int num, PowerOffMode expected)
    {
        var root = Json($"{{\"powerOffMode\": {num}}}");
        Assert.Equal(expected, SettingsService.ParsePowerOffMode(root, legacyDdcCiEnabled: false));
    }

    [Fact]
    public void ParsePowerOffMode_Absent_LegacyDdcTrue_MapsToBoth()
    {
        var root = Json("{}");
        Assert.Equal(PowerOffMode.Both, SettingsService.ParsePowerOffMode(root, legacyDdcCiEnabled: true));
    }

    [Fact]
    public void ParsePowerOffMode_Absent_LegacyDdcFalse_DefaultsAuto()
    {
        var root = Json("{}");
        Assert.Equal(PowerOffMode.Auto, SettingsService.ParsePowerOffMode(root, legacyDdcCiEnabled: false));
    }

    [Fact]
    public void ParsePowerOffMode_UnknownString_FallsBackToDefault()
    {
        var root = Json("{\"powerOffMode\": \"nonsense\"}");
        Assert.Equal(PowerOffMode.Auto, SettingsService.ParsePowerOffMode(root, legacyDdcCiEnabled: false));
    }

    [Theory]
    [InlineData("/install", LaunchMode.Install)]
    [InlineData("/INSTALL", LaunchMode.Install)]
    [InlineData("--install", LaunchMode.Install)]
    [InlineData("/s", LaunchMode.Screensaver)]
    [InlineData("/c", LaunchMode.Settings)]
    public void Parse_InstallFlag(string arg, LaunchMode expected)
    {
        Assert.Equal(expected, ScreensaverArgs.Parse(new[] { arg }).Mode);
    }
}
