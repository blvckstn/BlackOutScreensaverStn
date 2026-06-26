using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace PowerOffScreensaver.Services;

/// <summary>Result of querying or performing an install.</summary>
public sealed record InstallStatus(
    bool Installed,
    bool IsCurrentVersion,
    bool IsActiveScreensaver,
    string? InstalledPath,
    string? InstalledVersion,
    string CurrentVersion,
    int RemovedOldCount = 0,
    bool Succeeded = true,
    string? Error = null);

public interface IInstallerService
{
    InstallStatus GetStatus();
    InstallStatus Install();
    bool Uninstall();
}

/// <summary>
/// Installs BOSS as the per-user active screensaver without administrator rights.
/// The screensaver is deployed under the system-facing name "Blackout ScreenSaver"
/// (the Windows Screen Saver dialog shows the file name), registered via
/// HKCU\Control Panel\Desktop, and activated. Any previous versions/copies are
/// removed first, then the install is verified.
/// </summary>
public sealed class InstallerService : IInstallerService
{
    public const string DisplayName = "Blackout ScreenSaver";
    public const string TargetFileName = "Blackout ScreenSaver.scr";
    private const string DesktopKey = @"Control Panel\Desktop";

    // Legacy install footprints we clean up.
    private static readonly string[] LegacyFolderNames = { "PowerOffScreensaver", "BOSS" };

    private const uint SPI_SETSCREENSAVEACTIVE = 0x0011;
    private const uint SPI_SETSCREENSAVETIMEOUT = 0x000F;
    private const uint SPIF_UPDATEINIFILE = 0x01;
    private const uint SPIF_SENDCHANGE = 0x02;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

    public static string LocalAppData =>
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    public static string InstallDir => Path.Combine(LocalAppData, DisplayName);
    public static string TargetPath => Path.Combine(InstallDir, TargetFileName);

    private static string CurrentExePath =>
        Process.GetCurrentProcess().MainModule?.FileName
        ?? Environment.ProcessPath
        ?? "";

    /// <summary>Version of the running executable, read the same way as the installed file.</summary>
    public static string CurrentVersion()
    {
        var v = SafeFileVersion(CurrentExePath);
        if (!string.IsNullOrWhiteSpace(v)) return v!;
        var a = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        return a != null ? a.ToString() : "0.0.0.0";
    }

    public InstallStatus GetStatus()
    {
        string current = CurrentVersion();
        string? path = File.Exists(TargetPath) ? TargetPath : null;
        string? installedVersion = path != null ? SafeFileVersion(path) : null;
        bool installed = path != null;
        bool isCurrent = installed && VersionsMatch(installedVersion, current);
        bool active = IsActiveScreensaver(path);
        return new InstallStatus(installed, isCurrent, active, path, installedVersion, current);
    }

    public InstallStatus Install()
    {
        string current = CurrentVersion();
        try
        {
            int removed = RemoveOldVersions();
            Directory.CreateDirectory(InstallDir);
            File.Copy(CurrentExePath, TargetPath, overwrite: true);
            ConfigureRegistry(TargetPath);
            ApplyScreenSaverSettings();

            var status = GetStatus() with { RemovedOldCount = removed };
            if (!status.Installed || !status.IsCurrentVersion || !status.IsActiveScreensaver)
            {
                return status with
                {
                    Succeeded = false,
                    Error = "Verification failed after install."
                };
            }
            return status;
        }
        catch (Exception ex)
        {
            return new InstallStatus(false, false, false, null, null, current,
                Succeeded: false, Error: ex.Message);
        }
    }

