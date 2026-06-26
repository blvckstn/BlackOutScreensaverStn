using System;
using System.Drawing;

namespace PowerOffScreensaver;

/// <summary>
/// Pure decision logic for "should this input dismiss the screensaver?".
/// Mouse moves must leave a Euclidean dead-zone; the first move only sets the
/// baseline. Buttons, wheel and keys always trigger. Kept free of any Win32 /
/// WinForms dependency so it is fully unit-testable. Layer 1 of the lock spec.
/// </summary>
public sealed class InputGate
{
    public const int DefaultDeadZonePx = 5;

    private readonly int _deadZonePx;
    private Point? _baseline;

    public InputGate(int deadZonePx = DefaultDeadZonePx) => _deadZonePx = deadZonePx;

    /// <summary>First move establishes the baseline; later moves trigger once
    /// the cursor leaves the dead-zone (strictly greater than the radius).</summary>
    public bool OnMouseMove(Point position)
    {
        if (_baseline is null)
        {
            _baseline = position;
            return false;
        }

        var b = _baseline.Value;
        long dx = position.X - b.X;
        long dy = position.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy) > _deadZonePx;
    }

    /// <summary>Any key, mouse button or wheel event triggers immediately.</summary>
    public bool OnKeyOrButton() => true;
}
