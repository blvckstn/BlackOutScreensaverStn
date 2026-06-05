using System;
using System.Runtime.InteropServices;

namespace PowerOffScreensaver.Services;

public class WorkstationLockService : IWorkstationLockService
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool LockWorkStation();

    public void TryLock()
    {
        try
        {
            LockWorkStation();
        }
        catch
        {
            // Swallow exceptions - screensaver still exits cleanly
        }
    }
}
