using System;
using System.Collections.Generic;

namespace PowerOffScreensaver;

/// <summary>How the screensaver powers off the monitors.</summary>
public enum PowerOffMode
{
    /// <summary>Auto-detect: DDC/CI per monitor where supported, plus DPMS for the rest.</summary>
    Auto = 0,
    /// <summary>DDC/CI per-monitor only (VESA MCCS VCP 0xD6, Standby).</summary>
    DdcCi = 1,
    /// <summary>Global DPMS broadcast only (SC_MONITORPOWER). Safest wake — always recovers on input.</summary>
    Dpms = 2,
    /// <summary>Both DDC/CI per monitor and the DPMS broadcast.</summary>
    Both = 3,
    /// <summary>Black screen only — cover with black windows, never power the monitors
    /// off. Avoids the DisplayPort hot-unplug churn/sounds some setups get on power-off.</summary>
    None = 4
}

public record AppSettings
{
    public bool LockOnExit { get; init; } = true;
    public bool DdcCiEnabled { get; init; } = false;
    // DPMS by default: it always wakes on input. DDC/CI hard-off can strand some
    // monitors (the DDC bus dies when off), so it is opt-in via the settings/test.
    public PowerOffMode PowerOffMode { get; init; } = PowerOffMode.Dpms;
    // Optional per-monitor override (key = monitor index from the test). When set, it
    // is authoritative for power-off; empty means use the global PowerOffMode.
    public IReadOnlyDictionary<int, PowerOffMode> PerMonitorModes { get; init; }
        = new Dictionary<int, PowerOffMode>();
    public int PowerOffDelayMs { get; init; } = 500;
    public string Language { get; init; } = "en";
    public bool Initialized { get; init; } = false;

    public static AppSettings CreateDefaults()
    {
        return new AppSettings();
    }

    // The default record equality would compare PerMonitorModes by reference (a
    // Dictionary), so two otherwise-equal instances would differ. Compare it by content.
    public virtual bool Equals(AppSettings? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return LockOnExit == other.LockOnExit
            && DdcCiEnabled == other.DdcCiEnabled
            && PowerOffMode == other.PowerOffMode
            && PowerOffDelayMs == other.PowerOffDelayMs
            && Language == other.Language
            && Initialized == other.Initialized
            && DictEquals(PerMonitorModes, other.PerMonitorModes);
    }

    public override int GetHashCode() =>
        HashCode.Combine(LockOnExit, DdcCiEnabled, PowerOffMode, PowerOffDelayMs, Language, Initialized);

    private static bool DictEquals(
        IReadOnlyDictionary<int, PowerOffMode> a, IReadOnlyDictionary<int, PowerOffMode> b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null || a.Count != b.Count) return false;
        foreach (var kv in a)
            if (!b.TryGetValue(kv.Key, out var v) || v != kv.Value) return false;
        return true;
    }

    public AppSettings WithLockOnExit(bool value) => this with { LockOnExit = value };
    public AppSettings WithDdcCiEnabled(bool value) => this with { DdcCiEnabled = value };
    public AppSettings WithPowerOffMode(PowerOffMode value) => this with { PowerOffMode = value };
    public AppSettings WithPowerOffDelayMs(int value)
    {
        if (value < 0 || value > 5000)
            throw new ArgumentOutOfRangeException(nameof(value), "PowerOffDelayMs должна быть между 0 и 5000");
        return this with { PowerOffDelayMs = value };
    }
}
