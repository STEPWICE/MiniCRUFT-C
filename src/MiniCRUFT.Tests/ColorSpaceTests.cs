using System.Numerics;
using MiniCRUFT.Core;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class ColorSpaceTests
{
    [Fact]
    public void LinearSrgb_RoundTripPreservesValues()
    {
        var linear = new Vector3(0.18f, 0.5f, 0.85f);
        var srgb = ColorSpace.ToSrgb(linear);
        var roundTrip = ColorSpace.ToLinear(srgb);

        Assert.InRange(roundTrip.X, linear.X - 0.01f, linear.X + 0.01f);
        Assert.InRange(roundTrip.Y, linear.Y - 0.01f, linear.Y + 0.01f);
        Assert.InRange(roundTrip.Z, linear.Z - 0.01f, linear.Z + 0.01f);
    }
}
