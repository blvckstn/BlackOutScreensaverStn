using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PowerOffScreensaver.Services;

public class WorkstationLockService : IWorkstationLockService
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LockWorkStation();

    /// <summary>Primary lock path. Returns the OS BOOL result so the caller can verify/retry.</summary>
    public bool TryLock()
    {
        try
        {
            return LockWorkStation();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Fallback path: launch <c>rundll32.exe user32.dll,LockWorkStation</c>. This
    /// runs the lock through a separate process and an independent code path, so it
    /// can succeed even if the in-process P/Invoke transiently failed.
    /// </summary>
    public bool TryLockFallback()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "rundll32.exe",
                Arguments = "user32.dll,LockWorkStation",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            return proc != null;
        }
        catch
        {
            return false;
        }
    }
}
