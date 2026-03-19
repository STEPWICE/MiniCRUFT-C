using System;
using System.Numerics;
using MiniCRUFT.Core;

namespace MiniCRUFT.Game;

public sealed class SleepSystem
{
    private readonly SurvivalConfig _config;

    public SleepSystem(SurvivalConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public bool CanRest(Player player, MobSystem mobs, DayNightCycle dayNight)
    {
        if (!_config.Enabled || !_config.EnableRest)
        {
            return false;
        }

        if (player is null)
        {
            throw new ArgumentNullException(nameof(player));
        }

        if (mobs is null)
        {
            throw new ArgumentNullException(nameof(mobs));
        }

        if (dayNight is null)
        {
            throw new ArgumentNullException(nameof(dayNight));
        }

        if (dayNight.GetSunIntensity() > _config.RestMinSunIntensity)
        {
            return false;
        }

        if (!player.OnGround || player.InWater || player.InLava)
        {
            return false;
        }

        if (player.Velocity.LengthSquared() > 0.04f)
        {
            return false;
        }

        return !mobs.HasHostileNearby(player.Position, _config.RestThreatRadius);
    }

    public bool TryRest(Player player, MobSystem mobs, DayNightCycle dayNight)
    {
        if (!CanRest(player, mobs, dayNight))
        {
            return false;
        }

        dayNight.SetState(Math.Clamp(_config.RestWakeTimeOfDay, 0f, 1f), dayNight.DayCount + 1);
        return true;
    }
}
