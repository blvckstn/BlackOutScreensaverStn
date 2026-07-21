using System;

namespace PowerOffScreensaver;

/// <summary>How the screensaver powers off the monitors.</summary>
public enum PowerOffMode
{
    /// <summary>Auto-detect: DDC/CI per monitor where supported, plus DPMS for the rest.</summary>
    Auto = 0,
    /// <summary>DDC/CI per-monitor only (VESA MCCS VCP 0xD6).</summary>
    DdcCi = 1,
    /// <summary>Legacy global DPMS broadcast only (SC_MONITORPOWER).</summary>
    Dpms = 2,
    /// <summary>Both DDC/CI per monitor and the DPMS broadcast.</summary>
    Both = 3
}

public record AppSettings
{
    public bool LockOnExit { get; init; } = true;
    public bool DdcCiEnabled { get; init; } = false;
    public PowerOffMode PowerOffMode { get; init; } = PowerOffMode.Auto;
    public int PowerOffDelayMs { get; init; } = 500;
    public string Language { get; init; } = "en";
    public bool Initialized { get; init; } = false;

    public static AppSettings CreateDefaults()
    {
        return new AppSettings();
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
