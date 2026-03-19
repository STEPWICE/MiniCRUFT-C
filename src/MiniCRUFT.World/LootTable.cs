using System;
using System.Collections.Generic;
using System.Numerics;
using MiniCRUFT.Core;

namespace MiniCRUFT.World;

public static class LootTable
{
    public static void RollMobDrops(MobType type, int seed, Vector3 position, List<LootDrop> target)
    {
        RollMobDrops(type, false, EliteMobVariant.None, 1f, seed, position, target);
    }

    public static void RollMobDrops(MobType type, bool elite, int seed, Vector3 position, List<LootDrop> target)
    {
        RollMobDrops(type, elite, EliteMobVariant.None, 1f, seed, position, target);
    }

    public static void RollMobDrops(MobType type, bool elite, EliteMobVariant eliteVariant, int seed, Vector3 position, List<LootDrop> target)
    {
        RollMobDrops(type, elite, eliteVariant, 1f, seed, position, target);
    }

    public static void RollMobDrops(MobType type, bool elite, float eliteDropMultiplier, int seed, Vector3 position, List<LootDrop> target)
    {
        RollMobDrops(type, elite, EliteMobVariant.None, eliteDropMultiplier, seed, position, target);
    }

    public static void RollMobDrops(MobType type, bool elite, EliteMobVariant eliteVariant, float eliteDropMultiplier, int seed, Vector3 position, List<LootDrop> target)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.Clear();
        eliteVariant = NormalizeVariant(eliteVariant);

        int x = MathUtil.FloorToInt(position.X);
        int y = MathUtil.FloorToInt(position.Y);
        int z = MathUtil.FloorToInt(position.Z);
        var rand = new Random(Hash(seed, x, y, z, (int)type, elite ? 1 : 0, (int)eliteVariant));

        switch (type)
        {
            case MobType.Zombie:
                AddIf(rand, 0.95f, target, BlockId.RottenFlesh, 1 + rand.Next(2));
                break;
            case MobType.Creeper:
                AddIf(rand, 0.9f, target, BlockId.Gunpowder, 1 + rand.Next(2));
                break;
            case MobType.Cow:
                AddIf(rand, 0.95f, target, BlockId.RawBeef, 1 + rand.Next(2));
                AddIf(rand, 0.7f, target, BlockId.Leather, 1 + rand.Next(2));
                break;
            case MobType.Sheep:
                AddIf(rand, 0.95f, target, BlockId.RawMutton, 1 + rand.Next(2));
                AddIf(rand, 0.7f, target, BlockId.String, 1 + rand.Next(2));
                break;
            case MobType.Chicken:
                AddIf(rand, 0.95f, target, BlockId.RawChicken, 1 + rand.Next(2));
                AddIf(rand, 0.7f, target, BlockId.Feather, 1 + rand.Next(2));
                break;
            case MobType.Herobrine:
                AddIf(rand, 0.35f, target, BlockId.Torch, 1 + rand.Next(2));
                AddIf(rand, 0.9f, target, BlockId.MobTrophy, 1);
                AddIf(rand, 0.55f, target, BlockId.Diamond, 1 + rand.Next(2));
                AddIf(rand, 0.45f, target, BlockId.GoldIngot, 1);
                break;
        }

