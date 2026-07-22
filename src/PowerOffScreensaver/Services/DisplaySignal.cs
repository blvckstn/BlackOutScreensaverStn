using System;
using System.Runtime.InteropServices;

namespace PowerOffScreensaver.Services;

/// <summary>
/// Low-level display-wake primitives. Waking a monitor that was forced off is not
/// a single call: the OS needs a real input event to leave DPMS, the GPU must be
/// told the display is required and re-asserted, and as a last resort the display
/// mode can be re-applied to force stuck panels to re-sync (the software
/// equivalent of unplugging/replugging, similar to what Ctrl+Alt+Del triggers).
/// </summary>
public static class DisplaySignal
{
    [Flags]
    private enum ExecutionState : uint
    {
        Continuous = 0x80000000,
        SystemRequired = 0x00000001,
        DisplayRequired = 0x00000002
    }

    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const ushort VK_F15 = 0x7E; // no-op key used by keep-awake tools

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("kernel32.dll")]
    private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

    [DllImport("user32.dll")]
    private static extern int ChangeDisplaySettingsEx(
        string? lpszDeviceName, IntPtr lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

    /// <summary>Tell Windows the display and system are required (keeps them awake through the lock).</summary>
    public static void KeepDisplayOn()
    {
        try
        {
            SetThreadExecutionState(
                ExecutionState.Continuous | ExecutionState.DisplayRequired | ExecutionState.SystemRequired);
        }
        catch { }
    }

    /// <summary>Release the display-required hint (normal power management resumes).</summary>
    public static void ReleaseKeepOn()
    {
        try { SetThreadExecutionState(ExecutionState.Continuous); } catch { }
    }

    /// <summary>
    /// Inject a genuine, harmless keyboard event (F15 down/up). A real input event
    /// is the OS-sanctioned way to bring a display out of DPMS sleep.
    /// </summary>
    public static void NudgeInput()
    {
        try
        {
            var inputs = new INPUT[2];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].U.ki = new KEYBDINPUT { wVk = VK_F15, dwFlags = 0 };
            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].U.ki = new KEYBDINPUT { wVk = VK_F15, dwFlags = KEYEVENTF_KEYUP };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }
        catch { }
    }

    /// <summary>
    /// Last-resort: re-apply the current display mode for all adapters, forcing the
    /// GPU to re-initialise its outputs. This re-establishes the video signal for a
    /// panel that is stuck showing "no signal" and won't answer DDC/CI.
    /// </summary>
    public static void ForceRedetect()
    {
        try { ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero); }
        catch { }
    }
}