    public bool Uninstall()
    {
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(DesktopKey, writable: true))
            {
                if (key?.GetValue("SCRNSAVE.EXE") is string val &&
                    PathsEqual(val, TargetPath))
                {
                    key.SetValue("SCRNSAVE.EXE", "", RegistryValueKind.String);
                    key.SetValue("ScreenSaveActive", "0", RegistryValueKind.String);
                }
            }
            if (Directory.Exists(InstallDir))
                Directory.Delete(InstallDir, recursive: true);
            ApplyScreenSaverSettings();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ── old-version cleanup ──────────────────────────────────────────────
    private int RemoveOldVersions()
    {
        int removed = 0;

        // Stale .scr/.exe inside the current install dir (older names/copies).
        if (Directory.Exists(InstallDir))
        {
            foreach (var stale in StaleArtifacts(Directory.GetFiles(InstallDir), TargetFileName))
                removed += TryDelete(stale);
        }

        // Legacy per-user install folders from earlier builds.
        foreach (var name in LegacyFolderNames)
        {
            var dir = Path.Combine(LocalAppData, name);
            if (!string.Equals(dir, InstallDir, StringComparison.OrdinalIgnoreCase) && Directory.Exists(dir))
            {
                try { Directory.Delete(dir, recursive: true); removed++; } catch { }
            }
        }

        // Best-effort: copies dropped into System32 by an old manual install (no admin → usually skipped).
        var sys32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
        foreach (var fn in new[] { "PowerOffScreensaver.scr", TargetFileName })
        {
            var p = Path.Combine(sys32, fn);
            if (File.Exists(p)) removed += TryDelete(p);
        }

        return removed;
    }

    // ── registry / apply ─────────────────────────────────────────────────
    private static void ConfigureRegistry(string scrPath)
    {
        using var key = Registry.CurrentUser.CreateSubKey(DesktopKey, writable: true);
        if (key == null) return;
        key.SetValue("SCRNSAVE.EXE", scrPath, RegistryValueKind.String);
        key.SetValue("ScreenSaveActive", "1", RegistryValueKind.String);
        key.SetValue("ScreenSaverIsSecure", "1", RegistryValueKind.String);

        var timeout = key.GetValue("ScreenSaveTimeOut") as string;
        if (string.IsNullOrEmpty(timeout) || !(int.TryParse(timeout, out var t) && t > 0))
            key.SetValue("ScreenSaveTimeOut", "300", RegistryValueKind.String);
    }

    private static void ApplyScreenSaverSettings()
    {
        // Tell the running session to reload screensaver state immediately.
        SystemParametersInfo(SPI_SETSCREENSAVEACTIVE, 1, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        SystemParametersInfo(SPI_SETSCREENSAVETIMEOUT, 300, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
    }

    private static bool IsActiveScreensaver(string? installedPath)
    {
        if (installedPath == null) return false;
        using var key = Registry.CurrentUser.OpenSubKey(DesktopKey);
        return key?.GetValue("SCRNSAVE.EXE") is string val && PathsEqual(val, installedPath);
    }

    // ── pure helpers (unit-tested) ───────────────────────────────────────
    internal static List<string> StaleArtifacts(IEnumerable<string> files, string keepFileName)
    {
        var result = new List<string>();
        foreach (var f in files)
        {
            var ext = Path.GetExtension(f).ToLowerInvariant();
            if ((ext == ".scr" || ext == ".exe") &&
                !string.Equals(Path.GetFileName(f), keepFileName, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(f);
            }
        }
        return result;
    }

    internal static bool VersionsMatch(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
        if (Version.TryParse(a, out var va) && Version.TryParse(b, out var vb))
            return Normalize(va) == Normalize(vb);
        return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    internal static bool PathsEqual(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
        try
        {
            return string.Equals(
                Path.GetFullPath(a.Trim('"')),
                Path.GetFullPath(b.Trim('"')),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static Version Normalize(Version v) =>
        new(v.Major, Math.Max(v.Minor, 0), Math.Max(v.Build, 0), Math.Max(v.Revision, 0));

    private static int TryDelete(string path)
    {
        try { File.Delete(path); return 1; } catch { return 0; }
    }

    private static string? SafeFileVersion(string path)
    {
        try { return FileVersionInfo.GetVersionInfo(path).FileVersion; }
        catch { return null; }
    }
}
