using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PowerOffScreensaver.Services;

/// <summary>Per-monitor wake outcome (before/after DDC/CI power state).</summary>
public sealed record MonitorWakeState(
    int Index, string Description, bool SupportsDdc, DdcPowerState Before, DdcPowerState After);

/// <summary>Result of a verified wake, suitable for logging.</summary>
public sealed record WakeReport(
    IReadOnlyList<MonitorWakeState> Monitors, int Attempts, bool AllAwake, bool Escalated, long ElapsedMs);

/// <summary>
/// Orchestrates monitor power-off/on across both mechanisms (per-monitor DDC/CI
/// and global DPMS) according to the selected <see cref="PowerOffMode"/>.
/// Waking is the hard part: a panel forced off over DDC/CI will not return from a
/// mouse move on its own, so wake re-asserts the signal, injects real input, sends
/// DDC/CI On, and then verifies each panel actually reports On, retrying and
/// finally re-detecting the display before giving up.
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

    /// <summary>
    /// Bring every monitor back to a working state and confirm it. Runs a real
    /// input event + DPMS On + DDC/CI On, then polls DDC/CI readback until all
    /// verifiable panels report On (or attempts run out). On the last attempt it
    /// escalates to a display-mode re-apply to recover panels stuck at "no signal".
    /// Returns a report describing what happened, for logging.
    /// </summary>
    public WakeReport WakeVerified(int maxAttempts = 6, int perAttemptDelayMs = 300, bool allowRedetect = true)
    {
        long start = Environment.TickCount64;
        DisplaySignal.KeepDisplayOn();

        var before = SafeProbe();
        var after = before;
        int attempt = 0;
        bool escalated = false;

        for (attempt = 1; attempt <= maxAttempts; attempt++)
        {
            DisplaySignal.NudgeInput();     // real input → leave DPMS sleep
            _dpms.TryPowerOn();             // SC_MONITORPOWER ON → re-assert signal
            try { _ddc.PowerAll(true); } catch { }  // DDC/CI On → per-monitor backlight

            Thread.Sleep(perAttemptDelayMs);
            after = SafeProbe();

            if (WakePlan.AllAwake(after))
                break;

            if (attempt == maxAttempts && allowRedetect && !escalated && WakePlan.AnyVerifiable(after))
            {
                escalated = true;
                DisplaySignal.ForceRedetect();
                Thread.Sleep(perAttemptDelayMs);
                _dpms.TryPowerOn();
                try { _ddc.PowerAll(true); } catch { }
                Thread.Sleep(perAttemptDelayMs);
                after = SafeProbe();
                if (WakePlan.AllAwake(after))
                    break;
            }
        }

        // Keep the display asserted through the lock transition.
        DisplaySignal.KeepDisplayOn();

        long elapsed = Environment.TickCount64 - start;
        return new WakeReport(
            Correlate(before, after),
            Math.Min(attempt, maxAttempts),
            WakePlan.AllAwake(after),
            escalated,
            elapsed);
    }

    /// <summary>Single best-effort restore for exit/dispose paths that must not block.</summary>
    public void PowerOn()
    {
        DisplaySignal.NudgeInput();
        _dpms.TryPowerOn();
        try { _ddc.PowerAll(true); } catch { }
    }

    /// <summary>Enumerate physical monitors (for the settings test dialog).</summary>
    public IReadOnlyList<MonitorProbe> Probe() => SafeProbe();

    private static IReadOnlyList<MonitorWakeState> Correlate(
        IReadOnlyList<MonitorProbe> before, IReadOnlyList<MonitorProbe> after)
    {
        var rows = new List<MonitorWakeState>();
        int count = Math.Max(before.Count, after.Count);
        for (int i = 0; i < count; i++)
        {
            var b = i < before.Count ? before[i] : null;
            var a = i < after.Count ? after[i] : null;
            var src = a ?? b;
            rows.Add(new MonitorWakeState(
                i,
                src?.Description ?? $"Monitor {i + 1}",
                src?.SupportsPower ?? false,
                b?.State ?? DdcPowerState.Unknown,
                a?.State ?? DdcPowerState.Unknown));
        }
        return rows;
    }

    private IReadOnlyList<MonitorProbe> SafeProbe()
    {
        try { return _ddc.Probe(); }
        catch { return Array.Empty<MonitorProbe>(); }
    }
}
