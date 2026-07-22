using System;
using System.IO;
using System.Text;

namespace PowerOffScreensaver.Services;

/// <summary>
/// Appends a line per wake to <c>%LocalAppData%\Blackout ScreenSaver\wake.log</c>
/// so it can be confirmed that the monitors returned to a working state after
/// sleep (per-monitor before → after DDC/CI state, attempts, elapsed). Best-effort.
/// </summary>
public static class WakeLog
{
    public static void Write(WakeReport report)
    {
        try
        {
            Directory.CreateDirectory(InstallerService.InstallDir);
            var path = Path.Combine(InstallerService.InstallDir, "wake.log");

            var sb = new StringBuilder();
            sb.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ")
              .Append(report.AllAwake ? "ALL-AWAKE" : "NOT-CONFIRMED")
              .Append($" attempts={report.Attempts} escalated={report.Escalated} elapsedMs={report.ElapsedMs}");
            foreach (var m in report.Monitors)
                sb.Append($" | #{m.Index + 1} \"{m.Description}\" ddc={(m.SupportsDdc ? "yes" : "no")} {m.Before}->{m.After}");
            sb.AppendLine();

            File.AppendAllText(path, sb.ToString());
        }
        catch { /* logging is best-effort */ }
    }
}
