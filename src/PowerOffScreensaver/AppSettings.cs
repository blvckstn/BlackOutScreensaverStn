using System;

namespace PowerOffScreensaver;

public record AppSettings
{
    public bool LockOnExit { get; init; } = true;
    public bool DdcCiEnabled { get; init; } = false;
    public int PowerOffDelayMs { get; init; } = 500;
    public string Language { get; init; } = "en";

    public static AppSettings CreateDefaults()
    {
        return new AppSettings();
    }

    public AppSettings WithLockOnExit(bool value) => this with { LockOnExit = value };
    public AppSettings WithDdcCiEnabled(bool value) => this with { DdcCiEnabled = value };
    public AppSettings WithPowerOffDelayMs(int value)
    {
        if (value < 0 || value > 5000)
            throw new ArgumentOutOfRangeException(nameof(value), "PowerOffDelayMs должна быть между 0 и 5000");
        return this with { PowerOffDelayMs = value };
    }
}
