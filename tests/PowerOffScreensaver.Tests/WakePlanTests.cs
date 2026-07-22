using System;
using System.Collections.Generic;
using PowerOffScreensaver;
using PowerOffScreensaver.Services;
using Xunit;

namespace PowerOffScreensaver.Tests;

public class WakePlanTests
{
    private static MonitorProbe P(bool supports, DdcPowerState state, int i = 0)
        => new(i, $"Monitor {i}", supports, state);

    [Fact]
    public void AllAwake_NoMonitors_IsTrue()
    {
        Assert.True(WakePlan.AllAwake(Array.Empty<MonitorProbe>()));
        Assert.True(WakePlan.AllAwake(null));
    }

    [Fact]
    public void AllAwake_EveryDdcMonitorOn_IsTrue()
    {
        var probes = new[] { P(true, DdcPowerState.On, 0), P(true, DdcPowerState.On, 1), P(true, DdcPowerState.On, 2) };
        Assert.True(WakePlan.AllAwake(probes));
    }

    [Fact]
    public void AllAwake_OneDdcMonitorStillOff_IsFalse()
    {
        var probes = new[] { P(true, DdcPowerState.On, 0), P(true, DdcPowerState.Off, 1), P(true, DdcPowerState.On, 2) };
        Assert.False(WakePlan.AllAwake(probes));
    }

    [Fact]
    public void AllAwake_OneDdcMonitorUnknown_IsFalse()
    {
        // Unknown/no-reply means we can't confirm it woke → keep trying.
        var probes = new[] { P(true, DdcPowerState.On, 0), P(true, DdcPowerState.Unknown, 1) };
        Assert.False(WakePlan.AllAwake(probes));
    }

    [Fact]
    public void AllAwake_NonDdcMonitorsDoNotBlock()
    {
        // A monitor that doesn't speak DDC/CI can't be verified → it must not hold up the wake.
        var probes = new[] { P(false, DdcPowerState.Unknown, 0), P(true, DdcPowerState.On, 1) };
        Assert.True(WakePlan.AllAwake(probes));
    }

    [Fact]
    public void AnyVerifiable_TrueWhenAnyMonitorSupportsDdc()
    {
        Assert.True(WakePlan.AnyVerifiable(new[] { P(false, DdcPowerState.Unknown, 0), P(true, DdcPowerState.Off, 1) }));
    }

    [Fact]
    public void AnyVerifiable_FalseWhenNoneSupportDdc()
    {
        Assert.False(WakePlan.AnyVerifiable(new[] { P(false, DdcPowerState.Unknown, 0), P(false, DdcPowerState.Unknown, 1) }));
        Assert.False(WakePlan.AnyVerifiable(Array.Empty<MonitorProbe>()));
        Assert.False(WakePlan.AnyVerifiable(null));
    }
}
