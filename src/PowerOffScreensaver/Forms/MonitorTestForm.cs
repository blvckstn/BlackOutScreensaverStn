using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using PowerOffScreensaver.Localization;
using PowerOffScreensaver.Services;

namespace PowerOffScreensaver;

/// <summary>
/// Per-monitor test wizard. Numbers every monitor (1..N) with an on-screen badge,
/// then steps through them one at a time: a big movable window appears on the
/// monitor with a 5-second "this monitor will go dark" countdown, the monitor is
/// powered off, a 15-second countdown runs while it is dark, it is woken, and the
/// user answers two questions (did it sleep / wake correctly). On "no" the user can
/// change the method for that monitor and retry until it works. Choices are saved
/// per monitor. The method to test with is chosen from the combo at the top.
/// </summary>
public sealed class MonitorTestForm : Form
{
    private const int WarnSeconds = 5;
    private const int DarkSeconds = 15;

    private static readonly PowerOffMode[] ModeOrder =
        { PowerOffMode.Dpms, PowerOffMode.Auto, PowerOffMode.DdcCi, PowerOffMode.Both, PowerOffMode.None };

    private readonly MonitorPowerController _controller;
    private readonly Dictionary<int, PowerOffMode> _perMonitor = new();
    private readonly List<MonitorNumberOverlay> _overlays = new();
    private IReadOnlyList<MonitorInfo> _inventory = Array.Empty<MonitorInfo>();

    private ComboBox _modeCombo = null!;
    private CheckBox _logCheckBox = null!;
    private ListView _list = null!;
    private Label _phaseLabel = null!;
    private Label _countLabel = null!;
    private Button _startButton = null!;
    private Button _closeButton = null!;
    private bool _running;

    public PowerOffMode SelectedMode { get; private set; }
    public IReadOnlyDictionary<int, PowerOffMode>? PerMonitorResult { get; private set; }

    public MonitorTestForm(MonitorPowerController controller, PowerOffMode initialMode,
        IReadOnlyDictionary<int, PowerOffMode>? currentPerMonitor = null)
    {
        _controller = controller;
        SelectedMode = initialMode;
        if (currentPerMonitor != null)
            foreach (var kv in currentPerMonitor) _perMonitor[kv.Key] = kv.Value;
        InitializeUI(initialMode);
    }

    private PowerOffMode CurrentMode()
    {
        var i = _modeCombo.SelectedIndex;
        return (i >= 0 && i < ModeOrder.Length) ? ModeOrder[i] : PowerOffMode.Dpms;
    }

