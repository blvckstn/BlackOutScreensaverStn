using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PowerOffScreensaver.Localization;

namespace PowerOffScreensaver;

public sealed class DiagnosticsForm : Form
{
    private readonly bool _firstRun;

    private Panel _resultsPanel = null!;
    private Label _summaryLabel = null!;
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
        ClientSize = new Size(520, 340);
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
                ? "First-run check — making sure BOSS can work on this system."
                : "Verifying that BOSS can work correctly on this system.",
            Left = 20, Top = 46, Width = 480, Height = 17,
            ForeColor = SystemColors.GrayText,
            Font = new Font(Font.FontFamily, 8.5f)
        });

        Controls.Add(MakeSep(70));

        _resultsPanel = new Panel
        {
            Left = 0, Top = 71, Width = 520, Height = 192,
            BackColor = Color.White
        };
        Controls.Add(_resultsPanel);

        Controls.Add(MakeSep(263));

        _summaryLabel = new Label
        {
            Left = 20, Top = 272, Width = 340, Height = 22,
            Font = new Font(Font.FontFamily, 9f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Checking..."
        };
        Controls.Add(_summaryLabel);

        if (_firstRun)
        {
            _runButton = new Button
            {
                Text = s.DiagRunNow,
                Left = 256, Top = 298, Width = 140, Height = 34,
                Enabled = false,
                UseVisualStyleBackColor = true
            };
            _runButton.Click += (_, _) => { ShouldRunScreensaver = true; Close(); };
            Controls.Add(_runButton);
        }

        _closeButton = new Button
        {
            Left = 404, Top = 298, Width = 96, Height = 34,
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
        var checks = GatherChecks();
        AllPassed = checks.TrueForAll(c => c.Passed);
        RenderChecks(checks);

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

    private static List<CheckResult> GatherChecks()
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
