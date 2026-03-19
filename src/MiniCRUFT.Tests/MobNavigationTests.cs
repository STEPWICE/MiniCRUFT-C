using System.Numerics;
using MiniCRUFT.Game;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class MobNavigationTests
{
    [Fact]
    public void SelectBestDirection_PrefersSideRoute_WhenForwardPathIsBlocked()
    {
        Vector2 preferred = Vector2.UnitX;
        var samples = new[]
        {
            new MobNavigationSample(new Vector2(1f, 0f), 1f, false, false),
            new MobNavigationSample(MobNavigation.Rotate(preferred, 0.35f), 0.95f, true, true),
            new MobNavigationSample(MobNavigation.Rotate(preferred, -0.35f), 0.95f, true, true)
        };

        Vector2 chosen = MobNavigation.SelectBestDirection(preferred, samples, out float speed);

        Assert.True(chosen.X > 0f);
        Assert.True(chosen.Y > 0f);
        Assert.True(speed > 0f);
    }

    [Fact]
    public void SelectBestDirection_KeepsForwardRoute_WhenItIsClear()
    {
        Vector2 preferred = Vector2.UnitX;
        var samples = new[]
        {
            new MobNavigationSample(new Vector2(1f, 0f), 1f, true, true),
            new MobNavigationSample(MobNavigation.Rotate(preferred, 0.35f), 0.95f, true, true),
            new MobNavigationSample(MobNavigation.Rotate(preferred, -0.35f), 0.95f, true, true)
        };

        Vector2 chosen = MobNavigation.SelectBestDirection(preferred, samples, out float speed);

        Assert.True(chosen.X > 0.9f);
        Assert.InRange(MathF.Abs(chosen.Y), 0f, 0.1f);
        Assert.Equal(1f, speed);
    }
}
