using MiniCRUFT.Renderer;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class FogFactorTests
{
    [Fact]
    public void LinearFog_ClampsAtStartAndEnd()
    {
        float start = 50f;
        float end = 100f;

        Assert.Equal(1f, LightModel.ComputeFogFactor(50f, start, end, linear: true), 3);
        Assert.Equal(0f, LightModel.ComputeFogFactor(100f, start, end, linear: true), 3);
        Assert.InRange(LightModel.ComputeFogFactor(75f, start, end, linear: true), 0.4f, 0.6f);
    }

    [Fact]
    public void ExpFog_StaysBetweenZeroAndOne()
    {
        float start = 50f;
        float end = 150f;

        float near = LightModel.ComputeFogFactor(60f, start, end, linear: false);
        float far = LightModel.ComputeFogFactor(140f, start, end, linear: false);

        Assert.InRange(near, 0f, 1f);
        Assert.InRange(far, 0f, 1f);
        Assert.True(near > far);
    }
}
