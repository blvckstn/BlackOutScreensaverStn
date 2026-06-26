using System;
using System.Runtime.InteropServices;

namespace PowerOffScreensaver.Services;

public class MonitorPowerService : IMonitorPowerService
{
    private const int WM_SYSCOMMAND = 0x0112;
    private const int SC_MONITORPOWER = 0xF170;
    private const int POWER_OFF = 2;
    private const int POWER_ON = -1;
    private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);

    [Flags]
    private enum ExecutionState : uint
    {
        Continuous = 0x80000000,
        DisplayRequired = 0x00000002
    }

    [DllImport("user32.dll", SetLastError = false)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

    public void TryPowerOff()
    {
        try
        {
            SendMessage(HWND_BROADCAST, WM_SYSCOMMAND, new IntPtr(SC_MONITORPOWER), new IntPtr(POWER_OFF));
        }
        catch
        {
            // Swallow exceptions - black windows still provide burn-in protection
        }
    }

    public void TryPowerOn()
    {
        try
        {
            // Tell the OS the display is required (wakes it), then explicitly request ON
            // so the secure logon desktop is rendered rather than locking behind a dark panel.
            SetThreadExecutionState(ExecutionState.Continuous | ExecutionState.DisplayRequired);
            SendMessage(HWND_BROADCAST, WM_SYSCOMMAND, new IntPtr(SC_MONITORPOWER), new IntPtr(POWER_ON));
            SetThreadExecutionState(ExecutionState.Continuous);
        }
        catch
        {
            // Swallow exceptions - lock still proceeds even if the wake hint fails
        }
    }
}