    private void InitializeUI(PowerOffMode initialMode)
    {
        var s = Strings.Get();
        Text = s.TestTitle;
        ClientSize = new Size(620, 452);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        Controls.Add(new Label
        {
            Text = s.TestIntro,
            Left = 20, Top = 12, Width = 580, Height = 40,
            ForeColor = SystemColors.GrayText,
            Font = new Font(Font.FontFamily, 8.5f)
        });

        // ── Method selector (choose what to test with) + log toggle ─
        Controls.Add(new Label
        {
            Text = s.PowerMethodLabel,
            Left = 20, Top = 60, Width = 130, Height = 24,
            TextAlign = ContentAlignment.MiddleLeft
        });
        _modeCombo = new ComboBox
        {
            Left = 150, Top = 57, Width = 290, DropDownStyle = ComboBoxStyle.DropDownList
        };
        _modeCombo.Items.AddRange(new object[] { s.ModeDpms, s.ModeAuto, s.ModeDdcCi, s.ModeBoth, s.ModeNone });
        _modeCombo.SelectedIndex = Math.Max(0, Array.IndexOf(ModeOrder, initialMode));
        _modeCombo.SelectedIndexChanged += (_, _) => SelectedMode = CurrentMode();
        Controls.Add(_modeCombo);

        _logCheckBox = new CheckBox
        {
            Text = s.TestLogLabel, Left = 452, Top = 60, Width = 148, Height = 22,
            AutoSize = false
        };
        Controls.Add(_logCheckBox);

        // ── Monitor list ─────────────────────────────────────────
        _list = new ListView
        {
            Left = 20, Top = 94, Width = 580, Height = 186,
            View = View.Details, FullRowSelect = true, GridLines = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable
        };
        _list.Columns.Add("№", 44);
        _list.Columns.Add(s.ColMonitor, 206);
        _list.Columns.Add(s.ColDdc, 64);
        _list.Columns.Add(s.ColMethod, 130);
        _list.Columns.Add(s.ColResult, 108);
        Controls.Add(_list);

        // ── Compact phase line + big countdown number ────────────
        _phaseLabel = new Label
        {
            Left = 20, Top = 286, Width = 580, Height = 20,
            Font = new Font(Font.FontFamily, 9.5f),
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        Controls.Add(_phaseLabel);

        _countLabel = new Label
        {
            Left = 20, Top = 306, Width = 580, Height = 38,
            Font = new Font(Font.FontFamily, 20f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        Controls.Add(_countLabel);

        Controls.Add(new Label
        {
            Text = s.TestHint,
            Left = 20, Top = 348, Width = 580, Height = 32,
            ForeColor = SystemColors.GrayText,
            Font = new Font(Font.FontFamily, 8.5f)
        });

        _startButton = new Button
        {
            Text = s.TestRunBtn, Left = 20, Top = 396, Width = 240, Height = 34,
            UseVisualStyleBackColor = true
        };
        _startButton.Click += async (_, _) => await RunWizardAsync();
        Controls.Add(_startButton);

        _closeButton = new Button
        {
            Text = s.DiagClose, Left = 508, Top = 396, Width = 92, Height = 34,
            UseVisualStyleBackColor = true
        };
        _closeButton.Click += (_, _) => Close();
        Controls.Add(_closeButton);
        CancelButton = _closeButton;

        Load += (_, _) => Populate();
        FormClosed += (_, _) => ClearOverlays();
    }

    private void Populate()
    {
        var s = Strings.Get();
        _inventory = _controller.Inventory();
        ClearOverlays();
        _list.Items.Clear();

        for (int i = 0; i < _inventory.Count; i++)
        {
            var info = _inventory[i];
            string number = (i + 1).ToString();

            var overlay = new MonitorNumberOverlay(info.Bounds, number);
            overlay.Show();
            _overlays.Add(overlay);

            var mode = _perMonitor.TryGetValue(i, out var m) ? m : CurrentMode();
            var item = new ListViewItem(number);
            item.SubItems.Add(info.Description);
            item.SubItems.Add(info.SupportsDdc ? "✓" : "—");
            item.SubItems.Add(ModeName(mode, s));
            item.SubItems.Add("");
            item.UseItemStyleForSubItems = false;
            _list.Items.Add(item);
        }

        _phaseLabel.Text = "";
        _countLabel.Text = _inventory.Count == 0 ? "—" : "";
    }

    private async Task RunWizardAsync()
    {
        if (_running || _inventory.Count == 0) return;
        _running = true;
        SetBusy(true);
        var s = Strings.Get();

        Log($"===== Test run: mode={CurrentMode()}, monitors={_inventory.Count} =====");
        try
        {
            for (int i = 0; i < _inventory.Count; i++)
            {
                var info = _inventory[i];
                string number = (i + 1).ToString();
                string header = string.Format(s.TestMonitorHeaderFmt, number, _inventory.Count);
                var mode = _perMonitor.TryGetValue(i, out var m0) ? m0 : CurrentMode();
                Log($"Monitor {number}: desc='{info.Description}' bounds={info.Bounds.Width}x{info.Bounds.Height}@{info.Bounds.X},{info.Bounds.Y} ddc={info.SupportsDdc}");

                bool done = false;
                while (!done)
                {
                    HighlightRow(i);
                    Log($"Monitor {number}: testing with mode={mode}");

                    // 5-second warning ON the target monitor + on this dialog.
                    using (var prompt = new MonitorTestPrompt(info.Bounds))
                    {
                        prompt.Show();
                        for (int c = WarnSeconds; c >= 1; c--)
                        {
                            prompt.SetPhase(header, s.TestMonitorCountdown, c.ToString(), Color.FromArgb(255, 170, 60));
                            ShowPhase($"{header} — {s.TestMonitorCountdown}", c.ToString(), Color.FromArgb(180, 90, 0));
                            await Task.Delay(1000);
                        }
                        prompt.Close();
                    }

                    // Power this monitor off (timed: a slow/stuck DDC call can't freeze the test).
                    await TimedRun($"Monitor {number} PowerOffOne({mode})", () => _controller.PowerOffOne(i, mode), 5000);

                    if (_logCheckBox.Checked)
                    {
                        var st = await TimedRun($"Monitor {number} ProbeOne after off",
                            () => _controller.ProbeOne(i)?.State ?? DdcPowerState.Unknown, 4000, DdcPowerState.Unknown);
                        Log($"Monitor {number}: DDC state after off = {st}");
                    }

                    for (int c = DarkSeconds; c >= 1; c--)
                    {
                        ShowPhase(header, string.Format(s.TestWakeInFmt, c), SystemColors.GrayText);
                        await Task.Delay(1000);
                    }

                    // Quick single-pass wake (no long verify loop) so the questions ALWAYS appear.
                    await TimedRun($"Monitor {number} PowerOn (wake)", () => _controller.PowerOn(), 6000);
                    ShowPhase(header, s.TestWokeMsg, Color.FromArgb(0, 140, 60));

                    using var dlg = new MonitorTestResultDialog(header, mode);
                    dlg.ShowDialog(this);
                    Log($"Monitor {number}: answered slept={dlg.SleptOk} woke={dlg.WokeOk} decision={dlg.Decision} nextMode={dlg.SelectedMode}");

                    if (dlg.Decision == TestDecision.Retry)
                    {
                        mode = dlg.SelectedMode; // try again with a different method
                        continue;
                    }

                    mode = dlg.SelectedMode;
                    _perMonitor[i] = mode;
                    SetRow(i, dlg.SleptOk && dlg.WokeOk, mode, s);
                    done = true;
                }
            }

            PerMonitorResult = new Dictionary<int, PowerOffMode>(_perMonitor);
            SelectedMode = MostCommonMode() ?? SelectedMode;
            ShowPhase("", "✓", Color.FromArgb(0, 140, 60));
            Log("===== Test run finished =====");
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex}");
            ShowPhase("", ex.Message, Color.FromArgb(180, 30, 30));
            try { await Task.Run(() => _controller.PowerOn()); } catch { }
        }
        finally
        {
            _running = false;
            SetBusy(false);
        }
    }

    // Runs a possibly-blocking native op on a background thread with a timeout, logging its
    // duration. If it exceeds the timeout the wizard continues (the call keeps running in the
    // background), so a slow/stuck DDC monitor can never freeze the test.
    private Task TimedRun(string name, Action op, int timeoutMs) =>
        TimedRun<object?>(name, () => { op(); return null; }, timeoutMs, null);

    private async Task<T> TimedRun<T>(string name, Func<T> op, int timeoutMs, T onTimeout)
    {
        Log($"{name}: start");
        long t0 = Environment.TickCount64;
        var task = Task.Run(op);
        var finished = await Task.WhenAny(task, Task.Delay(timeoutMs));
        if (finished == task)
        {
            try
            {
                var r = await task;
                Log($"{name}: done in {Environment.TickCount64 - t0} ms -> {r}");
                return r;
            }
            catch (Exception ex)
            {
                Log($"{name}: ERROR after {Environment.TickCount64 - t0} ms -> {ex.Message}");
                return onTimeout;
            }
        }
        Log($"{name}: TIMEOUT after {timeoutMs} ms (continuing; native call still running)");
        return onTimeout;
    }

    private void Log(string line)
    {
        if (_logCheckBox == null || !_logCheckBox.Checked) return;
        try
        {
            System.IO.Directory.CreateDirectory(InstallerService.InstallDir);
            var path = System.IO.Path.Combine(InstallerService.InstallDir, "test.log");
            System.IO.File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss.fff}] {line}{Environment.NewLine}");
        }
        catch { }
    }

