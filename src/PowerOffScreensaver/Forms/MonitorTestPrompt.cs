using System.Drawing;
using System.Windows.Forms;

namespace PowerOffScreensaver;

/// <summary>
/// A large, draggable window placed on the monitor currently under test. It shows
/// which monitor is being tested and the current phase (warning countdown, dark
/// dwell, woke), so the identification is tied to the actual screen. The whole
/// window is draggable so the user can move it aside.
/// </summary>
public sealed class MonitorTestPrompt : Form
{
    private const int WM_NCHITTEST = 0x0084;
    private const int HTCLIENT = 1;
    private const int HTCAPTION = 2;

    private readonly Label _header;
    private readonly Label _message;
    private readonly Label _big;

    public MonitorTestPrompt(Rectangle monitorBounds)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.FromArgb(18, 18, 20);
        Size = new Size(560, 320);
        Location = new Point(
            monitorBounds.Left + (monitorBounds.Width - Width) / 2,
            monitorBounds.Top + (monitorBounds.Height - Height) / 2);

        var border = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2), BackColor = Color.FromArgb(90, 90, 96) };
        var inner = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 18, 20) };
        border.Controls.Add(inner);
        Controls.Add(border);

        _header = new Label
        {
            Left = 20, Top = 20, Width = 520, Height = 40,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        _message = new Label
        {
            Left = 20, Top = 74, Width = 520, Height = 60,
            ForeColor = Color.Gainsboro,
            Font = new Font("Segoe UI", 12f),
            TextAlign = ContentAlignment.MiddleCenter
        };
        _big = new Label
        {
            Left = 20, Top = 140, Width = 520, Height = 150,
            ForeColor = Color.FromArgb(255, 170, 60),
            Font = new Font("Segoe UI", 72f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        inner.Controls.Add(_header);
        inner.Controls.Add(_message);
        inner.Controls.Add(_big);
    }

    protected override bool ShowWithoutActivation => true;

    public void SetPhase(string header, string message, string big, Color bigColor)
    {
        _header.Text = header;
        _message.Text = message;
        _big.Text = big;
        _big.ForeColor = bigColor;
    }

    // Make the whole window draggable.
    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == WM_NCHITTEST && m.Result.ToInt32() == HTCLIENT)
            m.Result = new System.IntPtr(HTCAPTION);
    }
}
