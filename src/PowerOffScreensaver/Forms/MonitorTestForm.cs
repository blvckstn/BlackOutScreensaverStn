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
/// Per-monitor power-off test. Steps through each monitor one at a time: a 5-second
/// "this monitor will go dark now" countdown, then powers that monitor off, waits so
/// the user can watch, powers everything back on, and asks the user to confirm this
/// specific monitor darkened and came back. Lets the user find which method each
/// monitor responds to.
/// </summary>
public sealed class MonitorTestForm : Form
{
    private static readonly PowerOffMode[] ModeOrder =
        { PowerOffMode.Dpms, PowerOffMode.Auto, PowerOffMode.DdcCi, PowerOffMode.Both, PowerOffMode.None };

    private const int CountdownSeconds = 5;
    private const int DarkDwellMs = 4000;

    private readonly MonitorPowerController _controller;

    private ComboBox _modeCombo = null!;
    private Label _headerLabel = null!;
    private Label _countdownLabel = null!;
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
        ClientSize = new Size(560, 470);
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
            Left = 20, Top = 58, Width = 230, Height = 24,
            TextAlign = ContentAlignment.MiddleLeft
        });
        _modeCombo = new ComboBox
        {
            Left = 256, Top = 55, Width = 264,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _modeCombo.Items.AddRange(new object[] { s.ModeDpms, s.ModeAuto, s.ModeDdcCi, s.ModeBoth, s.ModeNone });
        _modeCombo.SelectedIndex = Math.Max(0, Array.IndexOf(ModeOrder, initialMode));
        _modeCombo.SelectedIndexChanged += (_, _) =>
        {
            var i = _modeCombo.SelectedIndex;
            if (i >= 0 && i < ModeOrder.Length) SelectedMode = ModeOrder[i];
        };
        Controls.Add(_modeCombo);

        _headerLabel = new Label
        {
            Left = 20, Top = 92, Width = 520, Height = 24,
            Font = new Font(Font.FontFamily, 10.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        Controls.Add(_headerLabel);

        _countdownLabel = new Label
        {
            Left = 20, Top = 118, Width = 520, Height = 42,
            Font = new Font(Font.FontFamily, 15f, FontStyle.Bold),
            ForeColor = Color.FromArgb(180, 90, 0),
            TextAlign = ContentAlignment.MiddleLeft
        };
        Controls.Add(_countdownLabel);

        _list = new ListView
        {
            Left = 20, Top = 168, Width = 520, Height = 176,
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
            Left = 20, Top = 352, Width = 520, Height = 20,
            Font = new Font(Font.FontFamily, 9f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        Controls.Add(_statusLabel);

        Controls.Add(new Label
        {
            Text = s.TestHint,
            Left = 20, Top = 376, Width = 520, Height = 36,
            ForeColor = SystemColors.GrayText,
            Font = new Font(Font.FontFamily, 8.5f)
        });

        _runButton = new Button
        {
            Text = s.TestRunBtn,
            Left = 20, Top = 420, Width = 220, Height = 34,
            UseVisualStyleBackColor = true
        };
        _runButton.Click += async (_, _) => await RunTestAsync();
        Controls.Add(_runButton);

        _closeButton = new Button
        {
            Text = s.DiagClose,
            Left = 428, Top = 420, Width = 92, Height = 34,
            UseVisualStyleBackColor = true
        };
        _closeButton.Click += (_, _) => Close();
        Controls.Add(_closeButton);
        CancelButton = _closeButton;

        Load += (_, _) => PopulateMonitors();
    }

    private IReadOnlyList<MonitorProbe> PopulateMonitors()
    {
        _list.Items.Clear();
        var probes = _controller.Probe();
        foreach (var p in probes)
        {
            var item = new ListViewItem(p.Description);
            item.SubItems.Add(p.SupportsPower ? "✓" : "—");
            item.SubItems.Add("");
            item.UseItemStyleForSubItems = false;
            _list.Items.Add(item);
        }
        return probes;
    }

    private async Task RunTestAsync()
    {
        if (_running) return;
        _running = true;
        SetBusy(true);
        var s = Strings.Get();
        var mode = SelectedMode;

        try
        {
            var probes = PopulateMonitors();
            if (probes.Count == 0)
            {
                _statusLabel.Text = "—";
                return;
            }

            // "Black screen only" never powers monitors off — nothing to test.
            if (mode == PowerOffMode.None)
            {
                _headerLabel.Text = s.ModeNone;
                _countdownLabel.Text = "";
                _statusLabel.Text = "—";
                _statusLabel.ForeColor = SystemColors.GrayText;
                return;
            }

            int confirmed = 0;
            for (int i = 0; i < probes.Count; i++)
            {
                HighlightRow(i);
                _headerLabel.Text = $"{string.Format(s.TestMonitorHeaderFmt, i + 1, probes.Count)} — {probes[i].Description}";

                // 5-second warning countdown so the user can watch this monitor.
                for (int c = CountdownSeconds; c >= 1; c--)
                {
                    _countdownLabel.Text = $"{s.TestMonitorCountdown}   {c}";
                    await Task.Delay(1000);
                }

                // Power this monitor off (DDC targets it; DPMS is global), let it stay dark.
                await Task.Run(() => _controller.PowerOffOne(i, mode));
                _countdownLabel.Text = "●";
                await Task.Delay(DarkDwellMs);

                var probe = await Task.Run(() => _controller.ProbeOne(i));

                // Bring everything back safely, then ask about this specific monitor.
                await Task.Run(() => _controller.Wake(mode));
                _countdownLabel.Text = "";

                var q = string.Format(s.TestPerMonitorConfirmFmt, i + 1);
                var ans = MessageBox.Show(this, q, _headerLabel.Text,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                bool ok = ans == DialogResult.Yes;
                if (ok) confirmed++;
                SetRowResult(i, ok, probe, s);
            }

            _headerLabel.Text = "";
            _statusLabel.Text = $"{(confirmed == probes.Count ? "✓" : "⚠")}  {confirmed}/{probes.Count}";
            _statusLabel.ForeColor = confirmed == probes.Count
                ? Color.FromArgb(0, 140, 60)
                : Color.FromArgb(180, 90, 0);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
            _statusLabel.ForeColor = Color.FromArgb(180, 30, 30);
            try { await Task.Run(() => _controller.Wake(SelectedMode)); } catch { }
        }
        finally
        {
            _countdownLabel.Text = "";
            _running = false;
            SetBusy(false);
        }
    }

    private void HighlightRow(int i)
    {
        if (i < 0 || i >= _list.Items.Count) return;
        _list.SelectedItems.Clear();
        _list.Items[i].Selected = true;
        _list.Items[i].EnsureVisible();
    }

    private void SetRowResult(int i, bool userConfirmed, MonitorProbe? probe, LangStrings s)
    {
        if (i < 0 || i >= _list.Items.Count) return;
        string text = userConfirmed ? s.ResultOff : s.ResultOn; // "went dark" vs "stayed on"
        Color color = userConfirmed ? Color.FromArgb(0, 140, 60) : Color.FromArgb(180, 30, 30);
        _list.Items[i].SubItems[2].Text = (userConfirmed ? "✓ " : "✗ ") + text;
        _list.Items[i].SubItems[2].ForeColor = color;
    }

    private void SetBusy(bool busy)
    {
        _runButton.Enabled = !busy;
        _modeCombo.Enabled = !busy;
        _closeButton.Enabled = !busy;
    }
}
