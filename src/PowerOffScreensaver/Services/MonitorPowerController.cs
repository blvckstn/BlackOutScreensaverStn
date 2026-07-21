using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerOffScreensaver.Services;

/// <summary>
/// Orchestrates monitor power-off/on across both mechanisms (per-monitor DDC/CI
/// and global DPMS) according to the selected <see cref="PowerOffMode"/>.
/// Wake always restores every monitor via both paths, because a panel forced off
/// over DDC/CI will not come back from mouse input on its own.
/// </summary>
public sealed class MonitorPowerController
{
    private readonly IMonitorPowerService _dpms;
    private readonly IDdcCiService _ddc;

    public MonitorPowerController(IMonitorPowerService dpms, IDdcCiService ddc)
    {
        _dpms = dpms;
        _ddc = ddc;
    }

    /// <summary>Power all monitors off using the chosen mode (auto-detecting DDC/CI support).</summary>
    public void PowerOff(PowerOffMode mode)
    {
        IReadOnlyList<bool> support = Array.Empty<bool>();
        if (mode == PowerOffMode.Auto)
            support = SafeProbe().Select(p => p.SupportsPower).ToList();

        if (PowerPlan.UsesDdc(mode))
        {
            try { _ddc.PowerAll(false); } catch { }
        }

        if (PowerPlan.UseGlobalDpms(mode, support))
            _dpms.TryPowerOff();
    }

    /// <summary>Restore every monitor. Safe to call multiple times and from any exit path.</summary>
    public void PowerOn()
    {
        try { _ddc.PowerAll(true); } catch { }
        _dpms.TryPowerOn();
    }

    /// <summary>Enumerate physical monitors (for the settings test dialog).</summary>
    public IReadOnlyList<MonitorProbe> Probe() => SafeProbe();

    private IReadOnlyList<MonitorProbe> SafeProbe()
    {
        try { return _ddc.Probe(); }
        catch { return Array.Empty<MonitorProbe>(); }
    }
}
