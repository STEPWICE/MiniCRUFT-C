using System;
using System.Collections.Generic;
using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public sealed class CraftingRecipe
{
    public string Id { get; }
    public BlockId Output { get; }
    public int OutputCount { get; }
    public int Priority { get; }
    public int Width { get; }
    public int Height { get; }
    public BlockId?[] Pattern { get; }
    public bool AllowMirror { get; }

    public CraftingRecipe(string id, BlockId output, int outputCount, int priority, int width, int height, BlockId?[] pattern, bool allowMirror = true)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Recipe id must be non-empty.", nameof(id));
        }

        if (outputCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(outputCount));
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (priority < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        if (pattern == null)
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        if (pattern.Length != width * height)
        {
            throw new ArgumentException("Pattern length must match width * height.", nameof(pattern));
        }

        Id = id;
        Output = output;
        OutputCount = outputCount;
        Priority = priority;
        Width = width;
        Height = height;
        Pattern = pattern;
        AllowMirror = allowMirror;
    }
}

public static class RecipeCatalog
{
    private static readonly CraftingRecipe[] CraftingRecipesValue =
    [
        Shaped("planks_from_wood", BlockId.Planks, 4, RecipePriority.Planks, 1, 1, [BlockId.Wood]),
        Shaped("planks_from_birch_wood", BlockId.Planks, 4, RecipePriority.Planks, 1, 1, [BlockId.BirchWood]),
        Shaped("planks_from_spruce_wood", BlockId.Planks, 4, RecipePriority.Planks, 1, 1, [BlockId.SpruceWood]),

        Shaped("sticks_from_planks", BlockId.Stick, 4, RecipePriority.Sticks, 1, 2, [BlockId.Planks, BlockId.Planks]),

        Shaped("crafting_table", BlockId.CraftingTable, 1, RecipePriority.CraftingTable, 2, 2, [
            BlockId.Planks, BlockId.Planks,
            BlockId.Planks, BlockId.Planks
        ]),

        Shaped("furnace", BlockId.Furnace, 1, RecipePriority.Furnace, 3, 3, [
            BlockId.Cobblestone, BlockId.Cobblestone, BlockId.Cobblestone,
            BlockId.Cobblestone, null, BlockId.Cobblestone,
            BlockId.Cobblestone, BlockId.Cobblestone, BlockId.Cobblestone
        ]),

        Shaped("chest", BlockId.Chest, 1, RecipePriority.Chest, 3, 3, [
            BlockId.Planks, BlockId.Planks, BlockId.Planks,
            BlockId.Planks, null, BlockId.Planks,
            BlockId.Planks, BlockId.Planks, BlockId.Planks
        ]),

        Shaped("wooden_pickaxe", BlockId.WoodenPickaxe, 1, RecipePriority.WoodenPickaxe, 3, 3, [
            BlockId.Wood, BlockId.Wood, BlockId.Wood,
            null, BlockId.Stick, null,
            null, BlockId.Stick, null
        ]),
        Shaped("stone_pickaxe", BlockId.StonePickaxe, 1, RecipePriority.StonePickaxe, 3, 3, [
            BlockId.Cobblestone, BlockId.Cobblestone, BlockId.Cobblestone,
            null, BlockId.Stick, null,
            null, BlockId.Stick, null
        ]),
        Shaped("iron_pickaxe", BlockId.IronPickaxe, 1, RecipePriority.IronPickaxe, 3, 3, [
            BlockId.IronIngot, BlockId.IronIngot, BlockId.IronIngot,
            null, BlockId.Stick, null,
            null, BlockId.Stick, null
        ]),
        Shaped("diamond_pickaxe", BlockId.DiamondPickaxe, 1, RecipePriority.DiamondPickaxe, 3, 3, [
            BlockId.Diamond, BlockId.Diamond, BlockId.Diamond,
            null, BlockId.Stick, null,
            null, BlockId.Stick, null
        ]),

        Shaped("wooden_axe", BlockId.WoodenAxe, 1, RecipePriority.WoodenAxe, 3, 3, [
            BlockId.Wood, BlockId.Wood, null,
            BlockId.Wood, BlockId.Stick, null,
            null, BlockId.Stick, null
        ]),
        Shaped("stone_axe", BlockId.StoneAxe, 1, RecipePriority.StoneAxe, 3, 3, [
            BlockId.Cobblestone, BlockId.Cobblestone, null,
            BlockId.Cobblestone, BlockId.Stick, null,
            null, BlockId.Stick, null
        ]),
        Shaped("iron_axe", BlockId.IronAxe, 1, RecipePriority.IronAxe, 3, 3, [
            BlockId.IronIngot, BlockId.IronIngot, null,
            BlockId.IronIngot, BlockId.Stick, null,
            null, BlockId.Stick, null
        ]),
        Shaped("diamond_axe", BlockId.DiamondAxe, 1, RecipePriority.DiamondAxe, 3, 3, [
            BlockId.Diamond, BlockId.Diamond, null,
            BlockId.Diamond, BlockId.Stick, null,
            null, BlockId.Stick, null
        ]),

        Shaped("wooden_shovel", BlockId.WoodenShovel, 1, RecipePriority.WoodenShovel, 3, 3, [
            null, BlockId.Wood, null,
            null, BlockId.Stick, null,
            null, BlockId.Stick, null
        ]),
        Shaped("stone_shovel", BlockId.StoneShovel, 1, RecipePriority.StoneShovel, 3, 3, [
            null, BlockId.Cobblestone, null,
            null, BlockId.Stick, null,
            null, BlockId.Stick, null
        ]),
        Shaped("iron_shovel", BlockId.IronShovel, 1, RecipePriority.IronShovel, 3, 3, [
            null, BlockId.IronIngot, null,
            null, BlockId.Stick, null,
            null, BlockId.Stick, null
        ]),
        Shaped("diamond_shovel", BlockId.DiamondShovel, 1, RecipePriority.DiamondShovel, 3, 3, [
            null, BlockId.Diamond, null,
            null, BlockId.Stick, null,
            null, BlockId.Stick, null
        ]),

        Shaped("wooden_sword", BlockId.WoodenSword, 1, RecipePriority.WoodenSword, 3, 3, [
            null, BlockId.Wood, null,
            null, BlockId.Wood, null,
            null, BlockId.Stick, null
        ]),
        Shaped("stone_sword", BlockId.StoneSword, 1, RecipePriority.StoneSword, 3, 3, [
            null, BlockId.Cobblestone, null,
            null, BlockId.Cobblestone, null,
            null, BlockId.Stick, null
        ]),
        Shaped("iron_sword", BlockId.IronSword, 1, RecipePriority.IronSword, 3, 3, [
            null, BlockId.IronIngot, null,
            null, BlockId.IronIngot, null,
            null, BlockId.Stick, null
        ]),
        Shaped("diamond_sword", BlockId.DiamondSword, 1, RecipePriority.DiamondSword, 3, 3, [
            null, BlockId.Diamond, null,
            null, BlockId.Diamond, null,
            null, BlockId.Stick, null
        ]),

        Shaped("torch_from_coal", BlockId.Torch, 4, RecipePriority.Torch, 1, 2, [BlockId.Coal, BlockId.Stick]),
        Shaped("torch_from_charcoal", BlockId.Torch, 4, RecipePriority.Torch, 1, 2, [BlockId.Charcoal, BlockId.Stick]),

        Shaped("tnt", BlockId.Tnt, 1, RecipePriority.Tnt, 3, 3, [
            BlockId.Gunpowder, BlockId.Sand, BlockId.Gunpowder,
            BlockId.Sand, BlockId.Gunpowder, BlockId.Sand,
            BlockId.Gunpowder, BlockId.Sand, BlockId.Gunpowder
        ])
    ];

    public static IReadOnlyList<CraftingRecipe> CraftingRecipes => CraftingRecipesValue;

    private static CraftingRecipe Shaped(string id, BlockId output, int outputCount, int priority, int width, int height, BlockId?[] pattern, bool allowMirror = true)
    {
        return new CraftingRecipe(id, output, outputCount, priority, width, height, pattern, allowMirror);
    }

    private static class RecipePriority
    {
        public const int CraftingTable = 100;
        public const int Sticks = 200;
        public const int WoodenPickaxe = 300;
        public const int StonePickaxe = 400;
        public const int Furnace = 500;
        public const int IronPickaxe = 600;
        public const int DiamondPickaxe = 700;
        public const int Torch = 800;
        public const int WoodenAxe = 900;
        public const int WoodenShovel = 910;
        public const int WoodenSword = 920;
        public const int StoneAxe = 930;
        public const int StoneShovel = 940;
        public const int StoneSword = 950;
        public const int IronAxe = 960;
        public const int IronShovel = 970;
        public const int IronSword = 980;
        public const int DiamondAxe = 990;
        public const int DiamondShovel = 1000;
        public const int DiamondSword = 1010;
        public const int Planks = 1200;
        public const int Chest = 1300;
        public const int Tnt = 1400;
    }
}
