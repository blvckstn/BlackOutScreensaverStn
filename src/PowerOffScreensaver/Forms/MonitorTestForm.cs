using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using PowerOffScreensaver.Localization;
using PowerOffScreensaver.Services;

namespace PowerOffScreensaver;

/// <summary>
/// Interactive monitor power-off test. Powers every monitor off with the chosen
/// method, waits while the user watches, reads each panel's DDC/CI power state to
/// verify, then powers back on. Lets the user pick the method when auto-detect
/// can't confirm a panel.
/// </summary>
public sealed class MonitorTestForm : Form
{
    private static readonly PowerOffMode[] ModeOrder =
        { PowerOffMode.Auto, PowerOffMode.DdcCi, PowerOffMode.Dpms, PowerOffMode.Both };

    private readonly MonitorPowerController _controller;

    private ComboBox _modeCombo = null!;
    private ListView _list = null!;
    private Label _statusLabel = null!;
    private Button _runButton = null!;
    private Button _closeButton = null!;
    private bool _running;

    public PowerOffMode SelectedMode { get; private set; }

    public MonitorTestForm(MonitorPowerController controller, PowerOffMode initialMode)
    {
        _controller = controller;
        SelectedMode = initialMode;
        InitializeUI(initialMode);
    }

    private void InitializeUI(PowerOffMode initialMode)
    {
        var s = Strings.Get();
        Text = s.TestTitle;
        ClientSize = new Size(560, 424);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        Controls.Add(new Label
        {
            Text = s.TestIntro,
            Left = 20, Top = 14, Width = 520, Height = 34,
            ForeColor = SystemColors.GrayText,
            Font = new Font(Font.FontFamily, 8.5f)
        });

        Controls.Add(new Label
        {
            Text = s.PowerMethodLabel,
            Left = 20, Top = 58, Width = 180, Height = 24,
            TextAlign = ContentAlignment.MiddleLeft
        });
        _modeCombo = new ComboBox
        {
            Left = 200, Top = 55, Width = 240,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _modeCombo.Items.AddRange(new object[] { s.ModeAuto, s.ModeDdcCi, s.ModeDpms, s.ModeBoth });
        _modeCombo.SelectedIndex = Math.Max(0, Array.IndexOf(ModeOrder, initialMode));
        _modeCombo.SelectedIndexChanged += (_, _) =>
        {
            var i = _modeCombo.SelectedIndex;
            if (i >= 0 && i < ModeOrder.Length) SelectedMode = ModeOrder[i];
        };
        Controls.Add(_modeCombo);

        _list = new ListView
        {
            Left = 20, Top = 90, Width = 520, Height = 214,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable
        };
        _list.Columns.Add(s.ColMonitor, 300);
        _list.Columns.Add(s.ColDdc, 80);
        _list.Columns.Add(s.ColResult, 130);
        Controls.Add(_list);

        _statusLabel = new Label
        {
            Left = 20, Top = 312, Width = 520, Height = 20,
            Font = new Font(Font.FontFamily, 9f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        Controls.Add(_statusLabel);

        Controls.Add(new Label
        {
            Text = s.TestHint,
            Left = 20, Top = 336, Width = 520, Height = 40,
            ForeColor = SystemColors.GrayText,
            Font = new Font(Font.FontFamily, 8.5f)
        });

        _runButton = new Button
        {
            Text = s.TestRunBtn,
            Left = 20, Top = 380, Width = 180, Height = 34,
            UseVisualStyleBackColor = true
        };
        _runButton.Click += async (_, _) => await RunTestAsync();
        Controls.Add(_runButton);

        _closeButton = new Button
        {
            Text = s.DiagClose,
            Left = 448, Top = 380, Width = 92, Height = 34,
            UseVisualStyleBackColor = true
        };
        _closeButton.Click += (_, _) => Close();
        Controls.Add(_closeButton);
        CancelButton = _closeButton;

        Load += (_, _) => PopulateMonitors();
    }

    private void PopulateMonitors()
    {
        var s = Strings.Get();
        _list.Items.Clear();
        var probes = _controller.Probe();
        foreach (var p in probes)
        {
            var item = new ListViewItem(p.Description);
            item.SubItems.Add(p.SupportsPower ? "✓" : "—");
            item.SubItems.Add("");
            _list.Items.Add(item);
        }
        _statusLabel.Text = probes.Count == 0
            ? s.StatusNotInstalled // reuse: nothing to show; unlikely
            : $"{probes.Count} × {s.ColMonitor}";
    }

    private async Task RunTestAsync()
    {
        if (_running) return;
        _running = true;
        _runButton.Enabled = false;
        _modeCombo.Enabled = false;
        _closeButton.Enabled = false;
        var s = Strings.Get();
        var mode = SelectedMode;

        try
        {
            PopulateMonitors();

            await Task.Run(() => _controller.PowerOff(mode));

            for (int i = 3; i >= 1; i--)
            {
                _statusLabel.Text = $"⏻  {i}…";
                _statusLabel.ForeColor = Color.FromArgb(180, 90, 0);
                await Task.Delay(1000);
            }

            // Read the off-state (did they actually go dark?), then wake and verify.
            var afterOff = await Task.Run(() => _controller.Probe());
            var wake = await Task.Run(() => _controller.WakeVerified());

            FillResults(afterOff, s);

            int awake = wake.Monitors.Count(m => !m.SupportsDdc || m.After == DdcPowerState.On);
            _statusLabel.Text = $"{(wake.AllAwake ? "✓" : "⚠")}  {awake}/{wake.Monitors.Count}";
            _statusLabel.ForeColor = wake.AllAwake
                ? Color.FromArgb(0, 140, 60)
                : Color.FromArgb(180, 90, 0);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
            _statusLabel.ForeColor = Color.FromArgb(180, 30, 30);
            try { await Task.Run(() => _controller.PowerOn()); } catch { }
        }
        finally
        {
            _running = false;
            _runButton.Enabled = true;
            _modeCombo.Enabled = true;
            _closeButton.Enabled = true;
        }
    }

    private void FillResults(IReadOnlyList<MonitorProbe> after, LangStrings s)
    {
        for (int i = 0; i < _list.Items.Count; i++)
        {
            string text; Color color;
            if (i < after.Count && after[i].State == DdcPowerState.Off)
            {
                text = s.ResultOff; color = Color.FromArgb(0, 140, 60);
            }
            else if (i < after.Count && after[i].State == DdcPowerState.On)
            {
                text = s.ResultOn; color = Color.FromArgb(180, 30, 30);
            }
            else
            {
                text = s.ResultUnknown; color = SystemColors.GrayText;
            }
            _list.Items[i].SubItems[2].Text = text;
            _list.Items[i].UseItemStyleForSubItems = false;
            _list.Items[i].SubItems[2].ForeColor = color;
        }
    }
}
