using System;
using System.Windows.Forms;
using PowerOffScreensaver.Services;

namespace PowerOffScreensaver;

public class SettingsForm : Form
{
    private readonly ISettingsService _settingsService;
    private AppSettings _settings;
    private CheckBox _lockCheckBox = null!;
    private CheckBox _ddcCiCheckBox = null!;
    private NumericUpDown _delaySpinner = null!;

    public SettingsForm()
    {
        _settingsService = new Services.SettingsService();
        _settings = _settingsService.Load();

        InitializeUI();
        LoadSettings();
    }

    private static string AppVersion()
    {
        var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        return ver != null ? $"v{ver.Major}.{ver.Minor}" : "v1.0";
    }

    private void InitializeUI()
    {
        Text = $"PowerOffScreensaver {AppVersion()} — Параметры";
        ClientSize = new System.Drawing.Size(440, 210);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _lockCheckBox = new CheckBox
        {
            Text = "Автоматическая блокировка рабочей станции при выходе",
            Left = 20, Top = 24, Width = 400, Height = 24,
            AutoSize = false
        };
        Controls.Add(_lockCheckBox);

        _ddcCiCheckBox = new CheckBox
        {
            Text = "Попытаться использовать DDC/CI для выключения мониторов",
            Left = 20, Top = 56, Width = 400, Height = 24,
            AutoSize = false
        };
        Controls.Add(_ddcCiCheckBox);

        var delayLabel = new Label
        {
            Text = "Задержка перед отключением мониторов (мс):",
            Left = 20, Top = 96, Width = 270, Height = 24,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        };
        _delaySpinner = new NumericUpDown
        {
            Left = 300, Top = 95, Width = 120,
            Minimum = 0, Maximum = 5000, Value = 500
        };
        Controls.Add(delayLabel);
        Controls.Add(_delaySpinner);

        var sep = new Label
        {
            Left = 0, Top = 136, Width = 440, Height = 1,
            BorderStyle = BorderStyle.Fixed3D
        };
        Controls.Add(sep);

        var testButton = new Button
        {
            Text = "Тестировать", Left = 20, Top = 152,
            Width = 120, Height = 32,
            UseVisualStyleBackColor = true
        };
        testButton.Click += (s, e) => LaunchScreensaver();
        Controls.Add(testButton);

        var okButton = new Button
        {
            Text = "ОК", Left = 210, Top = 152,
            Width = 96, Height = 32,
            DialogResult = DialogResult.OK,
            UseVisualStyleBackColor = true
        };
        okButton.Click += (s, e) => SaveSettings();
        Controls.Add(okButton);

        var cancelButton = new Button
        {
            Text = "Отмена", Left = 320, Top = 152,
            Width = 96, Height = 32,
            UseVisualStyleBackColor = true
        };
        cancelButton.Click += (s, e) => Close();
        Controls.Add(cancelButton);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private void LoadSettings()
    {
        _lockCheckBox.Checked = _settings.LockOnExit;
        _ddcCiCheckBox.Checked = _settings.DdcCiEnabled;
        _delaySpinner.Value = _settings.PowerOffDelayMs;
    }

    private void SaveSettings()
    {
        _settings = new AppSettings
        {
            LockOnExit = _lockCheckBox.Checked,
            DdcCiEnabled = _ddcCiCheckBox.Checked,
            PowerOffDelayMs = (int)_delaySpinner.Value
        };

        _settingsService.Save(_settings);
        Close();
    }

    private void LaunchScreensaver()
    {
        // Application.Run() cannot be nested — spawn a new process instead
        var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        if (exe != null)
            System.Diagnostics.Process.Start(exe, "/s");
    }
}
