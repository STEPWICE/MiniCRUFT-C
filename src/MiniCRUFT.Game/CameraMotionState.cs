using System;
using System.Numerics;
using MiniCRUFT.Core;

namespace MiniCRUFT.Game;

public sealed class CameraMotionState
{
    private float _phase;

    public Vector3 CurrentOffset { get; private set; }

    public void Reset()
    {
        _phase = 0f;
        CurrentOffset = Vector3.Zero;
    }

    public Vector3 Update(float dt, float yawDegrees, Vector3 velocity, bool onGround, bool inLiquid, float walkSpeed, CameraMotionConfig config)
    {
        if (!config.Enabled)
        {
            Reset();
            return CurrentOffset;
        }

        if (dt <= 0f)
        {
            return CurrentOffset;
        }

        float horizontalSpeed = new Vector2(velocity.X, velocity.Z).Length();
        float speedScale = Math.Clamp(horizontalSpeed / Math.Max(0.01f, walkSpeed), 0f, 1f);
        float environmentScale = onGround ? 1f : config.AirborneMultiplier;
        if (inLiquid)
        {
            environmentScale = Math.Min(environmentScale, config.LiquidMultiplier);
        }

        float bobStrength = speedScale * environmentScale;
        if (config.BobSpeed <= 0f || config.BobAmplitude <= 0f)
        {
            bobStrength = 0f;
        }

        if (bobStrength > 0f && config.BobSpeed > 0f)
        {
            _phase += dt * config.BobSpeed * (0.75f + bobStrength);
            if (_phase > MathF.Tau)
            {
                _phase -= MathF.Tau;
            }
        }

        float yawRad = MathF.PI * yawDegrees / 180f;
        Vector3 forward = new(MathF.Cos(yawRad), 0f, MathF.Sin(yawRad));
        Vector3 right = new(-forward.Z, 0f, forward.X);

        float vertical = MathF.Sin(_phase) * config.BobAmplitude * bobStrength;
        float lateral = MathF.Cos(_phase * 0.5f) * config.BobAmplitude * config.BobLateralFactor * bobStrength;
        float forwardOffset = MathF.Sin(_phase * 0.5f) * config.BobAmplitude * config.BobForwardFactor * bobStrength;

        var target = right * lateral + Vector3.UnitY * vertical + forward * forwardOffset;
        float smoothing = config.InertiaStrength <= 0f ? 1f : 1f - MathF.Exp(-config.InertiaStrength * dt);
        CurrentOffset = Vector3.Lerp(CurrentOffset, target, smoothing);
        return CurrentOffset;
    }
}
