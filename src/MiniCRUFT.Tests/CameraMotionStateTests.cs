using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.Game;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class CameraMotionStateTests
{
    [Fact]
    public void Update_AppliesBob_AndDecaysAfterMovementStops()
    {
        var motion = new CameraMotionState();
        var config = new CameraMotionConfig
        {
            Enabled = true,
            BobAmplitude = 0.03f,
            BobSpeed = 7f,
            BobLateralFactor = 0.55f,
            BobForwardFactor = 0.25f,
            InertiaStrength = 14f,
            AirborneMultiplier = 0.2f,
            LiquidMultiplier = 0.35f
        };

        motion.Update(0.016f, 45f, new Vector3(6f, 0f, 0f), onGround: true, inLiquid: false, walkSpeed: 6f, config);
        Vector3 movingOffset = motion.CurrentOffset;

        Assert.True(movingOffset.Length() > 0f);

        motion.Update(0.25f, 45f, Vector3.Zero, onGround: true, inLiquid: false, walkSpeed: 6f, config);
        Vector3 stoppedOffset = motion.CurrentOffset;

        Assert.True(stoppedOffset.Length() < movingOffset.Length());
    }

    [Fact]
    public void Reset_ClearsTheMotionOffset()
    {
        var motion = new CameraMotionState();
        var config = new CameraMotionConfig();

        motion.Update(0.016f, 0f, new Vector3(6f, 0f, 0f), onGround: true, inLiquid: false, walkSpeed: 6f, config);
        motion.Reset();

        Assert.Equal(Vector3.Zero, motion.CurrentOffset);
    }
}
