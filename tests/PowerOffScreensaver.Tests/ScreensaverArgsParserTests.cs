using System;
using PowerOffScreensaver;
using Xunit;

namespace PowerOffScreensaver.Tests;

public class ScreensaverArgsParserTests
{
    [Fact]
    public void Parse_WithNoArguments_DefaultsToSettings()
    {
        var result = ScreensaverArgs.Parse([]);
        Assert.Equal(LaunchMode.Settings, result.Mode);
        Assert.Null(result.PreviewHwnd);
    }

    [Fact]
    public void Parse_WithEmptyArguments_DefaultsToSettings()
    {
        var result = ScreensaverArgs.Parse([]);
        Assert.Equal(LaunchMode.Settings, result.Mode);
        Assert.Null(result.PreviewHwnd);
    }

    [Fact]
    public void Parse_WithSlashS_LaunchesScreensaver()
    {
        var result = ScreensaverArgs.Parse(["/s"]);
        Assert.Equal(LaunchMode.Screensaver, result.Mode);
        Assert.Null(result.PreviewHwnd);
    }

    [Fact]
    public void Parse_WithSlashS_CaseInsensitive()
    {
        var result = ScreensaverArgs.Parse(["/S"]);
        Assert.Equal(LaunchMode.Screensaver, result.Mode);
    }

    [Fact]
    public void Parse_WithSlashC_ShowsSettings()
    {
        var result = ScreensaverArgs.Parse(["/c"]);
        Assert.Equal(LaunchMode.Settings, result.Mode);
        Assert.Null(result.PreviewHwnd);
    }

    [Fact]
    public void Parse_WithSlashC_CaseInsensitive()
    {
        var result = ScreensaverArgs.Parse(["/C"]);
        Assert.Equal(LaunchMode.Settings, result.Mode);
    }

    [Fact]
    public void Parse_WithSlashCColon_ShowsSettings()
    {
        var result = ScreensaverArgs.Parse(["/c:1234"]);
        Assert.Equal(LaunchMode.Settings, result.Mode);
    }

    [Fact]
    public void Parse_WithSlashP_LaunchesPreview()
    {
        var result = ScreensaverArgs.Parse(["/p", "12345"]);
        Assert.Equal(LaunchMode.Preview, result.Mode);
        Assert.Equal(new IntPtr(12345), result.PreviewHwnd);
    }

    [Fact]
    public void Parse_WithSlashP_CaseInsensitive()
    {
        var result = ScreensaverArgs.Parse(["/P", "99999"]);
        Assert.Equal(LaunchMode.Preview, result.Mode);
        Assert.Equal(new IntPtr(99999), result.PreviewHwnd);
    }

    [Fact]
    public void Parse_WithSlashPAttachedHwnd_LaunchesPreview()
    {
        var result = ScreensaverArgs.Parse(["/p54321"]);
        Assert.Equal(LaunchMode.Preview, result.Mode);
        Assert.Equal(new IntPtr(54321), result.PreviewHwnd);
    }

    [Fact]
    public void Parse_WithSlashP_NoHwnd_LaunchesPreviewWithNoHwnd()
    {
        var result = ScreensaverArgs.Parse(["/p"]);
        Assert.Equal(LaunchMode.Preview, result.Mode);
        Assert.Null(result.PreviewHwnd);
    }

    [Fact]
    public void Parse_WithSlashP_InvalidHwnd_LaunchesPreviewWithNoHwnd()
    {
        var result = ScreensaverArgs.Parse(["/p", "notanumber"]);
        Assert.Equal(LaunchMode.Preview, result.Mode);
        Assert.Null(result.PreviewHwnd);
    }

    [Fact]
    public void Parse_WithUnknownArgument_DefaultsToSettings()
    {
        var result = ScreensaverArgs.Parse(["/x"]);
        Assert.Equal(LaunchMode.Settings, result.Mode);
    }

    [Fact]
    public void Parse_WithMultipleArguments_UsesFirst()
    {
        var result = ScreensaverArgs.Parse(["/s", "/c", "/p"]);
        Assert.Equal(LaunchMode.Screensaver, result.Mode);
    }

    [Fact]
    public void Parse_WithZeroHwnd_AcceptsAsValidIntPtr()
    {
        var result = ScreensaverArgs.Parse(["/p", "0"]);
        Assert.Equal(LaunchMode.Preview, result.Mode);
        Assert.Equal(IntPtr.Zero, result.PreviewHwnd);
    }
}
