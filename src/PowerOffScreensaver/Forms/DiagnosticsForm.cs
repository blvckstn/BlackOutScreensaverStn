using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PowerOffScreensaver.Localization;
using PowerOffScreensaver.Services;

namespace PowerOffScreensaver;

public sealed class DiagnosticsForm : Form
{
    private readonly bool _firstRun;
    private readonly IInstallerService _installer = new InstallerService();

    private Panel _resultsPanel = null!;
    private Label _summaryLabel = null!;
    private Button _installButton = null!;
    private Button? _runButton;
    private Button _closeButton = null!;

    public bool AllPassed { get; private set; }
    public bool ShouldRunScreensaver { get; private set; }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = false)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = false)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    public DiagnosticsForm(bool firstRun = false)
    {
        _firstRun = firstRun;
        InitializeUI();
    }

    private void InitializeUI()
    {
        var s = Strings.Get();
        Text = s.DiagTitle;
        ClientSize = new Size(520, 404);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        Controls.Add(new Label
        {
            Text = "System Compatibility Check",
            Left = 20, Top = 16, Width = 480, Height = 26,
            Font = new Font(Font.FontFamily, 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 28, 28)
        });
        Controls.Add(new Label
        {
            Text = _firstRun
                ? "First-run check — making sure Blackout ScreenSaver can work on this system."
                : "Verifying that Blackout ScreenSaver can work correctly on this system.",
            Left = 20, Top = 46, Width = 480, Height = 17,
            ForeColor = SystemColors.GrayText,
            Font = new Font(Font.FontFamily, 8.5f)
        });

        Controls.Add(MakeSep(70));

        _resultsPanel = new Panel
        {
            Left = 0, Top = 71, Width = 520, Height = 248,
            BackColor = Color.White
        };
        Controls.Add(_resultsPanel);

        Controls.Add(MakeSep(323));

        _summaryLabel = new Label
        {
            Left = 20, Top = 330, Width = 480, Height = 22,
            Font = new Font(Font.FontFamily, 9f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Text = "Checking..."
        };
        Controls.Add(_summaryLabel);

        _installButton = new Button
        {
            Text = s.InstallBtn,
            Left = 20, Top = 360, Width = 200, Height = 34,
            UseVisualStyleBackColor = true
        };
        _installButton.Click += (_, _) => DoInstall();
        Controls.Add(_installButton);

        if (_firstRun)
        {
            _runButton = new Button
            {
                Text = s.DiagRunNow,
                Left = 296, Top = 360, Width = 116, Height = 34,
                Enabled = false,
                UseVisualStyleBackColor = true
            };
            _runButton.Click += (_, _) => { ShouldRunScreensaver = true; Close(); };
            Controls.Add(_runButton);
        }

        _closeButton = new Button
        {
            Left = 416, Top = 360, Width = 84, Height = 34,
            Text = s.DiagClose,
            UseVisualStyleBackColor = true
        };
        _closeButton.Click += (_, _) => Close();
        Controls.Add(_closeButton);
        CancelButton = _closeButton;

        Load += (_, _) => RunChecks();
    }

    private void RunChecks()
    {
        var core = GatherCoreChecks();
        AllPassed = core.TrueForAll(c => c.Passed);

        var rows = new List<CheckResult>(core) { GatherInstallCheck() };
        RenderChecks(rows);

        var s = Strings.Get();
        if (AllPassed)
        {
            _summaryLabel.Text = s.DiagAllPass;
            _summaryLabel.ForeColor = Color.FromArgb(0, 140, 60);
            if (_runButton != null) _runButton.Enabled = true;
        }
        else
        {
            _summaryLabel.Text = s.DiagSomeFail;
            _summaryLabel.ForeColor = Color.FromArgb(180, 30, 30);
        }
    }

    private void DoInstall()
    {
        var s = Strings.Get();
        _installButton.Enabled = false;
        Application.DoEvents();

        var result = _installer.Install();

        // Refresh the rendered rows so the Installation line reflects the new state.
        var rows = new List<CheckResult>(GatherCoreChecks()) { GatherInstallCheck() };
        RenderChecks(rows);

        if (result.Succeeded)
        {
            var msg = string.Format(s.InstallDoneFmt, result.CurrentVersion);
            if (result.RemovedOldCount > 0)
                msg += " " + string.Format(s.InstallRemovedFmt, result.RemovedOldCount);
            _summaryLabel.Text = msg;
            _summaryLabel.ForeColor = Color.FromArgb(0, 140, 60);
        }
        else
        {
            _summaryLabel.Text = string.Format(s.InstallFailedFmt, result.Error ?? "");
            _summaryLabel.ForeColor = Color.FromArgb(180, 30, 30);
        }
        _installButton.Enabled = true;
    }

    private static List<CheckResult> GatherCoreChecks()
    {
        var list = new List<CheckResult>();

        var screens = Screen.AllScreens;
        list.Add(new CheckResult(
            "Monitors detected",
            screens.Length > 0,
            screens.Length == 1 ? "1 monitor" : $"{screens.Length} monitors — multi-monitor ready"
        ));

        bool mp = IsApiAvailable("user32.dll", "SendMessageW");
        list.Add(new CheckResult(
            "Monitor power command",
            mp,
            mp ? "Win32 SendMessageW — available" : "API missing in user32.dll"
        ));

        bool lw = IsApiAvailable("user32.dll", "LockWorkStation");
        list.Add(new CheckResult(
            "Workstation lock",
            lw,
            lw ? "Win32 LockWorkStation — available" : "API missing in user32.dll"
        ));

        bool writable = false;
        try
        {
            var dir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PowerOffScreensaver");
            System.IO.Directory.CreateDirectory(dir);
            var tmp = System.IO.Path.Combine(dir, ".writetest");
            System.IO.File.WriteAllText(tmp, "ok");
            System.IO.File.Delete(tmp);
            writable = true;
        }
        catch { }
        list.Add(new CheckResult(
            "Settings storage",
            writable,
            writable ? "%AppData%\\PowerOffScreensaver — writable" : "Cannot write to %AppData%"
        ));

        return list;
    }

    private CheckResult GatherInstallCheck()
    {
        var s = Strings.Get();
        var st = _installer.GetStatus();
        bool ok = st.Installed && st.IsCurrentVersion && st.IsActiveScreensaver;
        string detail = st.Installed
            ? string.Format(s.StatusInstalledFmt, st.InstalledVersion ?? st.CurrentVersion)
            : s.StatusNotInstalled;
        return new CheckResult(s.InstallCheckName, ok, detail);
    }

    private static bool IsApiAvailable(string dll, string func)
    {
        try
        {
            var mod = GetModuleHandle(dll);
            return mod != IntPtr.Zero && GetProcAddress(mod, func) != IntPtr.Zero;
        }
        catch { return false; }
    }

    private void RenderChecks(List<CheckResult> checks)
    {
        _resultsPanel.Controls.Clear();
        int y = 16;
        var passColor = Color.FromArgb(0, 140, 60);
        var failColor = Color.FromArgb(180, 30, 30);

        foreach (var check in checks)
        {
            var fg = check.Passed ? passColor : failColor;

            _resultsPanel.Controls.Add(new Label
            {
                Text = check.Passed ? "✓" : "✗",
                Left = 20, Top = y, Width = 28, Height = 28,
                ForeColor = fg,
                Font = new Font(Font.FontFamily, 11f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            });
            _resultsPanel.Controls.Add(new Label
            {
                Text = check.Name,
                Left = 56, Top = y, Width = 206, Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(Font.FontFamily, 9f)
            });
            _resultsPanel.Controls.Add(new Label
            {
                Text = check.Detail,
                Left = 272, Top = y, Width = 228, Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = check.Passed ? SystemColors.GrayText : fg,
                Font = new Font(Font.FontFamily, 8.5f)
            });

            y += 44;
        }
    }

    private static Label MakeSep(int top) =>
        new() { Left = 0, Top = top, Width = 520, Height = 1, BorderStyle = BorderStyle.Fixed3D };

    private sealed record CheckResult(string Name, bool Passed, string Detail);
}
