using System;

namespace PowerOffScreensaver;

public enum LaunchMode
{
    Screensaver,
    Settings,
    Preview
}

public record ScreensaverArgs(LaunchMode Mode, IntPtr? PreviewHwnd = null)
{
    public static ScreensaverArgs Parse(string[] args)
    {
        if (args == null || args.Length == 0)
        {
            return new ScreensaverArgs(LaunchMode.Settings);
        }

        string firstArg = args[0].ToLowerInvariant();

        if (firstArg == "/s")
        {
            return new ScreensaverArgs(LaunchMode.Screensaver);
        }

        if (firstArg == "/c" || firstArg.StartsWith("/c:"))
        {
            // TODO: parse HWND if provided after colon
            return new ScreensaverArgs(LaunchMode.Settings);
        }

        if (firstArg == "/p" || firstArg.StartsWith("/p"))
        {
            // Extract HWND from next argument or after /p
            IntPtr? hwnd = null;
            if (firstArg == "/p" && args.Length > 1 && int.TryParse(args[1], out int hval))
            {
                hwnd = new IntPtr(hval);
            }
            else if (firstArg.StartsWith("/p") && firstArg.Length > 2 && int.TryParse(firstArg.Substring(2), out int hval2))
            {
                hwnd = new IntPtr(hval2);
            }
            return new ScreensaverArgs(LaunchMode.Preview, hwnd);
        }

        // Unknown or invalid argument: default to Settings
        return new ScreensaverArgs(LaunchMode.Settings);
    }
}
