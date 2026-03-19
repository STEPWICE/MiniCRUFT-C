using System.Collections.Generic;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.Game;
using MiniCRUFT.IO;
using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class SleepSystemTests
{
    [Fact]
    public void SleepSystem_RestsUntilMorning_WhenSafeAndDark()
    {
        var survival = new SurvivalConfig
        {
            Enabled = true,
            EnableRest = true,
            RestMinSunIntensity = 0.12f,
            RestWakeTimeOfDay = 0.25f,
            RestThreatRadius = 16f
        };

        var sleep = new SleepSystem(survival);
        var player = new Player(new Vector3(0f, 64f, 0f), new PhysicsConfig());
        player.OnGround = true;

        var mobs = new MobSystem(1337, new MobConfig { Enabled = false });
        var dayNight = new DayNightCycle(new DayNightConfig { StartTimeOfDay = 0.75f });
        dayNight.SetState(0.75f, 3);

        Assert.True(sleep.TryRest(player, mobs, dayNight));
        Assert.Equal(0.25f, dayNight.TimeOfDay, 4);
        Assert.Equal(4, dayNight.DayCount);
    }

    [Fact]
    public void SleepSystem_RejectsWhenHostileNearby()
    {
        var survival = new SurvivalConfig
        {
            Enabled = true,
            EnableRest = true,
            RestMinSunIntensity = 0.12f,
            RestWakeTimeOfDay = 0.25f,
            RestThreatRadius = 16f
        };

        var sleep = new SleepSystem(survival);
        var player = new Player(new Vector3(0f, 64f, 0f), new PhysicsConfig());
        player.OnGround = true;

        var mobs = new MobSystem(1337, new MobConfig { Enabled = false });
        mobs.Load(new[]
        {
            new MobSaveData(
                MobType.Zombie,
                new Vector3(2f, 64f, 0f),
                Vector3.Zero,
                new Vector3(2f, 64f, 0f),
                0f,
                0f,
                20,
                0f,
                0f,
                0f,
                0f,
                0f,
                false,
                true,
                0f,
                0f)
        });

        var dayNight = new DayNightCycle(new DayNightConfig { StartTimeOfDay = 0.75f });
        dayNight.SetState(0.75f, 3);

        Assert.False(sleep.TryRest(player, mobs, dayNight));
        Assert.Equal(0.75f, dayNight.TimeOfDay, 4);
        Assert.Equal(3, dayNight.DayCount);
    }
}
