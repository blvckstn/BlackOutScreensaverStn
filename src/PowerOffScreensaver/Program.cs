using PowerOffScreensaver.Localization;

namespace PowerOffScreensaver;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        var parsed = ScreensaverArgs.Parse(args);

        switch (parsed.Mode)
        {
            case LaunchMode.Install:
                RunHeadlessInstall();
                break;

            case LaunchMode.Screensaver:
                var svc = new Services.SettingsService();
                var settings = svc.Load();
                Strings.Set(settings.Language);

                if (!settings.Initialized)
                {
                    using var diagForm = new DiagnosticsForm(firstRun: true);
                    Application.Run(diagForm);

                    // Onboarding has been shown — don't prompt again next launch.
                    svc.Save(settings with { Initialized = true });

                    if (diagForm.ShouldRunScreensaver)
                    {
                        var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                        if (exe != null)
                            System.Diagnostics.Process.Start(exe, "/s");
                    }
                }
                else
                {
                    Application.Run(new ScreensaverHost());
                }
                break;

            case LaunchMode.Preview:
                Environment.Exit(0);
                break;

            case LaunchMode.Settings:
            default:
                Application.Run(new SettingsForm());
                break;
        }
    }

    /// <summary>
    /// Headless install path for command-line integration (<c>/install</c>).
    /// Runs the per-user install, writes a log, and sets the process exit code.
    /// </summary>
    private static void RunHeadlessInstall()
    {
        var installer = new Services.InstallerService();
        var result = installer.Install();

        // Installing implies onboarding is done, so the screensaver runs directly
        // (blackout) next time Windows starts it instead of showing the first-run check.
        try
        {
            var svc = new Services.SettingsService();
            svc.Save(svc.Load() with { Initialized = true });
        }
        catch { /* best-effort */ }

        try
        {
            System.IO.Directory.CreateDirectory(Services.InstallerService.InstallDir);
            var log = System.IO.Path.Combine(Services.InstallerService.InstallDir, "install.log");
            var line =
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                (result.Succeeded ? "OK" : "FAIL") +
                $" version={result.CurrentVersion}" +
                $" installedPath={result.InstalledPath}" +
                $" active={result.IsActiveScreensaver}" +
                $" removedOld={result.RemovedOldCount}" +
                (result.Error != null ? $" error={result.Error}" : "") +
                Environment.NewLine;
            System.IO.File.AppendAllText(log, line);
        }
        catch { /* logging is best-effort */ }

        Environment.ExitCode = result.Succeeded ? 0 : 1;
    }
}
