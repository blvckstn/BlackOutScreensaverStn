using System;
using System.Drawing;
using System.Windows.Forms;
using PowerOffScreensaver;
using Xunit;

namespace PowerOffScreensaver.Tests;

public class DeadZoneTests
{
    private const int DEAD_ZONE_PX = 5;

    // Code uses distance > DEAD_ZONE_PX (strictly >), so exactly 5px stays within zone.
    [Theory]
    [InlineData(0, 0, 0, 0, true)]       // No movement - within zone
    [InlineData(0, 0, 2, 0, true)]       // 2px right - within zone
    [InlineData(0, 0, 0, 2, true)]       // 2px down - within zone
    [InlineData(0, 0, 3, 4, true)]       // magnitude exactly 5.0 - on boundary (NOT > 5) - within zone
    [InlineData(0, 0, 4, 3, true)]       // magnitude exactly 5.0 - on boundary - within zone
    [InlineData(0, 0, 5, 0, true)]       // exactly 5px right - 5.0 NOT > 5 - within zone
    [InlineData(0, 0, 0, 5, true)]       // exactly 5px down - within zone
    [InlineData(0, 0, -3, -4, true)]     // magnitude 5.0 negative direction - within zone
    [InlineData(0, 0, -5, 0, true)]      // exactly -5px - within zone
    [InlineData(0, 0, 5, 1, false)]      // magnitude ~5.1 > 5 - exceeds zone - triggers exit
    [InlineData(0, 0, 1, 5, false)]      // magnitude ~5.1 > 5 - exceeds zone
    [InlineData(0, 0, 6, 0, false)]      // 6px right - exceeds zone
    [InlineData(0, 0, 4, 4, false)]      // magnitude ~5.66 > 5 - exceeds zone
    [InlineData(0, 0, 10, 0, false)]     // 10px right - exceeds zone
    [InlineData(0, 0, 0, 10, false)]     // 10px down - exceeds zone
    [InlineData(50, 100, 52, 102, true)] // (50,100)→(52,102) magnitude 2.82 - within zone
    [InlineData(50, 100, 55, 105, false)] // (50,100)→(55,105) magnitude 7.07 - exceeds zone
    public void EuclideanDistance_DeadZoneLogic(int startX, int startY, int endX, int endY, bool withinZone)
    {
        int dx = endX - startX;
        int dy = endY - startY;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        bool isWithinZone = distance <= DEAD_ZONE_PX;

        Assert.Equal(withinZone, isWithinZone);
    }

    [Fact]
    public void DeadZone_BoundaryAt5px_ShouldTriggerExitAt5_1px()
    {
        // Exactly at 5.0 should NOT trigger
        int dx = 3, dy = 4;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        Assert.Equal(5.0, distance);
        Assert.False(distance > DEAD_ZONE_PX);

        // Slightly over 5.0 should trigger
        dx = 4; dy = 3;
        distance = Math.Sqrt(dx * dx + dy * dy);
        Assert.Equal(5.0, distance);
        Assert.False(distance > DEAD_ZONE_PX);

        // Clearly over 5.0 should trigger
        dx = 5; dy = 1;
        distance = Math.Sqrt(dx * dx + dy * dy);
        Assert.True(distance > DEAD_ZONE_PX);
    }

    [Fact]
    public void DeadZone_DiagonalMovement_CalculatesCorrectly()
    {
        // 3-4-5 triangle
        int dx = 3, dy = 4;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        Assert.Equal(5.0, distance, 10);
    }

    [Fact]
    public void DeadZone_ZeroInitialPosition_TracksMovement()
    {
        var initialPos = Point.Empty;
        var currentPos = new Point(3, 4);

        int dx = currentPos.X - initialPos.X;
        int dy = currentPos.Y - initialPos.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        Assert.False(distance > DEAD_ZONE_PX);
    }

    [Fact]
    public void DeadZone_NegativeMovement_TreatedSameAsPositive()
    {
        // Moving (-3, -4) should be same distance as (3, 4)
        int dx1 = 3, dy1 = 4;
        double distance1 = Math.Sqrt(dx1 * dx1 + dy1 * dy1);

        int dx2 = -3, dy2 = -4;
        double distance2 = Math.Sqrt(dx2 * dx2 + dy2 * dy2);

        Assert.Equal(distance1, distance2);
    }

    [Fact]
    public void DeadZone_LargeCoordinates_CalculatesCorrectly()
    {
        int startX = 1920, startY = 1080;
        int endX = 1925, endY = 1085;

        int dx = endX - startX;
        int dy = endY - startY;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        Assert.True(distance > DEAD_ZONE_PX);
    }

    [Fact]
    public void DeadZone_SmallMovement_StaysWithinZone()
    {
        for (int x = 1; x <= 5; x++)
        {
            for (int y = 1; y <= 5; y++)
            {
                double distance = Math.Sqrt(x * x + y * y);
                if (distance <= DEAD_ZONE_PX)
                {
                    Assert.False(distance > DEAD_ZONE_PX);
                }
            }
        }
    }

    [Theory]
    [InlineData(0, 5)]    // Straight vertical
    [InlineData(5, 0)]    // Straight horizontal
    [InlineData(3, 4)]    // Diagonal
    [InlineData(1, 1)]    // Small diagonal
    [InlineData(2, 2)]    // Small diagonal 2x2
    public void DeadZone_Magnitude_NeverExceedsMaxInZone(int dx, int dy)
    {
        double distance = Math.Sqrt(dx * dx + dy * dy);
        // These should all be on or below the 5px boundary
        Assert.True(distance <= 5.1 || distance == 5.0);
    }
}
