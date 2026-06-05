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
                Application.Run(new ScreensaverHost());
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
