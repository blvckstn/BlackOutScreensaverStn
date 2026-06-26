using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace PowerOffScreensaver.Services;

/// <summary>
/// Layer 1 of the lock spec: system-wide low-level mouse and keyboard hooks.
/// Unlike per-window WinForms events, these receive every input regardless of
/// which window has focus or sits under the cursor — including the input that
/// merely wakes a powered-off display. The hook proc runs on the installing
/// thread (the UI thread, which pumps messages under Application.Run), so the
/// raised events can be handled directly.
/// </summary>
public sealed class GlobalInputHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;
    private const int WM_MOUSEMOVE = 0x0200;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int x; public int y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    // Hold delegate references so the GC does not collect the native callbacks.
    private readonly HookProc _mouseProc;
    private readonly HookProc _keyboardProc;
    private IntPtr _mouseHook = IntPtr.Zero;
    private IntPtr _keyboardHook = IntPtr.Zero;
    private bool _disposed;

    /// <summary>Raised on every mouse move, with the screen-space cursor position.</summary>
    public event Action<Point>? MouseMoved;

    /// <summary>Raised on any key, mouse button or wheel event.</summary>
    public event Action? KeyOrButtonPressed;

    public GlobalInputHook()
    {
        _mouseProc = MouseHookCallback;
        _keyboardProc = KeyboardHookCallback;
    }

    public void Install()
    {
        IntPtr hMod;
        using (var module = Process.GetCurrentProcess().MainModule)
        {
            hMod = GetModuleHandle(module?.ModuleName);
        }

        _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, hMod, 0);
        _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, hMod, 0);
    }

    /// <summary>True if at least one hook is currently installed.</summary>
    public bool IsInstalled => _mouseHook != IntPtr.Zero || _keyboardHook != IntPtr.Zero;

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = (int)wParam;
            if (msg == WM_MOUSEMOVE)
            {
                var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                MouseMoved?.Invoke(new Point(data.pt.x, data.pt.y));
            }
            else
            {
                // Any button down/up or wheel.
                KeyOrButtonPressed?.Invoke();
            }
        }
        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = (int)wParam;
            if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
            {
                KeyOrButtonPressed?.Invoke();
            }
        }
        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }
        if (_keyboardHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }
    }
}
