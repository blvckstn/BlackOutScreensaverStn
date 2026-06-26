using System.Drawing;
using PowerOffScreensaver;
using Xunit;

namespace PowerOffScreensaver.Tests;

public class InputGateTests
{
    [Fact]
    public void FirstMouseMove_OnlySetsBaseline_DoesNotTrigger()
    {
        var gate = new InputGate();
        Assert.False(gate.OnMouseMove(new Point(100, 100)));
    }

    [Theory]
    [InlineData(0, 0)]   // no movement
    [InlineData(2, 0)]   // 2px
    [InlineData(3, 4)]   // exactly 5.0 — boundary, NOT > 5
    [InlineData(5, 0)]   // exactly 5px
    [InlineData(-3, -4)] // 5.0 in negative direction
    public void MoveWithinDeadZone_DoesNotTrigger(int dx, int dy)
    {
        var gate = new InputGate();
        gate.OnMouseMove(new Point(100, 100)); // baseline
        Assert.False(gate.OnMouseMove(new Point(100 + dx, 100 + dy)));
    }

    [Theory]
    [InlineData(5, 1)]   // ~5.10
    [InlineData(6, 0)]   // 6px
    [InlineData(4, 4)]   // ~5.66
    [InlineData(100, 0)] // far
    public void MoveBeyondDeadZone_Triggers(int dx, int dy)
    {
        var gate = new InputGate();
        gate.OnMouseMove(new Point(100, 100)); // baseline
        Assert.True(gate.OnMouseMove(new Point(100 + dx, 100 + dy)));
    }

    [Fact]
    public void KeyOrButton_AlwaysTriggers()
    {
        var gate = new InputGate();
        Assert.True(gate.OnKeyOrButton());
    }

    [Fact]
    public void CustomDeadZone_IsRespected()
    {
        var gate = new InputGate(deadZonePx: 20);
        gate.OnMouseMove(new Point(0, 0));
        Assert.False(gate.OnMouseMove(new Point(15, 0)));
        Assert.True(gate.OnMouseMove(new Point(25, 0)));
    }

    [Fact]
    public void BaselineIsStable_AcrossMultipleSmallMoves()
    {
        var gate = new InputGate();
        gate.OnMouseMove(new Point(500, 500)); // baseline
        Assert.False(gate.OnMouseMove(new Point(502, 500)));
        Assert.False(gate.OnMouseMove(new Point(500, 503)));
        // Still measured from the original baseline, not the last point.
        Assert.True(gate.OnMouseMove(new Point(510, 500)));
    }
}
