using System.Collections.Generic;

namespace PowerOffScreensaver;

/// <summary>
/// Pure decision logic for how to power off the monitors given the chosen mode
/// and per-monitor DDC/CI support. Kept free of Win32 so it is unit-testable.
/// </summary>
public static class PowerPlan
{
    /// <summary>Whether DDC/CI per-monitor power-off should be attempted at all.</summary>
    public static bool UsesDdc(PowerOffMode mode) => mode != PowerOffMode.Dpms;

    /// <summary>
    /// Whether the global DPMS broadcast should also be sent. In Auto it is used
    /// only to cover monitors that don't support DDC/CI (or when detection found
    /// nothing); DdcCi-only never uses it; Dpms and Both always do.
    /// </summary>
    public static bool UseGlobalDpms(PowerOffMode mode, IReadOnlyList<bool>? ddcSupport)
    {
        switch (mode)
        {
            case PowerOffMode.Dpms:
            case PowerOffMode.Both:
                return true;
            case PowerOffMode.DdcCi:
                return false;
            case PowerOffMode.Auto:
            default:
                if (ddcSupport == null || ddcSupport.Count == 0)
                    return true;
                foreach (var supported in ddcSupport)
                    if (!supported) return true;
                return false;
        }
    }
}
