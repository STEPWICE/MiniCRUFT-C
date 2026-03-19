using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.Game;
using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class HungerSystemTests
{
    [Fact]
    public void HungerSystem_DrainsAndAllowsEating()
    {
        var config = new SurvivalConfig
        {
            Enabled = true,
            MaxHunger = 20,
            StartingHunger = 12f,
            BaseDrainPerSecond = 1f,
            SprintDrainMultiplier = 2f,
            LiquidDrainMultiplier = 1f,
            MinHungerToSprint = 4,
            StarvationDamageIntervalSeconds = 1f,
            StarvationDamage = 1
        };

        var system = new HungerSystem(config);
        var player = new Player(new Vector3(0f, 64f, 0f), new PhysicsConfig());

        system.Update(1f, player, sprinting: true, inLiquid: false);

        Assert.Equal(10f, system.Hunger, 3);
        Assert.True(system.CanSprint);
        Assert.True(system.TryEat(BlockId.CookedBeef, out int restored));
        Assert.Equal(8, restored);
        Assert.Equal(18f, system.Hunger, 3);
    }

    [Fact]
    public void HungerSystem_StarvationDamagesPlayer()
    {
        var config = new SurvivalConfig
        {
            Enabled = true,
            MaxHunger = 20,
            StartingHunger = 0f,
            BaseDrainPerSecond = 0f,
            SprintDrainMultiplier = 1f,
            LiquidDrainMultiplier = 1f,
            MinHungerToSprint = 4,
            StarvationDamageIntervalSeconds = 0.5f,
            StarvationDamage = 2
        };

        var system = new HungerSystem(config);
        var player = new Player(new Vector3(0f, 64f, 0f), new PhysicsConfig());

        int healthBefore = player.Health;

        system.Update(1f, player, sprinting: false, inLiquid: false);

        Assert.Equal(0f, system.Hunger, 3);
        Assert.Equal(healthBefore - 4, player.Health);
    }
}
