using System;
using System.Collections.Generic;
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
    private readonly IDdcCiService _ddcCiService;
    private readonly AppSettings _settings;

    public ScreensaverHost()
    {
        var settingsService = new Services.SettingsService();
        _settings = settingsService.Load();

        _monitorPowerService = new Services.MonitorPowerService();
        _workstationLockService = new Services.WorkstationLockService();
        _ddcCiService = new Services.NullDdcCiService();

        CreateBlackoutForms();
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
        if (Interlocked.CompareExchange(ref _exiting, 1, 0) == 0)
        {
            foreach (var form in _forms)
            {
                form.Close();
            }

            if (_settings.LockOnExit)
            {
                _workstationLockService.TryLock();
            }

            ExitThread();
        }
    }

    protected override void Dispose(bool disposing)
    {
        foreach (var form in _forms)
        {
            form?.Dispose();
        }
        base.Dispose(disposing);
    }
}
