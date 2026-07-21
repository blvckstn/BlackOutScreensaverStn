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
    private readonly MonitorPowerController _powerController;
    private readonly IWorkstationLockService _workstationLockService;
    private readonly ILockStateProbe _lockStateProbe;
    private readonly AppSettings _settings;
    private readonly GlobalInputHook _inputHook = new();
    private readonly InputGate _inputGate = new();
    private readonly EventHandler _processExitHandler;

    public ScreensaverHost()
    {
        var settingsService = new Services.SettingsService();
        _settings = settingsService.Load();

        _powerController = new MonitorPowerController(
            new Services.MonitorPowerService(),
            new Services.DdcCiService());
        _workstationLockService = new Services.WorkstationLockService();
        _lockStateProbe = new Services.DesktopLockProbe();

        // Safety net: if the process ends by any path while monitors are off,
        // bring them back on (a DDC/CI-off panel won't wake from input by itself).
        _processExitHandler = (_, _) => { try { _powerController.PowerOn(); } catch { } };
        AppDomain.CurrentDomain.ProcessExit += _processExitHandler;

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
            _powerController.PowerOff(_settings.PowerOffMode);
        }, null, delay, System.Threading.Timeout.Infinite);
    }

    private void OnExitRequested(object? sender, EventArgs e)
    {
        if (Interlocked.CompareExchange(ref _exiting, 1, 0) != 0)
            return;

        // Stop receiving further input as we tear down.
        _inputHook.Dispose();

        // Always restore the displays first — including any panel we forced off
        // over DDC/CI — so the desktop / lock screen is actually visible.
        _powerController.PowerOn();

        if (_settings.LockOnExit)
        {
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
