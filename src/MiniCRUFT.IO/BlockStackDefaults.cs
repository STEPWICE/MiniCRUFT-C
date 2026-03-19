using MiniCRUFT.World;

namespace MiniCRUFT.IO;

public static class BlockStackDefaults
{
    public static int GetDefaultCount(BlockId block)
    {
        return block switch
        {
            BlockId.Air => 0,
            BlockId.Planks => 13,
            BlockId.Stick => 8,
            BlockId.Coal => 4,
            BlockId.Charcoal => 4,
            BlockId.Torch => 16,
            BlockId.Tnt => 4,
            BlockId.CraftingTable => 1,
            BlockId.Furnace => 1,
            BlockId.Chest => 1,
            BlockId.Diamond => 1,
            BlockId.RawIron => 1,
            BlockId.IronIngot => 1,
            BlockId.RawGold => 1,
            BlockId.GoldIngot => 1,
            BlockId.MobTrophy => 1,
            _ => 1
        };
    }

    public static int GetMaxCount(BlockId block)
    {
        return block switch
        {
            BlockId.Air => 0,
            BlockId.WoodenPickaxe => 1,
            BlockId.StonePickaxe => 1,
            BlockId.IronPickaxe => 1,
            BlockId.DiamondPickaxe => 1,
            BlockId.WoodenAxe => 1,
            BlockId.StoneAxe => 1,
            BlockId.IronAxe => 1,
            BlockId.DiamondAxe => 1,
            BlockId.WoodenShovel => 1,
            BlockId.StoneShovel => 1,
            BlockId.IronShovel => 1,
            BlockId.DiamondShovel => 1,
            BlockId.WoodenSword => 1,
            BlockId.StoneSword => 1,
            BlockId.IronSword => 1,
            BlockId.DiamondSword => 1,
            BlockId.MobTrophy => 64,
            _ => 64
        };
    }

    public static int[] CreateDefaultCounts(BlockId[] hotbar)
    {
        var counts = new int[hotbar.Length];
        for (int i = 0; i < hotbar.Length; i++)
        {
            counts[i] = GetDefaultCount(hotbar[i]);
        }

        return counts;
    }
}
