using System;
using System.Drawing;
using System.Windows.Forms;
using PowerOffScreensaver.Localization;

namespace PowerOffScreensaver;

public enum TestDecision { Next, Retry }

/// <summary>
/// Shown after a monitor's off/on test: two explicit questions (did it sleep
/// correctly / wake correctly), a method picker for this monitor, and a choice to
/// retry this monitor with the chosen method or move on to the next.
/// </summary>
public sealed class MonitorTestResultDialog : Form
{
    private static readonly PowerOffMode[] ModeOrder =
        { PowerOffMode.Dpms, PowerOffMode.Auto, PowerOffMode.DdcCi, PowerOffMode.Both, PowerOffMode.None };

    private RadioButton _sleepYes = null!, _sleepNo = null!, _wakeYes = null!, _wakeNo = null!;
    private ComboBox _modeCombo = null!;

    public bool SleptOk => _sleepYes.Checked;
    public bool WokeOk => _wakeYes.Checked;
    public PowerOffMode SelectedMode
    {
        get
        {
            var i = _modeCombo.SelectedIndex;
            return (i >= 0 && i < ModeOrder.Length) ? ModeOrder[i] : PowerOffMode.Dpms;
        }
    }
    public TestDecision Decision { get; private set; } = TestDecision.Next;

    public MonitorTestResultDialog(string header, PowerOffMode currentMode)
    {
        var s = Strings.Get();
        Text = s.TestTitle;
        ClientSize = new Size(480, 306);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        Controls.Add(new Label
        {
            Text = header, Left = 20, Top = 16, Width = 440, Height = 28,
            Font = new Font(Font.FontFamily, 12f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        });

        Controls.Add(new Label { Text = s.TestQSleep, Left = 20, Top = 54, Width = 440, Height = 20 });
        var sleepPanel = new Panel { Left = 20, Top = 76, Width = 440, Height = 26 };
        _sleepYes = new RadioButton { Text = s.TestYes, Left = 0, Top = 2, Width = 80, Checked = true };
        _sleepNo = new RadioButton { Text = s.TestNo, Left = 96, Top = 2, Width = 80 };
        sleepPanel.Controls.Add(_sleepYes);
        sleepPanel.Controls.Add(_sleepNo);
        Controls.Add(sleepPanel);

        Controls.Add(new Label { Text = s.TestQWake, Left = 20, Top = 110, Width = 440, Height = 20 });
        var wakePanel = new Panel { Left = 20, Top = 132, Width = 440, Height = 26 };
        _wakeYes = new RadioButton { Text = s.TestYes, Left = 0, Top = 2, Width = 80, Checked = true };
        _wakeNo = new RadioButton { Text = s.TestNo, Left = 96, Top = 2, Width = 80 };
        wakePanel.Controls.Add(_wakeYes);
        wakePanel.Controls.Add(_wakeNo);
        Controls.Add(wakePanel);

        Controls.Add(new Label
        {
            Text = s.PowerMethodLabel, Left = 20, Top = 172, Width = 150, Height = 24,
            TextAlign = ContentAlignment.MiddleLeft
        });
        _modeCombo = new ComboBox
        {
            Left = 176, Top = 169, Width = 284, DropDownStyle = ComboBoxStyle.DropDownList
        };
        _modeCombo.Items.AddRange(new object[] { s.ModeDpms, s.ModeAuto, s.ModeDdcCi, s.ModeBoth, s.ModeNone });
        _modeCombo.SelectedIndex = Math.Max(0, Array.IndexOf(ModeOrder, currentMode));
        Controls.Add(_modeCombo);

        var retry = new Button
        {
            Text = s.TestRetry, Left = 20, Top = 216, Width = 230, Height = 36,
            UseVisualStyleBackColor = true
        };
        retry.Click += (_, _) => { Decision = TestDecision.Retry; DialogResult = DialogResult.OK; Close(); };
        Controls.Add(retry);

        var next = new Button
        {
            Text = s.TestNext, Left = 260, Top = 216, Width = 200, Height = 36,
            UseVisualStyleBackColor = true
        };
        next.Click += (_, _) => { Decision = TestDecision.Next; DialogResult = DialogResult.OK; Close(); };
        Controls.Add(next);

        AcceptButton = next;
    }
}
