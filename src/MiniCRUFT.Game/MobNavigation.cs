using System;
using System.Numerics;

namespace MiniCRUFT.Game;

public readonly struct MobNavigationSample
{
    public Vector2 Direction { get; }
    public float Speed { get; }
    public bool HasGroundAhead { get; }
    public bool HasHeadClear { get; }

    public MobNavigationSample(Vector2 direction, float speed, bool hasGroundAhead, bool hasHeadClear)
    {
        Direction = direction;
        Speed = speed;
        HasGroundAhead = hasGroundAhead;
        HasHeadClear = hasHeadClear;
    }
}

public static class MobNavigation
{
    public static Vector2 NormalizeOrZero(Vector2 direction)
    {
        return direction.LengthSquared() > float.Epsilon ? Vector2.Normalize(direction) : Vector2.Zero;
    }

    public static Vector2 Rotate(Vector2 direction, float radians)
    {
        if (direction.LengthSquared() <= float.Epsilon)
        {
            return Vector2.Zero;
        }

        float sin = MathF.Sin(radians);
        float cos = MathF.Cos(radians);
        return new Vector2(
            (direction.X * cos) - (direction.Y * sin),
            (direction.X * sin) + (direction.Y * cos));
    }

    public static Vector2 SelectBestDirection(Vector2 preferredDirection, ReadOnlySpan<MobNavigationSample> samples, out float speed)
    {
        Vector2 preferred = NormalizeOrZero(preferredDirection);
        if (preferred.LengthSquared() <= float.Epsilon || samples.Length == 0)
        {
            speed = 0f;
            return Vector2.Zero;
        }

        float bestScore = float.NegativeInfinity;
        Vector2 bestDirection = Vector2.Zero;
        float bestSpeed = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            MobNavigationSample sample = samples[i];
            Vector2 candidate = NormalizeOrZero(sample.Direction);
            if (candidate.LengthSquared() <= float.Epsilon)
            {
                continue;
            }

            float alignment = Vector2.Dot(candidate, preferred);
            float score = alignment * 4.5f;
            score += sample.HasGroundAhead ? 1.75f : -3.5f;
            score += sample.HasHeadClear ? 2.5f : -6f;
            score -= (1f - alignment) * 0.35f;
            if (!sample.HasGroundAhead || !sample.HasHeadClear)
            {
                float turnStrength = MathF.Abs((candidate.X * preferred.Y) - (candidate.Y * preferred.X));
                score += turnStrength * turnStrength * 5f;
            }
            score += sample.Speed * 0.02f;

            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = candidate;
                bestSpeed = sample.Speed;
            }
        }

        if (bestScore == float.NegativeInfinity)
        {
            speed = 0f;
            return Vector2.Zero;
        }

        speed = bestSpeed;
        return bestDirection;
    }
}
