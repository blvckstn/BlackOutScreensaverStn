using System;
using System.Drawing;
using System.Windows.Forms;
using PowerOffScreensaver.Localization;
using PowerOffScreensaver.Services;

namespace PowerOffScreensaver;

public class SettingsForm : Form
{
    private readonly ISettingsService _settingsService;
    private AppSettings _settings;
    private readonly MonitorPowerController _powerController =
        new(new MonitorPowerService(), new DdcCiService());

    private CheckBox _lockCheckBox = null!;
    private Label _methodLabel = null!;
    private ComboBox _modeCombo = null!;
    private Button _testMonitorsButton = null!;
    private NumericUpDown _delaySpinner = null!;
    private Label _delayLabel = null!;
    private Label _versionLabel = null!;
    private Button _testButton = null!;
    private Button _checkButton = null!;
    private Button _okButton = null!;
    private Button _cancelButton = null!;
    private ComboBox _langCombo = null!;

    private readonly ToolTip _toolTip = new() { AutoPopDelay = 15000, InitialDelay = 350, ReshowDelay = 100 };

    private static readonly string[] LangOrder =
        ["en", "ru", "de", "fr", "es", "it", "pt", "pl", "zh"];

    private static readonly PowerOffMode[] ModeOrder =
        { PowerOffMode.Dpms, PowerOffMode.Auto, PowerOffMode.DdcCi, PowerOffMode.Both, PowerOffMode.None };

    public SettingsForm()
    {
        _settingsService = new Services.SettingsService();
        _settings = _settingsService.Load();
        Strings.Set(_settings.Language);
        InitializeUI();
        LoadSettings();
    }

    private static string AppVersion()
    {
        var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        return ver != null ? $"v{ver.Major}.{ver.Minor}" : "v1.8";
    }

    private void InitializeUI()
    {
        ClientSize = new Size(500, 300);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        // ── Language row ────────────────────────────────────────
        var globeLabel = new Label
        {
            Text = "🌐", Left = 20, Top = 18, Width = 26, Height = 24,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font.FontFamily, 11f)
        };
        Controls.Add(globeLabel);

