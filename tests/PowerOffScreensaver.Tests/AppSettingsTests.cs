using System;
using PowerOffScreensaver;
using Xunit;

namespace PowerOffScreensaver.Tests;

public class AppSettingsTests
{
    [Fact]
    public void CreateDefaults_ReturnsCorrectDefaults()
    {
        var settings = AppSettings.CreateDefaults();

        Assert.True(settings.LockOnExit);
        Assert.False(settings.DdcCiEnabled);
        Assert.Equal(500, settings.PowerOffDelayMs);
    }

    [Fact]
    public void Constructor_WithNoArguments_UsesDefaults()
    {
        var settings = new AppSettings();

        Assert.True(settings.LockOnExit);
        Assert.False(settings.DdcCiEnabled);
        Assert.Equal(500, settings.PowerOffDelayMs);
    }

    [Fact]
    public void WithLockOnExit_ModifiesLockOnExitProperty()
    {
        var original = AppSettings.CreateDefaults();
        var modified = original.WithLockOnExit(false);

        Assert.True(original.LockOnExit);
        Assert.False(modified.LockOnExit);
        Assert.False(modified.DdcCiEnabled);
        Assert.Equal(500, modified.PowerOffDelayMs);
    }

    [Fact]
    public void WithDdcCiEnabled_ModifiesDdcCiEnabledProperty()
    {
        var original = AppSettings.CreateDefaults();
        var modified = original.WithDdcCiEnabled(true);

        Assert.False(original.DdcCiEnabled);
        Assert.True(modified.DdcCiEnabled);
        Assert.True(modified.LockOnExit);
        Assert.Equal(500, modified.PowerOffDelayMs);
    }

    [Fact]
    public void WithPowerOffDelayMs_ModifiesPowerOffDelayMsProperty()
    {
        var original = AppSettings.CreateDefaults();
        var modified = original.WithPowerOffDelayMs(1000);

        Assert.Equal(500, original.PowerOffDelayMs);
        Assert.Equal(1000, modified.PowerOffDelayMs);
        Assert.True(modified.LockOnExit);
        Assert.False(modified.DdcCiEnabled);
    }

    [Fact]
    public void WithPowerOffDelayMs_AcceptsMinimumValue()
    {
        var settings = AppSettings.CreateDefaults();
        var modified = settings.WithPowerOffDelayMs(0);

        Assert.Equal(0, modified.PowerOffDelayMs);
    }

    [Fact]
    public void WithPowerOffDelayMs_AcceptsMaximumValue()
    {
        var settings = AppSettings.CreateDefaults();
        var modified = settings.WithPowerOffDelayMs(5000);

        Assert.Equal(5000, modified.PowerOffDelayMs);
    }

    [Fact]
    public void WithPowerOffDelayMs_ThrowsOnNegativeValue()
    {
        var settings = AppSettings.CreateDefaults();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => settings.WithPowerOffDelayMs(-1)
        );
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void WithPowerOffDelayMs_ThrowsOnValueExceedingMaximum()
    {
        var settings = AppSettings.CreateDefaults();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => settings.WithPowerOffDelayMs(5001)
        );
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Record_ImplementsEquality()
    {
        var settings1 = new AppSettings();
        var settings2 = new AppSettings();

        Assert.Equal(settings1, settings2);
    }

    [Fact]
    public void Record_ImplementsInequality()
    {
        var settings1 = new AppSettings();
        var settings2 = settings1.WithLockOnExit(false);

        Assert.NotEqual(settings1, settings2);
    }

    [Fact]
    public void Record_IsImmutable()
    {
        var settings = AppSettings.CreateDefaults();
        var modified = settings.WithPowerOffDelayMs(1500);

        Assert.NotSame(settings, modified);
        Assert.Equal(500, settings.PowerOffDelayMs);
        Assert.Equal(1500, modified.PowerOffDelayMs);
    }

    [Fact]
    public void ChainedWith_AppliesMultipleModifications()
    {
        var settings = AppSettings.CreateDefaults()
            .WithLockOnExit(false)
            .WithDdcCiEnabled(true)
            .WithPowerOffDelayMs(2000);

        Assert.False(settings.LockOnExit);
        Assert.True(settings.DdcCiEnabled);
        Assert.Equal(2000, settings.PowerOffDelayMs);
    }
}
