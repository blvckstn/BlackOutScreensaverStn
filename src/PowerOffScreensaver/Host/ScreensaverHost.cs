using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using PowerOffScreensaver.Services;

namespace PowerOffScreensaver;

public class ScreensaverHost : ApplicationContext
{
    private readonly List<BlackoutForm> _forms = new();
    private int _exiting = 0;
    private readonly MonitorPowerController _powerController;
    private readonly IWorkstationLockService _workstationLockService;
    private readonly ILockStateProbe _lockStateProbe;
    private readonly AppSettings _settings;
    private readonly GlobalInputHook _inputHook = new();
    private readonly InputGate _inputGate = new();
    private readonly EventHandler _processExitHandler;
    private readonly SynchronizationContext? _uiContext;
    private long _lastRebuildTick;

    public ScreensaverHost()
    {
        var settingsService = new Services.SettingsService();
        _settings = settingsService.Load();
        _uiContext = SynchronizationContext.Current;

        _powerController = new MonitorPowerController(
            new Services.MonitorPowerService(),
            new Services.DdcCiService());
        _workstationLockService = new Services.WorkstationLockService();
        _lockStateProbe = new Services.DesktopLockProbe();

        // Safety net: if the process ends by any path while monitors are off,
        // bring them back on (a DDC/CI-off panel won't wake from input by itself).
        _processExitHandler = (_, _) => { try { _powerController.PowerOn(); } catch { } };
        AppDomain.CurrentDomain.ProcessExit += _processExitHandler;

        // Powering monitors off can make DisplayPort panels hot-unplug: Windows then
        // rearranges the desktop and our black windows no longer cover the returning
        // monitors (desktop peeks through on the sides). Re-cover on every change.
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

        CreateBlackoutForms();
        InstallGlobalInputHook();
        SchedulePowerOff();
    }

    private void CreateBlackoutForms()
    {
        foreach (var screen in Screen.AllScreens)
        {
            var form = new BlackoutForm(screen.Bounds);
            form.ExitRequested += OnExitRequested;
            _forms.Add(form);
            form.Show();
        }
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        // SystemEvents fires on its own thread — marshal to the UI thread.
        if (_uiContext != null)
            _uiContext.Post(_ => RebuildBlackoutForms(), null);
        else
            RebuildBlackoutForms();
    }

    // Tear down and recreate the black windows so they cover whatever monitors
    // currently exist. Recreating (rather than matching) keeps it simple and correct;
    // black-over-black causes no visible flicker. We do NOT re-issue power-off here —
    // that would risk a hot-unplug loop; coverage is what matters.
    private void RebuildBlackoutForms()
    {
        if (Volatile.Read(ref _exiting) != 0) return;

        // Coalesce bursts of change events.
        long now = Environment.TickCount64;
        if (now - _lastRebuildTick < 250) return;
        _lastRebuildTick = now;

        foreach (var form in _forms)
        {
            form.ExitRequested -= OnExitRequested;
            try { form.Close(); form.Dispose(); } catch { }
        }
        _forms.Clear();

        if (Volatile.Read(ref _exiting) != 0) return;
        CreateBlackoutForms();
    }

    // Layer 1: catch every input system-wide, independent of window focus.
    private void InstallGlobalInputHook()
    {
        _inputHook.MouseMoved += pt =>
        {
            if (_inputGate.OnMouseMove(pt))
                OnExitRequested(this, EventArgs.Empty);
        };
        _inputHook.KeyOrButtonPressed += () => OnExitRequested(this, EventArgs.Empty);
        _inputHook.Install();
    }

    private void SchedulePowerOff()
    {
        var delay = _settings.PowerOffDelayMs;
        new System.Threading.Timer(_ =>
        {
            _powerController.PowerOff(_settings);
        }, null, delay, System.Threading.Timeout.Infinite);
    }

    private void OnExitRequested(object? sender, EventArgs e)
    {
        if (Interlocked.CompareExchange(ref _exiting, 1, 0) != 0)
            return;

        // Stop reacting to input and display churn as we tear down.
        _inputHook.Dispose();
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;

        // Bring every monitor back to a working state (and, for DDC/CI, confirm it)
        // BEFORE we lock, so we never switch to the secure desktop while a panel sleeps.
        var wake = _powerController.Wake(_settings);
        Services.WakeLog.Write(wake);

        if (_settings.LockOnExit)
        {
            var guard = new LockGuard(
                tryLock: _workstationLockService.TryLock,
                isLocked: _lockStateProbe.IsLocked,
                sleep: Thread.Sleep,
                fallback: _workstationLockService.TryLockFallback);
            guard.Ensure();
        }

        foreach (var form in _forms)
        {
            form.Close();
        }

        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inputHook.Dispose();
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            try { _powerController.PowerOn(); } catch { }
            AppDomain.CurrentDomain.ProcessExit -= _processExitHandler;
        }
        foreach (var form in _forms)
        {
            form?.Dispose();
        }
        base.Dispose(disposing);
    }
}
