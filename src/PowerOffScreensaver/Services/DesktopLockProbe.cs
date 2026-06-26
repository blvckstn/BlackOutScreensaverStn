using System;
using System.Runtime.InteropServices;

namespace PowerOffScreensaver.Services;

/// <summary>
/// Probes the lock state via <c>OpenInputDesktop</c>. A non-elevated process can
/// open the "Default" input desktop while the session is unlocked, but once the
/// workstation is locked the input desktop becomes the Winlogon secure desktop,
/// which the process cannot open (ACCESS_DENIED). A NULL handle therefore means
/// "locked". This is independent of any window message pump, so it works even
/// when WTS session notifications are not delivered.
/// </summary>
public sealed class DesktopLockProbe : ILockStateProbe
{
    private const uint DESKTOP_SWITCHDESKTOP = 0x0100;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr OpenInputDesktop(uint dwFlags, bool fInherit, uint dwDesiredAccess);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseDesktop(IntPtr hDesktop);

    public bool IsLocked()
    {
        IntPtr hDesktop = OpenInputDesktop(0, false, DESKTOP_SWITCHDESKTOP);
        if (hDesktop == IntPtr.Zero)
        {
            // Could not open the current input desktop → secure desktop is active → locked.
            return true;
        }

        CloseDesktop(hDesktop);
        return false;
    }
}
