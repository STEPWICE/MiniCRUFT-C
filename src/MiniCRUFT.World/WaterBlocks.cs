namespace MiniCRUFT.World;

public static class WaterBlocks
{
    public const int MaxLevel = LiquidBlocks.MaxLevel;
    public static BlockId SourceId => BlockId.Water;

    public static bool IsWater(BlockId id)
    {
        return LiquidBlocks.IsWater(id);
    }

    public static bool IsSource(BlockId id) => LiquidBlocks.IsSource(id, LiquidKind.Water);

    public static int GetLevel(BlockId id)
    {
        return LiquidBlocks.GetLevel(id);
    }

    public static BlockId FromLevel(int level)
    {
        return LiquidBlocks.FromLevel(LiquidKind.Water, level);
    }
}
