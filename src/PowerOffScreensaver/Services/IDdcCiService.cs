using System.Collections.Generic;

namespace PowerOffScreensaver.Services;

/// <summary>Power state reported by a monitor over DDC/CI (VCP 0xD6).</summary>
public enum DdcPowerState
{
    Unknown,
    On,
    Off,
    Other
}

/// <summary>One physical monitor as seen over DDC/CI.</summary>
public sealed record MonitorProbe(int Index, string Description, bool SupportsPower, DdcPowerState State);

/// <summary>Outcome of a bulk power command.</summary>
public sealed record DdcResult(int Total, int Succeeded)
{
    public bool AnySucceeded => Succeeded > 0;
}

/// <summary>Per-monitor hardware power control over the DDC/CI channel.</summary>
public interface IDdcCiService
{
    /// <summary>Enumerate physical monitors with DDC/CI support and current power state.</summary>
    IReadOnlyList<MonitorProbe> Probe();

    /// <summary>Probe a single physical monitor by its <see cref="Probe"/> index.</summary>
    MonitorProbe? ProbeOne(int index);

    /// <summary>Set power on (true) or off (false) on every physical monitor.</summary>
    DdcResult PowerAll(bool on);

    /// <summary>Set power on a single physical monitor, addressed by its <see cref="Probe"/> index.</summary>
    DdcResult PowerOne(int index, bool on);
}
