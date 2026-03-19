using System;

namespace MiniCRUFT.World;

public static class LiquidBlocks
{
    public const int MaxLevel = 7;

    public static bool IsLiquid(BlockId id)
    {
        return IsWater(id) || IsLava(id);
    }

    public static bool IsWater(BlockId id)
    {
        return id is BlockId.Water or BlockId.Water1 or BlockId.Water2 or BlockId.Water3 or
            BlockId.Water4 or BlockId.Water5 or BlockId.Water6 or BlockId.Water7;
    }

    public static bool IsLava(BlockId id)
    {
        return id is BlockId.Lava or BlockId.Lava1 or BlockId.Lava2 or BlockId.Lava3 or
            BlockId.Lava4 or BlockId.Lava5 or BlockId.Lava6 or BlockId.Lava7;
    }

    public static BlockId GetSourceId(LiquidKind kind)
    {
        return kind == LiquidKind.Water ? BlockId.Water : BlockId.Lava;
    }

    public static bool IsSource(BlockId id)
    {
        return id == BlockId.Water || id == BlockId.Lava;
    }

    public static bool IsSource(BlockId id, LiquidKind kind)
    {
        return id == GetSourceId(kind);
    }

    public static LiquidKind? TryGetKind(BlockId id)
    {
        if (IsWater(id))
        {
            return LiquidKind.Water;
        }

        if (IsLava(id))
        {
            return LiquidKind.Lava;
        }

        return null;
    }

    public static LiquidKind GetKind(BlockId id)
    {
        return TryGetKind(id) ?? throw new ArgumentOutOfRangeException(nameof(id), id, "Block is not a liquid.");
    }

    public static int GetLevel(BlockId id)
    {
        return id switch
        {
            BlockId.Water or BlockId.Lava => 0,
            BlockId.Water1 or BlockId.Lava1 => 1,
            BlockId.Water2 or BlockId.Lava2 => 2,
            BlockId.Water3 or BlockId.Lava3 => 3,
            BlockId.Water4 or BlockId.Lava4 => 4,
            BlockId.Water5 or BlockId.Lava5 => 5,
            BlockId.Water6 or BlockId.Lava6 => 6,
            BlockId.Water7 or BlockId.Lava7 => 7,
            _ => MaxLevel + 1
        };
    }

    public static BlockId FromLevel(LiquidKind kind, int level)
    {
        if (level <= 0)
        {
            return GetSourceId(kind);
        }

        return kind switch
        {
            LiquidKind.Water => level switch
            {
                1 => BlockId.Water1,
                2 => BlockId.Water2,
                3 => BlockId.Water3,
                4 => BlockId.Water4,
                5 => BlockId.Water5,
                6 => BlockId.Water6,
                _ => BlockId.Water7
            },
            _ => level switch
            {
                1 => BlockId.Lava1,
                2 => BlockId.Lava2,
                3 => BlockId.Lava3,
                4 => BlockId.Lava4,
                5 => BlockId.Lava5,
                6 => BlockId.Lava6,
                _ => BlockId.Lava7
            }
        };
    }

    public static bool IsSameKind(BlockId left, BlockId right)
    {
        var leftKind = TryGetKind(left);
        var rightKind = TryGetKind(right);
        return leftKind.HasValue && rightKind.HasValue && leftKind.Value == rightKind.Value;
    }
}
