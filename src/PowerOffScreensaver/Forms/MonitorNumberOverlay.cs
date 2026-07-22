using System.Drawing;
using System.Windows.Forms;

namespace PowerOffScreensaver;

/// <summary>
/// A small, semi-transparent number badge shown in the centre of a monitor while
/// the test dialog is open, so the user can tell which physical monitor is #1/#2/#3
/// (raw names are usually all "Generic PnP Monitor"). Never takes focus or blocks
/// clicks.
/// </summary>
public sealed class MonitorNumberOverlay : Form
{
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TRANSPARENT = 0x00000020;

    public MonitorNumberOverlay(Rectangle monitorBounds, string number, bool highlight = false)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        Opacity = 0.82;
        BackColor = highlight ? Color.FromArgb(180, 90, 0) : Color.FromArgb(24, 24, 24);
        Size = new Size(150, 150);
        Location = new Point(
            monitorBounds.Left + (monitorBounds.Width - Width) / 2,
            monitorBounds.Top + (monitorBounds.Height - Height) / 2);

        Controls.Add(new Label
        {
            Text = number,
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 60f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        });
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TRANSPARENT;
            return cp;
        }
    }
}