    private void ShowPhase(string phase, string big, Color bigColor)
    {
        _phaseLabel.Text = phase;
        _countLabel.Text = big;
        _countLabel.ForeColor = bigColor;
    }

    private PowerOffMode? MostCommonMode()
    {
        if (_perMonitor.Count == 0) return null;
        var counts = new Dictionary<PowerOffMode, int>();
        foreach (var v in _perMonitor.Values)
            counts[v] = counts.TryGetValue(v, out var c) ? c + 1 : 1;
        PowerOffMode best = CurrentMode(); int bestC = -1;
        foreach (var kv in counts)
            if (kv.Value > bestC) { best = kv.Key; bestC = kv.Value; }
        return best;
    }

    private void HighlightRow(int i)
    {
        if (i < 0 || i >= _list.Items.Count) return;
        _list.SelectedItems.Clear();
        _list.Items[i].Selected = true;
        _list.Items[i].EnsureVisible();
    }

    private void SetRow(int i, bool ok, PowerOffMode mode, LangStrings s)
    {
        if (i < 0 || i >= _list.Items.Count) return;
        _list.Items[i].SubItems[3].Text = ModeName(mode, s);
        _list.Items[i].SubItems[4].Text = (ok ? "✓ " : "✗ ") + (ok ? s.TestYes : s.TestNo);
        _list.Items[i].SubItems[4].ForeColor = ok ? Color.FromArgb(0, 140, 60) : Color.FromArgb(180, 30, 30);
    }

    private static string ModeName(PowerOffMode mode, LangStrings s) => mode switch
    {
        PowerOffMode.Dpms => s.ModeDpms,
        PowerOffMode.Auto => s.ModeAuto,
        PowerOffMode.DdcCi => s.ModeDdcCi,
        PowerOffMode.Both => s.ModeBoth,
        PowerOffMode.None => s.ModeNone,
        _ => s.ModeDpms
    };

    private void SetBusy(bool busy)
    {
        _startButton.Enabled = !busy;
        _modeCombo.Enabled = !busy;
        _closeButton.Enabled = !busy;
    }

    private void ClearOverlays()
    {
        foreach (var o in _overlays)
        {
            try { o.Close(); o.Dispose(); } catch { }
        }
        _overlays.Clear();
    }
}