        _langCombo = new ComboBox
        {
            Left = 50, Top = 16, Width = 180,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        foreach (var code in LangOrder)
            if (Strings.All.TryGetValue(code, out var ls))
                _langCombo.Items.Add($"{ls.Flag}  {ls.NativeName}");
        _langCombo.SelectedIndexChanged += OnLangChanged;
        Controls.Add(_langCombo);

        Controls.Add(MakeSep(50));

        // ── Lock checkbox ────────────────────────────────────────
        _lockCheckBox = new CheckBox
            { Left = 20, Top = 58, Width = 460, Height = 22, AutoSize = false };
        Controls.Add(_lockCheckBox);

        // ── Monitor power-off method row ─────────────────────────
        _methodLabel = new Label
        {
            Left = 20, Top = 90, Width = 180, Height = 24,
            TextAlign = ContentAlignment.MiddleLeft
        };
        Controls.Add(_methodLabel);

        _modeCombo = new ComboBox
        {
            Left = 176, Top = 88, Width = 170,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        Controls.Add(_modeCombo);

        _testMonitorsButton = new Button
        {
            Left = 352, Top = 87, Width = 128, Height = 26,
            UseVisualStyleBackColor = true
        };
        _testMonitorsButton.Click += (_, _) => OpenMonitorTest();
        Controls.Add(_testMonitorsButton);

        Controls.Add(MakeSep(122));

        // ── Delay row ────────────────────────────────────────────
        _delayLabel = new Label
        {
            Left = 20, Top = 130, Width = 308, Height = 22,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _delaySpinner = new NumericUpDown
            { Left = 338, Top = 129, Width = 140, Minimum = 0, Maximum = 5000, Value = 500 };
        Controls.Add(_delayLabel);
        Controls.Add(_delaySpinner);

        Controls.Add(MakeSep(165));

        // ── Version label ────────────────────────────────────────
        _versionLabel = new Label
        {
            Left = 20, Top = 173, Width = 460, Height = 16,
            ForeColor = SystemColors.GrayText,
            Font = new Font(Font.FontFamily, 7.5f)
        };
        Controls.Add(_versionLabel);

        // ── Buttons ──────────────────────────────────────────────
        _testButton = new Button
            { Left = 15, Top = 200, Width = 104, Height = 34, UseVisualStyleBackColor = true };
        _testButton.Click += (_, _) => LaunchScreensaver();
        Controls.Add(_testButton);

        _checkButton = new Button
            { Left = 126, Top = 200, Width = 126, Height = 34, UseVisualStyleBackColor = true };
        _checkButton.Click += (_, _) =>
        {
            using var diag = new DiagnosticsForm(firstRun: false);
            diag.ShowDialog(this);
        };
        Controls.Add(_checkButton);

        _okButton = new Button
        {
            Left = 268, Top = 200, Width = 96, Height = 34,
            DialogResult = DialogResult.OK,
            UseVisualStyleBackColor = true
        };
        _okButton.Click += (_, _) => SaveSettings();
        Controls.Add(_okButton);

        _cancelButton = new Button
            { Left = 372, Top = 200, Width = 110, Height = 34, UseVisualStyleBackColor = true };
        _cancelButton.Click += (_, _) => Close();
        Controls.Add(_cancelButton);

        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        ApplyLocalization();
    }

    private static Label MakeSep(int top) =>
        new() { Left = 0, Top = top, Width = 500, Height = 1, BorderStyle = BorderStyle.Fixed3D };

    private void ApplyLocalization()
    {
        var s = Strings.Get();
        Text = string.Format(s.WindowTitle, AppVersion());
        _lockCheckBox.Text = s.LockOnExit;
        _methodLabel.Text = s.PowerMethodLabel;
        _delayLabel.Text = s.DelayMs;
        _versionLabel.Text = $"{s.VersionPrefix} {AppVersion()}";
        _testButton.Text = s.TestBtn;
        _checkButton.Text = s.CheckBtn;
        _testMonitorsButton.Text = s.TestMonitorsBtn;
        _okButton.Text = s.OkBtn;
        _cancelButton.Text = s.CancelBtn;

        // Rebuild the mode combo in the current language, preserving the selection.
        var keepMode = CurrentMode();
        _modeCombo.Items.Clear();
        _modeCombo.Items.AddRange(new object[] { s.ModeDpms, s.ModeAuto, s.ModeDdcCi, s.ModeBoth, s.ModeNone });
        _modeCombo.SelectedIndex = Math.Max(0, Array.IndexOf(ModeOrder, keepMode));

        _toolTip.SetToolTip(_lockCheckBox, s.LockOnExitHint);
        _toolTip.SetToolTip(_methodLabel, s.TestHint);
        _toolTip.SetToolTip(_modeCombo, s.TestHint);
        _toolTip.SetToolTip(_delayLabel, s.DelayHint);
        _toolTip.SetToolTip(_delaySpinner, s.DelayHint);

        var idx = Array.IndexOf(LangOrder, Strings.Current);
        if (idx >= 0 && _langCombo.SelectedIndex != idx)
        {
            _langCombo.SelectedIndexChanged -= OnLangChanged;
            _langCombo.SelectedIndex = idx;
            _langCombo.SelectedIndexChanged += OnLangChanged;
        }
    }

    private PowerOffMode CurrentMode()
    {
        var i = _modeCombo.SelectedIndex;
        return (i >= 0 && i < ModeOrder.Length) ? ModeOrder[i] : PowerOffMode.Auto;
    }

    private void SetModeCombo(PowerOffMode mode) =>
        _modeCombo.SelectedIndex = Math.Max(0, Array.IndexOf(ModeOrder, mode));

    private void LoadSettings()
    {
        _lockCheckBox.Checked = _settings.LockOnExit;
        SetModeCombo(_settings.PowerOffMode);
        _delaySpinner.Value = Math.Clamp(_settings.PowerOffDelayMs, 0, 5000);
    }

    private void OnLangChanged(object? sender, EventArgs e)
    {
        var idx = _langCombo.SelectedIndex;
        if (idx >= 0 && idx < LangOrder.Length)
        {
            Strings.Set(LangOrder[idx]);
            ApplyLocalization();
        }
    }

    private void OpenMonitorTest()
    {
        using var f = new MonitorTestForm(_powerController, CurrentMode());
        f.ShowDialog(this);
        SetModeCombo(f.SelectedMode); // reflect a method change made while testing
    }

    private void SaveSettings()
    {
        var mode = CurrentMode();
        _settings = new AppSettings
        {
            LockOnExit = _lockCheckBox.Checked,
            DdcCiEnabled = mode is PowerOffMode.DdcCi or PowerOffMode.Both, // legacy mirror
            PowerOffMode = mode,
            PowerOffDelayMs = (int)_delaySpinner.Value,
            Language = Strings.Current,
            Initialized = _settings.Initialized
        };
        _settingsService.Save(_settings);
        Close();
    }

    private void LaunchScreensaver()
    {
        var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        if (exe != null)
            System.Diagnostics.Process.Start(exe, "/s");
    }
}
