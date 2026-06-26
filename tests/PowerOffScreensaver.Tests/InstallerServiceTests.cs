using System.Collections.Generic;
using System.Linq;
using PowerOffScreensaver.Services;
using Xunit;

namespace PowerOffScreensaver.Tests;

public class InstallerServiceTests
{
    [Fact]
    public void StaleArtifacts_ReturnsOldScrAndExe_KeepsTarget()
    {
        var files = new[]
        {
            @"C:\dir\Blackout ScreenSaver.scr",  // keep
            @"C:\dir\PowerOffScreensaver.scr",   // stale
            @"C:\dir\PowerOffScreensaver.exe",   // stale
            @"C:\dir\settings.json",             // ignored
            @"C:\dir\readme.txt",                // ignored
        };

        var stale = InstallerService.StaleArtifacts(files, "Blackout ScreenSaver.scr");

        Assert.Equal(2, stale.Count);
        Assert.Contains(@"C:\dir\PowerOffScreensaver.scr", stale);
        Assert.Contains(@"C:\dir\PowerOffScreensaver.exe", stale);
        Assert.DoesNotContain(@"C:\dir\Blackout ScreenSaver.scr", stale);
    }

    [Fact]
    public void StaleArtifacts_KeepNameIsCaseInsensitive()
    {
        var files = new[] { @"C:\d\Blackout ScreenSaver.scr" };
        var stale = InstallerService.StaleArtifacts(files, "blackout screensaver.scr");
        Assert.Empty(stale);
    }

    [Fact]
    public void StaleArtifacts_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(InstallerService.StaleArtifacts(new List<string>(), "x.scr"));
    }

    [Theory]
    [InlineData("1.4.0.0", "1.4.0.0", true)]
    [InlineData("1.4", "1.4.0.0", true)]      // normalized
    [InlineData("1.4.0", "1.4.0.0", true)]
    [InlineData("1.3.0.0", "1.4.0.0", false)]
    [InlineData("2.0.0.0", "1.4.0.0", false)]
    public void VersionsMatch_NormalizesComponents(string a, string b, bool expected)
    {
        Assert.Equal(expected, InstallerService.VersionsMatch(a, b));
    }

    [Theory]
    [InlineData(null, "1.4")]
    [InlineData("", "1.4")]
    [InlineData("1.4", null)]
    [InlineData("1.4", "  ")]
    public void VersionsMatch_NullOrEmpty_IsFalse(string? a, string? b)
    {
        Assert.False(InstallerService.VersionsMatch(a, b));
    }

    [Fact]
    public void PathsEqual_SamePathDifferentCase_IsTrue()
    {
        Assert.True(InstallerService.PathsEqual(@"C:\Users\X\App\BOSS.scr", @"c:\users\x\app\boss.scr"));
    }

    [Fact]
    public void PathsEqual_HandlesQuotesAndDotSegments()
    {
        Assert.True(InstallerService.PathsEqual("\"C:\\a\\b.scr\"", @"C:\a\.\b.scr"));
    }

    [Fact]
    public void PathsEqual_DifferentPaths_IsFalse()
    {
        Assert.False(InstallerService.PathsEqual(@"C:\a\b.scr", @"C:\a\c.scr"));
    }

    [Theory]
    [InlineData(null, "x")]
    [InlineData("x", null)]
    [InlineData("", "")]
    public void PathsEqual_NullOrEmpty_IsFalse(string? a, string? b)
    {
        Assert.False(InstallerService.PathsEqual(a, b));
    }

    [Fact]
    public void InstallPaths_UseBlackoutScreenSaverName()
    {
        Assert.Equal("Blackout ScreenSaver.scr", InstallerService.TargetFileName);
        Assert.EndsWith(@"Blackout ScreenSaver\Blackout ScreenSaver.scr", InstallerService.TargetPath);
    }
}
