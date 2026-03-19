using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public enum ToolType
{
    None = 0,
    Pickaxe = 1,
    Axe = 2,
    Shovel = 3,
    Sword = 4
}

public enum ToolTier
{
    None = 0,
    Wood = 1,
    Stone = 2,
    Iron = 3,
    Diamond = 4
}

public readonly struct ToolProfile
{
    public ToolType Type { get; }
    public ToolTier Tier { get; }
    public int AttackBonus { get; }

    public ToolProfile(ToolType type, ToolTier tier, int attackBonus)
    {
        Type = type;
        Tier = tier;
        AttackBonus = attackBonus;
    }

    public bool IsTool => Type != ToolType.None;
}

public static class ToolCatalog
{
    public static ToolProfile GetProfile(BlockId item)
    {
        return item switch
        {
            BlockId.WoodenPickaxe => new ToolProfile(ToolType.Pickaxe, ToolTier.Wood, 1),
            BlockId.StonePickaxe => new ToolProfile(ToolType.Pickaxe, ToolTier.Stone, 2),
            BlockId.IronPickaxe => new ToolProfile(ToolType.Pickaxe, ToolTier.Iron, 3),
            BlockId.DiamondPickaxe => new ToolProfile(ToolType.Pickaxe, ToolTier.Diamond, 4),
            BlockId.WoodenAxe => new ToolProfile(ToolType.Axe, ToolTier.Wood, 2),
            BlockId.StoneAxe => new ToolProfile(ToolType.Axe, ToolTier.Stone, 3),
            BlockId.IronAxe => new ToolProfile(ToolType.Axe, ToolTier.Iron, 4),
            BlockId.DiamondAxe => new ToolProfile(ToolType.Axe, ToolTier.Diamond, 5),
            BlockId.WoodenShovel => new ToolProfile(ToolType.Shovel, ToolTier.Wood, 1),
            BlockId.StoneShovel => new ToolProfile(ToolType.Shovel, ToolTier.Stone, 2),
            BlockId.IronShovel => new ToolProfile(ToolType.Shovel, ToolTier.Iron, 3),
            BlockId.DiamondShovel => new ToolProfile(ToolType.Shovel, ToolTier.Diamond, 4),
            BlockId.WoodenSword => new ToolProfile(ToolType.Sword, ToolTier.Wood, 3),
            BlockId.StoneSword => new ToolProfile(ToolType.Sword, ToolTier.Stone, 4),
            BlockId.IronSword => new ToolProfile(ToolType.Sword, ToolTier.Iron, 5),
            BlockId.DiamondSword => new ToolProfile(ToolType.Sword, ToolTier.Diamond, 6),
            _ => new ToolProfile(ToolType.None, ToolTier.None, 0)
        };
    }

    public static bool IsTool(BlockId item) => GetProfile(item).IsTool;

    public static bool IsPickaxe(BlockId item) => GetProfile(item).Type == ToolType.Pickaxe;

    public static bool IsAxe(BlockId item) => GetProfile(item).Type == ToolType.Axe;

    public static bool IsShovel(BlockId item) => GetProfile(item).Type == ToolType.Shovel;

    public static bool IsSword(BlockId item) => GetProfile(item).Type == ToolType.Sword;

    public static ToolTier GetTier(BlockId item) => GetProfile(item).Tier;

    public static int GetCombatBonus(BlockId item) => GetProfile(item).AttackBonus;
}
