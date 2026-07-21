using System.Collections.Generic;
using PowerOffScreensaver;
using Xunit;

namespace PowerOffScreensaver.Tests;

public class PowerPlanTests
{
    [Theory]
    [InlineData(PowerOffMode.Auto, true)]
    [InlineData(PowerOffMode.DdcCi, true)]
    [InlineData(PowerOffMode.Both, true)]
    [InlineData(PowerOffMode.Dpms, false)]
    public void UsesDdc_MatchesMode(PowerOffMode mode, bool expected)
    {
        Assert.Equal(expected, PowerPlan.UsesDdc(mode));
    }

    [Fact]
    public void UseGlobalDpms_Dpms_AlwaysTrue()
    {
        Assert.True(PowerPlan.UseGlobalDpms(PowerOffMode.Dpms, new[] { true, true }));
    }

    [Fact]
    public void UseGlobalDpms_Both_AlwaysTrue()
    {
        Assert.True(PowerPlan.UseGlobalDpms(PowerOffMode.Both, new[] { true, true, true }));
    }

    [Fact]
    public void UseGlobalDpms_DdcCi_AlwaysFalse()
    {
        Assert.False(PowerPlan.UseGlobalDpms(PowerOffMode.DdcCi, new[] { false, false }));
    }

    [Fact]
    public void UseGlobalDpms_Auto_AllSupportDdc_False()
    {
        // Every monitor speaks DDC/CI → no need for the global DPMS broadcast.
        Assert.False(PowerPlan.UseGlobalDpms(PowerOffMode.Auto, new[] { true, true, true }));
    }

    [Fact]
    public void UseGlobalDpms_Auto_SomeLackDdc_True()
    {
        // One monitor can't do DDC/CI → cover it with the DPMS broadcast.
        Assert.True(PowerPlan.UseGlobalDpms(PowerOffMode.Auto, new[] { true, false, true }));
    }

    [Fact]
    public void UseGlobalDpms_Auto_NoMonitorsDetected_True()
    {
        Assert.True(PowerPlan.UseGlobalDpms(PowerOffMode.Auto, new List<bool>()));
        Assert.True(PowerPlan.UseGlobalDpms(PowerOffMode.Auto, null));
    }
}
