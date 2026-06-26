using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using PowerOffScreensaver.Services;

namespace PowerOffScreensaver;

public class ScreensaverHost : ApplicationContext
{
    private readonly List<BlackoutForm> _forms = new();
    private int _exiting = 0;
    private readonly IMonitorPowerService _monitorPowerService;
    private readonly IWorkstationLockService _workstationLockService;
    private readonly ILockStateProbe _lockStateProbe;
    private readonly IDdcCiService _ddcCiService;
    private readonly AppSettings _settings;
    private readonly GlobalInputHook _inputHook = new();
    private readonly InputGate _inputGate = new();

    public ScreensaverHost()
    {
        var settingsService = new Services.SettingsService();
        _settings = settingsService.Load();

        _monitorPowerService = new Services.MonitorPowerService();
        _workstationLockService = new Services.WorkstationLockService();
        _lockStateProbe = new Services.DesktopLockProbe();
        _ddcCiService = new Services.NullDdcCiService();

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
            _monitorPowerService.TryPowerOff();
            if (_ddcCiService.IsSupported && _settings.DdcCiEnabled)
            {
                _ddcCiService.TryPowerOff();
            }
        }, null, delay, System.Threading.Timeout.Infinite);
    }

    private void OnExitRequested(object? sender, EventArgs e)
    {
        if (Interlocked.CompareExchange(ref _exiting, 1, 0) != 0)
            return;

        // Stop receiving further input as we tear down.
        _inputHook.Dispose();

        if (_settings.LockOnExit)
        {
            // Layer 2: wake the display so the lock screen is actually visible.
            _monitorPowerService.TryPowerOn();

            // Layers 3-5: lock while the black forms still cover the screen,
            // verify it took effect, retry, then fall back before giving up.
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
        }
        foreach (var form in _forms)
        {
            form?.Dispose();
        }
        base.Dispose(disposing);
    }
}
