using System.Numerics;
using MiniCRUFT.Core;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class SpatialAudioTests
{
    [Fact]
    public void Calculate_AttenuatesByDistance_AndPansByListenerRightVector()
    {
        var config = new AudioConfig
        {
            SpatialInnerRadius = 2f,
            SpatialOuterRadius = 10f,
            SpatialPanStrength = 1f
        };

        var near = SpatialAudio.Calculate(Vector3.Zero, new Vector3(1f, 0f, 0f), Vector3.UnitX, config, 1f);
        Assert.Equal(1f, near.Volume);
        Assert.Equal(1f, near.Pan);

        var mid = SpatialAudio.Calculate(Vector3.Zero, new Vector3(6f, 0f, 0f), Vector3.UnitX, config, 1f);
        Assert.InRange(mid.Volume, 0.49f, 0.51f);
        Assert.True(mid.Pan > 0.99f);

        var left = SpatialAudio.Calculate(Vector3.Zero, new Vector3(-6f, 0f, 0f), Vector3.UnitX, config, 1f);
        Assert.True(left.Pan < -0.99f);

        var far = SpatialAudio.Calculate(Vector3.Zero, new Vector3(20f, 0f, 0f), Vector3.UnitX, config, 1f);
        Assert.Equal(0f, far.Volume);
        Assert.Equal(0f, far.Pan);
    }
}
