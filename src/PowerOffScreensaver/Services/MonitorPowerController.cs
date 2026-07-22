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
    /// Bring every monitor back to a working state before locking. For DPMS mode a
    /// real input event plus SC_MONITORPOWER On reliably wakes the panels (the OS
    /// handles it), so no DDC/CI verification is needed. For DDC/CI modes we also
    /// send DDC/CI On and verify each panel reports On via readback, retrying and
    /// finally re-applying the display mode. Returns a report for logging.
    /// </summary>
    public WakeReport Wake(PowerOffMode mode, int maxAttempts = 6, int perAttemptDelayMs = 300, bool allowRedetect = true)
    {
        long start = Environment.TickCount64;
        DisplaySignal.KeepDisplayOn();
        var before = SafeProbe();

        if (!PowerPlan.UsesDdc(mode))
        {
            // DPMS-only: input + SC_MONITORPOWER On, twice, is enough — the display
            // manager restores every monitor and never leaves one stranded.
            for (int i = 0; i < 2; i++)
            {
                DisplaySignal.NudgeInput();
                _dpms.TryPowerOn();
                Thread.Sleep(perAttemptDelayMs / 2);
            }
            DisplaySignal.KeepDisplayOn();
            var afterDpms = SafeProbe();
            return new WakeReport(Correlate(before, afterDpms), 1, true, false,
                Environment.TickCount64 - start);
        }

        // DDC/CI modes: wake and verify each panel reports On.
        var after = before;
        int attempt = 0;
        bool escalated = false;

        for (attempt = 1; attempt <= maxAttempts; attempt++)
        {
            DisplaySignal.NudgeInput();     // real input → leave DPMS sleep
            _dpms.TryPowerOn();             // SC_MONITORPOWER ON → re-assert signal
            try { _ddc.PowerAll(true); } catch { }  // DDC/CI On → per-monitor

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

        DisplaySignal.KeepDisplayOn();
        return new WakeReport(
            Correlate(before, after),
            Math.Min(attempt, maxAttempts),
            WakePlan.AllAwake(after),
            escalated,
            Environment.TickCount64 - start);
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