        if (elite)
        {
            AddEliteMobDrops(rand, type, eliteVariant, eliteDropMultiplier, target);
        }
    }

    private static void AddEliteMobDrops(Random rand, MobType type, EliteMobVariant eliteVariant, float eliteDropMultiplier, List<LootDrop> target)
    {
        float dropScale = Math.Max(1f, eliteDropMultiplier);
        AddIf(rand, 1f, target, BlockId.MobTrophy, ScaleCount(GetEliteTrophyCount(eliteVariant), dropScale));

        switch (eliteVariant)
        {
            case EliteMobVariant.Brute:
                AddIf(rand, 0.9f, target, BlockId.IronIngot, ScaleCount(2 + rand.Next(2), dropScale));
                AddIf(rand, 0.65f, target, BlockId.GoldIngot, ScaleCount(1 + rand.Next(2), dropScale));
                break;
            case EliteMobVariant.Hunter:
                AddIf(rand, 0.9f, target, BlockId.Diamond, ScaleCount(1 + rand.Next(2), dropScale));
                AddIf(rand, 0.7f, target, BlockId.GoldIngot, ScaleCount(1 + rand.Next(2), dropScale));
                break;
            case EliteMobVariant.Warden:
                AddIf(rand, 0.95f, target, BlockId.Diamond, ScaleCount(2 + rand.Next(2), dropScale));
                AddIf(rand, 0.85f, target, BlockId.GoldIngot, ScaleCount(2 + rand.Next(2), dropScale));
                AddIf(rand, 0.8f, target, BlockId.IronIngot, ScaleCount(2 + rand.Next(2), dropScale));
                break;
        }

        switch (type)
        {
            case MobType.Zombie:
                AddIf(rand, 0.9f, target, BlockId.RottenFlesh, ScaleCount(2 + rand.Next(3), dropScale));
                AddIf(rand, 0.7f, target, BlockId.IronIngot, ScaleCount(1 + rand.Next(2), dropScale));
                AddIf(rand, 0.25f, target, BlockId.GoldIngot, ScaleCount(1, dropScale));
                break;
            case MobType.Creeper:
                AddIf(rand, 0.9f, target, BlockId.Gunpowder, ScaleCount(2 + rand.Next(3), dropScale));
                AddIf(rand, 0.5f, target, BlockId.Diamond, ScaleCount(1, dropScale));
                AddIf(rand, 0.25f, target, BlockId.GoldIngot, ScaleCount(1, dropScale));
                break;
            case MobType.Cow:
                AddIf(rand, 0.9f, target, BlockId.RawBeef, ScaleCount(2 + rand.Next(3), dropScale));
                AddIf(rand, 0.8f, target, BlockId.Leather, ScaleCount(2 + rand.Next(2), dropScale));
                AddIf(rand, 0.35f, target, BlockId.GoldIngot, ScaleCount(1, dropScale));
                break;
            case MobType.Sheep:
                AddIf(rand, 0.9f, target, BlockId.RawMutton, ScaleCount(2 + rand.Next(3), dropScale));
                AddIf(rand, 0.8f, target, BlockId.String, ScaleCount(2 + rand.Next(2), dropScale));
                AddIf(rand, 0.25f, target, BlockId.Diamond, ScaleCount(1, dropScale));
                break;
            case MobType.Chicken:
                AddIf(rand, 0.9f, target, BlockId.RawChicken, ScaleCount(2 + rand.Next(3), dropScale));
                AddIf(rand, 0.8f, target, BlockId.Feather, ScaleCount(2 + rand.Next(2), dropScale));
                AddIf(rand, 0.25f, target, BlockId.GoldIngot, ScaleCount(1, dropScale));
                break;
            case MobType.Herobrine:
                AddIf(rand, 0.85f, target, BlockId.Diamond, ScaleCount(1 + rand.Next(2), dropScale));
                AddIf(rand, 0.65f, target, BlockId.GoldIngot, ScaleCount(1 + rand.Next(2), dropScale));
                AddIf(rand, 0.35f, target, BlockId.IronIngot, ScaleCount(1 + rand.Next(2), dropScale));
                break;
        }
    }

    private static int GetEliteTrophyCount(EliteMobVariant eliteVariant)
    {
        return eliteVariant switch
        {
            EliteMobVariant.Brute => 2,
            EliteMobVariant.Hunter => 2,
            EliteMobVariant.Warden => 3,
            _ => 1
        };
    }

    private static EliteMobVariant NormalizeVariant(EliteMobVariant eliteVariant)
    {
        return eliteVariant switch
        {
            EliteMobVariant.None => EliteMobVariant.None,
            EliteMobVariant.Brute => EliteMobVariant.Brute,
            EliteMobVariant.Hunter => EliteMobVariant.Hunter,
            EliteMobVariant.Warden => EliteMobVariant.Warden,
            _ => EliteMobVariant.None
        };
    }

    private static int ScaleCount(int count, float scale)
    {
        if (count <= 0)
        {
            return 0;
        }

        return Math.Max(1, (int)MathF.Ceiling(count * Math.Max(1f, scale)));
    }

    public static void RollChestLoot(int seed, BlockCoord position, BiomeId biome, int depth, List<LootDrop> target)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.Clear();

        var rand = new Random(Hash(seed, position.X, position.Y, position.Z, (int)biome, depth));
        int rolls = 2 + rand.Next(3);
        for (int i = 0; i < rolls; i++)
        {
            AddChestRoll(rand, biome, depth, target);
        }
    }

    public static void RollChestLoot(int seed, BlockCoord position, BiomeId biome, int depth, PoiLootKind poiKind, List<LootDrop> target)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (poiKind == PoiLootKind.Generic)
        {
            RollChestLoot(seed, position, biome, depth, target);
            return;
        }

        target.Clear();

        var rand = new Random(Hash(seed, position.X, position.Y, position.Z, (int)biome, depth, (int)poiKind));
        int rolls = GetChestRollCount(rand, poiKind);
        for (int i = 0; i < rolls; i++)
        {
            AddChestRoll(rand, biome, depth, poiKind, target);
        }
    }

    private static int GetChestRollCount(Random rand, PoiLootKind poiKind)
    {
        return poiKind switch
        {
            PoiLootKind.Camp => 2 + rand.Next(2),
            PoiLootKind.Watchtower => 2 + rand.Next(3),
            PoiLootKind.BuriedCache => 2 + rand.Next(2),
            PoiLootKind.CaveCache => 2 + rand.Next(3),
            PoiLootKind.Ruin => 3 + rand.Next(2),
            PoiLootKind.MineShaft => 3 + rand.Next(2),
            _ => 2 + rand.Next(3)
        };
    }

    private static void AddChestRoll(Random rand, BiomeId biome, int depth, List<LootDrop> target)
    {
        int roll = rand.Next(100);
        bool deep = depth < 40;
        bool surface = depth > 84;

        if (deep)
        {
            if (roll < 24)
            {
                target.Add(new LootDrop(BlockId.Coal, 1 + rand.Next(3)));
                return;
            }

            if (roll < 42)
            {
                target.Add(new LootDrop(BlockId.RawIron, 1 + rand.Next(2)));
                return;
            }

            if (roll < 55)
            {
                target.Add(new LootDrop(BlockId.RawGold, 1 + rand.Next(2)));
                return;
            }

            if (roll < 65)
            {
                target.Add(new LootDrop(BlockId.IronIngot, 1 + rand.Next(2)));
                return;
            }

            if (roll < 72)
            {
                target.Add(new LootDrop(BlockId.GoldIngot, 1));
                return;
            }

            if (roll < 78)
            {
                target.Add(new LootDrop(BlockId.Bread, 1 + rand.Next(2)));
                return;
            }

            if (roll < 84)
            {
                target.Add(new LootDrop(BlockId.Torch, 4 + rand.Next(4)));
                return;
            }
        }

        if (surface)
        {
            if (roll < 28)
            {
                target.Add(new LootDrop(BlockId.Stick, 2 + rand.Next(4)));
                return;
            }

            if (roll < 48)
            {
                target.Add(new LootDrop(BlockId.Planks, 4 + rand.Next(4)));
                return;
            }

            if (roll < 62)
            {
                target.Add(new LootDrop(BlockId.Torch, 4 + rand.Next(4)));
                return;
            }

            if (roll < 74)
            {
                target.Add(new LootDrop(BlockId.Bread, 1 + rand.Next(2)));
                return;
            }

            if (roll < 82)
            {
                target.Add(new LootDrop(BlockId.CookedChicken, 1 + rand.Next(2)));
                return;
            }

            if (roll < 88)
            {
                target.Add(new LootDrop(BlockId.CookedBeef, 1));
                return;
            }
        }

        switch (biome)
        {
            case BiomeId.Desert:
                if (roll < 25)
                {
                    target.Add(new LootDrop(BlockId.Apple, 1 + rand.Next(2)));
                }
                else if (roll < 50)
                {
                    target.Add(new LootDrop(BlockId.Bread, 1 + rand.Next(2)));
                }
                else if (roll < 70)
                {
                    target.Add(new LootDrop(BlockId.RawChicken, 1 + rand.Next(2)));
                }
                else if (roll < 85)
                {
                    target.Add(new LootDrop(BlockId.CookedMutton, 1));
                }
                else
                {
                    target.Add(new LootDrop(BlockId.GoldIngot, 1));
                }
                break;
            case BiomeId.Mountains:
            case BiomeId.Taiga:
            case BiomeId.Tundra:
                if (roll < 28)
                {
                    target.Add(new LootDrop(BlockId.Coal, 1 + rand.Next(3)));
                }
                else if (roll < 50)
                {
                    target.Add(new LootDrop(BlockId.RawBeef, 1 + rand.Next(2)));
                }
                else if (roll < 68)
                {
                    target.Add(new LootDrop(BlockId.CookedBeef, 1));
                }
                else if (roll < 82)
                {
                    target.Add(new LootDrop(BlockId.CookedChicken, 1 + rand.Next(2)));
                }
                else
                {
                    target.Add(new LootDrop(BlockId.StonePickaxe, 1));
                }
                break;
            case BiomeId.Swamp:
                if (roll < 24)
                {
                    target.Add(new LootDrop(BlockId.String, 2 + rand.Next(3)));
                }
                else if (roll < 48)
                {
                    target.Add(new LootDrop(BlockId.Apple, 1 + rand.Next(2)));
                }
                else if (roll < 72)
                {
                    target.Add(new LootDrop(BlockId.CookedChicken, 1 + rand.Next(2)));
                }
                else
                {
                    target.Add(new LootDrop(BlockId.Bread, 1 + rand.Next(2)));
                }
                break;
            default:
                if (roll < 28)
                {
                    target.Add(new LootDrop(BlockId.Stick, 2 + rand.Next(4)));
                }
                else if (roll < 46)
                {
                    target.Add(new LootDrop(BlockId.Bread, 1 + rand.Next(2)));
                }
                else if (roll < 64)
                {
                    target.Add(new LootDrop(BlockId.RawMutton, 1 + rand.Next(2)));
                }
                else if (roll < 80)
                {
                    target.Add(new LootDrop(BlockId.CookedChicken, 1 + rand.Next(2)));
                }
                else if (roll < 90)
                {
                    target.Add(new LootDrop(BlockId.Apple, 1 + rand.Next(2)));
                }
                else
                {
                    target.Add(new LootDrop(BlockId.CookedBeef, 1));
                }
                break;
        }
    }

    private static void AddChestRoll(Random rand, BiomeId biome, int depth, PoiLootKind poiKind, List<LootDrop> target)
    {
        switch (poiKind)
        {
            case PoiLootKind.Camp:
                AddCampChestRoll(rand, target);
                return;
            case PoiLootKind.Watchtower:
                AddWatchtowerChestRoll(rand, target);
                return;
            case PoiLootKind.BuriedCache:
                AddBuriedCacheChestRoll(rand, target);
                return;
            case PoiLootKind.CaveCache:
                AddCaveCacheChestRoll(rand, target);
                return;
            case PoiLootKind.Ruin:
                AddRuinChestRoll(rand, target);
                return;
            case PoiLootKind.MineShaft:
                AddMineShaftChestRoll(rand, target);
                return;
            default:
                AddChestRoll(rand, biome, depth, target);
                return;
        }
    }

    private static void AddCampChestRoll(Random rand, List<LootDrop> target)
    {
        int roll = rand.Next(100);
        if (roll < 22)
        {
            target.Add(new LootDrop(BlockId.Stick, 2 + rand.Next(4)));
        }
        else if (roll < 40)
        {
            target.Add(new LootDrop(BlockId.Planks, 4 + rand.Next(4)));
        }
        else if (roll < 56)
        {
            target.Add(new LootDrop(BlockId.Torch, 4 + rand.Next(4)));
        }
        else if (roll < 72)
        {
            target.Add(new LootDrop(BlockId.Bread, 1 + rand.Next(2)));
        }
        else if (roll < 84)
        {
            target.Add(new LootDrop(BlockId.WoodenPickaxe, 1));
        }
        else if (roll < 94)
        {
            target.Add(new LootDrop(BlockId.WoodenAxe, 1));
        }
        else
        {
            target.Add(new LootDrop(BlockId.StonePickaxe, 1));
        }
    }

    private static void AddWatchtowerChestRoll(Random rand, List<LootDrop> target)
    {
        int roll = rand.Next(100);
        if (roll < 18)
        {
            target.Add(new LootDrop(BlockId.Torch, 4 + rand.Next(4)));
        }
        else if (roll < 36)
        {
            target.Add(new LootDrop(BlockId.Stick, 2 + rand.Next(4)));
        }
        else if (roll < 52)
        {
            target.Add(new LootDrop(BlockId.Planks, 4 + rand.Next(4)));
        }
        else if (roll < 66)
        {
            target.Add(new LootDrop(BlockId.Bread, 1 + rand.Next(2)));
        }
        else if (roll < 78)
        {
            target.Add(new LootDrop(BlockId.StoneAxe, 1));
        }
        else if (roll < 90)
        {
            target.Add(new LootDrop(BlockId.StonePickaxe, 1));
        }
        else
        {
            target.Add(new LootDrop(BlockId.IronPickaxe, 1));
        }
    }

    private static void AddBuriedCacheChestRoll(Random rand, List<LootDrop> target)
    {
        int roll = rand.Next(100);
        if (roll < 20)
        {
            target.Add(new LootDrop(BlockId.GoldIngot, 1));
        }
        else if (roll < 38)
        {
            target.Add(new LootDrop(BlockId.RawGold, 1 + rand.Next(2)));
        }
        else if (roll < 54)
        {
            target.Add(new LootDrop(BlockId.Bread, 1 + rand.Next(2)));
        }
        else if (roll < 68)
        {
            target.Add(new LootDrop(BlockId.Apple, 1 + rand.Next(2)));
        }
        else if (roll < 82)
        {
            target.Add(new LootDrop(BlockId.IronIngot, 1 + rand.Next(2)));
        }
        else if (roll < 92)
        {
            target.Add(new LootDrop(BlockId.Torch, 2 + rand.Next(3)));
        }
        else
        {
            target.Add(new LootDrop(BlockId.StonePickaxe, 1));
        }
    }

    private static void AddCaveCacheChestRoll(Random rand, List<LootDrop> target)
    {
        int roll = rand.Next(100);
        if (roll < 22)
        {
            target.Add(new LootDrop(BlockId.Coal, 2 + rand.Next(3)));
        }
        else if (roll < 40)
        {
            target.Add(new LootDrop(BlockId.Torch, 4 + rand.Next(4)));
        }
        else if (roll < 56)
        {
            target.Add(new LootDrop(BlockId.RawIron, 1 + rand.Next(2)));
        }
        else if (roll < 68)
        {
            target.Add(new LootDrop(BlockId.Bread, 1 + rand.Next(2)));
        }
        else if (roll < 80)
        {
            target.Add(new LootDrop(BlockId.StonePickaxe, 1));
        }
        else if (roll < 90)
        {
            target.Add(new LootDrop(BlockId.WoodenPickaxe, 1));
        }
        else
        {
            target.Add(new LootDrop(BlockId.IronIngot, 1));
        }
    }

    private static void AddRuinChestRoll(Random rand, List<LootDrop> target)
    {
        int roll = rand.Next(100);
        if (roll < 18)
        {
            target.Add(new LootDrop(BlockId.StonePickaxe, 1));
        }
        else if (roll < 32)
        {
            target.Add(new LootDrop(BlockId.WoodenPickaxe, 1));
        }
        else if (roll < 46)
        {
            target.Add(new LootDrop(BlockId.Torch, 4 + rand.Next(4)));
        }
        else if (roll < 60)
        {
            target.Add(new LootDrop(BlockId.Bread, 1 + rand.Next(2)));
        }
        else if (roll < 74)
        {
            target.Add(new LootDrop(BlockId.IronIngot, 1 + rand.Next(2)));
        }
        else if (roll < 86)
        {
            target.Add(new LootDrop(BlockId.GoldIngot, 1));
        }
        else if (roll < 94)
        {
            target.Add(new LootDrop(BlockId.WoodenAxe, 1));
        }
        else
        {
            target.Add(new LootDrop(BlockId.CookedBeef, 1));
        }
    }

    private static void AddMineShaftChestRoll(Random rand, List<LootDrop> target)
    {
        int roll = rand.Next(100);
        if (roll < 20)
        {
            target.Add(new LootDrop(BlockId.Coal, 2 + rand.Next(4)));
        }
        else if (roll < 38)
        {
            target.Add(new LootDrop(BlockId.Torch, 4 + rand.Next(4)));
        }
        else if (roll < 54)
        {
            target.Add(new LootDrop(BlockId.Stick, 2 + rand.Next(4)));
        }
        else if (roll < 68)
        {
            target.Add(new LootDrop(BlockId.WoodenPickaxe, 1));
        }
        else if (roll < 80)
        {
            target.Add(new LootDrop(BlockId.StonePickaxe, 1));
        }
        else if (roll < 90)
        {
            target.Add(new LootDrop(BlockId.RawIron, 1 + rand.Next(2)));
        }
        else
        {
            target.Add(new LootDrop(BlockId.IronIngot, 1));
        }
    }

    private static void AddIf(Random rand, float chance, List<LootDrop> target, BlockId item, int count)
    {
        if (rand.NextDouble() <= chance)
        {
            target.Add(new LootDrop(item, count));
        }
    }

    private static int Hash(params int[] values)
    {
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < values.Length; i++)
            {
                hash = (hash * 31) ^ values[i];
            }

            return hash;
        }
    }
}
