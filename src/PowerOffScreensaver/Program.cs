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
            case LaunchMode.Screensaver:
                var svc = new Services.SettingsService();
                var settings = svc.Load();
                Strings.Set(settings.Language);

                if (!settings.Initialized)
                {
                    using var diagForm = new DiagnosticsForm(firstRun: true);
                    Application.Run(diagForm);

                    if (diagForm.ShouldRunScreensaver)
                    {
                        svc.Save(settings with { Initialized = true });
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
}
