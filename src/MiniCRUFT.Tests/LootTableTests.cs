using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.Game;
using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class LootTableTests
{
    [Fact]
    public void RollMobDrops_IsDeterministic_ForSameInput()
    {
        var first = new List<LootDrop>();
        var second = new List<LootDrop>();
        Vector3 position = new(12.4f, 64.2f, -5.8f);

        LootTable.RollMobDrops(MobType.Zombie, 1337, position, first);
        LootTable.RollMobDrops(MobType.Zombie, 1337, position, second);

        Assert.Equal(first, second);
    }

    [Fact]
    public void RollMobDrops_EliteMobAwardsMobTrophy()
    {
        var loot = new List<LootDrop>();

        LootTable.RollMobDrops(MobType.Zombie, true, 1337, new Vector3(12.4f, 64.2f, -5.8f), loot);

        Assert.Contains(loot, drop => drop.Item == BlockId.MobTrophy);
    }

    [Fact]
    public void RollMobDrops_RareEliteVariantAwardsMoreTrophies()
    {
        var baseLoot = new List<LootDrop>();
        var rareLoot = new List<LootDrop>();
        Vector3 position = new(12.4f, 64.2f, -5.8f);

        LootTable.RollMobDrops(MobType.Zombie, true, EliteMobVariant.None, 1337, position, baseLoot);
        LootTable.RollMobDrops(MobType.Zombie, true, EliteMobVariant.Warden, 1337, position, rareLoot);

        Assert.Equal(1, baseLoot.Single(drop => drop.Item == BlockId.MobTrophy).Count);
        Assert.Equal(3, rareLoot.Single(drop => drop.Item == BlockId.MobTrophy).Count);
        Assert.True(rareLoot.Count >= baseLoot.Count);
    }

    [Fact]
    public void RollChestLoot_IsDeterministic_ForSameInput()
    {
        var first = new List<LootDrop>();
        var second = new List<LootDrop>();
        var position = new BlockCoord(32, 28, -12);

        LootTable.RollChestLoot(4242, position, BiomeId.Desert, 28, first);
        LootTable.RollChestLoot(4242, position, BiomeId.Desert, 28, second);

        Assert.Equal(first, second);
    }

    [Fact]
    public void RollChestLoot_IsDeterministic_ForPoiKind()
    {
        var first = new List<LootDrop>();
        var second = new List<LootDrop>();
        var position = new BlockCoord(16, 18, 16);

        LootTable.RollChestLoot(4242, position, BiomeId.Plains, 18, PoiLootKind.MineShaft, first);
        LootTable.RollChestLoot(4242, position, BiomeId.Plains, 18, PoiLootKind.MineShaft, second);

        Assert.Equal(first, second);
    }

    [Fact]
    public void RollChestLoot_ReturnsAtLeastOneItem_ForRepresentativeRoll()
    {
        var loot = new List<LootDrop>();

        LootTable.RollChestLoot(9999, new BlockCoord(48, 18, 48), BiomeId.Plains, 18, loot);

        Assert.NotEmpty(loot);
        Assert.All(loot, drop => Assert.True(drop.Count > 0));
    }

    [Fact]
    public void RollChestLoot_ReturnsAtLeastOneItem_ForPoiKind()
    {
        var loot = new List<LootDrop>();

        LootTable.RollChestLoot(9999, new BlockCoord(48, 18, 48), BiomeId.Plains, 18, PoiLootKind.Ruin, loot);

        Assert.NotEmpty(loot);
        Assert.All(loot, drop => Assert.True(drop.Count > 0));
    }
}
