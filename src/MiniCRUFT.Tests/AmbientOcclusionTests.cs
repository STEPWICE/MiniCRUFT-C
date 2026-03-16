using MiniCRUFT.Renderer;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class AmbientOcclusionTests
{
    [Theory]
    [InlineData(false, false, false, 1.0f)]
    [InlineData(true, false, false, 0.8f)]
    [InlineData(false, true, false, 0.8f)]
    [InlineData(false, false, true, 0.8f)]
    [InlineData(true, true, false, 0.6f)]
    public void ComputeAmbientOcclusion_MatchesExpected(bool side1, bool side2, bool corner, float expected)
    {
        float value = LightModel.ComputeAmbientOcclusion(side1, side2, corner, min: 0.6f, strength: 1.0f);
        Assert.InRange(value, expected - 0.001f, expected + 0.001f);
    }
}
