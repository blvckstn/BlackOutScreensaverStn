using System;
using System.Runtime.InteropServices;

namespace PowerOffScreensaver.Services;

public class MonitorPowerService : IMonitorPowerService
{
    private const int WM_SYSCOMMAND = 0x0112;
    private const int SC_MONITORPOWER = 0xF170;
    private const int POWER_OFF = 2;
    private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);

    [DllImport("user32.dll", SetLastError = false)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

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
}
