using System;
using System.Windows.Forms;

namespace PowerOffScreensaver;

public class BlackoutForm : Form
{
    private const int DEAD_ZONE_PX = 5;
    private Point _initialMousePos = Point.Empty;
    private bool _exitRequested = false;

    public event EventHandler? ExitRequested;

    public BlackoutForm(Rectangle bounds)
    {
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Normal;
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = System.Drawing.Color.Black;
        Bounds = bounds;

        DoubleBuffered = false;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Cursor.Hide();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        Cursor.Show();
        base.OnFormClosing(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!_exitRequested)
        {
            if (_initialMousePos == Point.Empty)
            {
                _initialMousePos = e.Location;
            }
            else
            {
                int dx = e.Location.X - _initialMousePos.X;
                int dy = e.Location.Y - _initialMousePos.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance > DEAD_ZONE_PX)
                {
                    TriggerExit();
                }
            }
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        TriggerExit();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        TriggerExit();
    }

    private void TriggerExit()
    {
        if (!_exitRequested)
        {
            _exitRequested = true;
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    internal static bool IsOutsideDeadZone(Point initial, Point current, int deadZonePx = DEAD_ZONE_PX)
    {
        int dx = current.X - initial.X;
        int dy = current.Y - initial.Y;
        return Math.Sqrt(dx * dx + dy * dy) > deadZonePx;
    }
}
