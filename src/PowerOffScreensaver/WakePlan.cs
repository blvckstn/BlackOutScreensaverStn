using System.Collections.Generic;
using PowerOffScreensaver.Services;

namespace PowerOffScreensaver;

/// <summary>
/// Pure logic for deciding whether the monitors have returned to a working state
/// after a wake attempt, based on DDC/CI power-state readback. Kept free of Win32
/// so the verification policy is unit-testable.
/// </summary>
public static class WakePlan
{
    /// <summary>
    /// True when every monitor we can verify reports On. Monitors that don't speak
    /// DDC/CI can't be verified, so they don't block (we can't read them, and DPMS
    /// wake handles them). If nothing can be verified, there is nothing to wait for.
    /// </summary>
    public static bool AllAwake(IReadOnlyList<MonitorProbe>? probes)
    {
        if (probes == null || probes.Count == 0) return true;
        foreach (var p in probes)
            if (p.SupportsPower && p.State != DdcPowerState.On)
                return false;
        return true;
    }

    /// <summary>Whether at least one monitor exposes a verifiable DDC/CI power state.</summary>
    public static bool AnyVerifiable(IReadOnlyList<MonitorProbe>? probes)
    {
        if (probes == null) return false;
        foreach (var p in probes)
            if (p.SupportsPower) return true;
        return false;
    }
}
