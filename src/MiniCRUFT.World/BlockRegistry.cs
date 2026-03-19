using System.Collections.Generic;

namespace MiniCRUFT.World;

public static class BlockRegistry
{
    private static readonly Dictionary<BlockId, BlockDefinition> Definitions = new();
    private static readonly object Sync = new();
    private static volatile bool _initialized;

    public static IReadOnlyDictionary<BlockId, BlockDefinition> All => Definitions;

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        lock (Sync)
        {
            if (_initialized)
            {
                return;
            }

            Register(new BlockDefinition(BlockId.Air, "air", false, true, "missing", "missing", "missing", blocksSkyLight: false, renderMode: RenderMode.Opaque));
            Register(new BlockDefinition(BlockId.Grass, "grass", true, false, "grass_block_top", "dirt", "grass_block_side", tintType: TintType.Grass, renderMode: RenderMode.Opaque));
            Register(new BlockDefinition(BlockId.Dirt, "dirt", true, false, "dirt", "dirt", "dirt"));
            Register(new BlockDefinition(BlockId.Stone, "stone", true, false, "stone", "stone", "stone"));
            Register(new BlockDefinition(BlockId.Sand, "sand", true, false, "sand", "sand", "sand"));
            Register(new BlockDefinition(BlockId.Gravel, "gravel", true, false, "gravel", "gravel", "gravel"));
            Register(new BlockDefinition(BlockId.Wood, "wood", true, false, "oak_log_top", "oak_log_top", "oak_log"));
            Register(new BlockDefinition(BlockId.Leaves, "leaves", true, true, "oak_leaves", "oak_leaves", "oak_leaves", blocksSkyLight: false, tintType: TintType.Foliage, renderMode: RenderMode.Cutout, isFoliage: true));
            Register(new BlockDefinition(BlockId.Water, "water", false, true, "water_still", "water_still", "water_flow", blocksSkyLight: false, renderMode: RenderMode.Transparent));
            Register(new BlockDefinition(BlockId.CoalOre, "coal_ore", true, false, "coal_ore", "coal_ore", "coal_ore"));
            Register(new BlockDefinition(BlockId.IronOre, "iron_ore", true, false, "iron_ore", "iron_ore", "iron_ore"));
            Register(new BlockDefinition(BlockId.GoldOre, "gold_ore", true, false, "gold_ore", "gold_ore", "gold_ore"));
            Register(new BlockDefinition(BlockId.DiamondOre, "diamond_ore", true, false, "diamond_ore", "diamond_ore", "diamond_ore"));
            Register(new BlockDefinition(BlockId.Bedrock, "bedrock", true, false, "bedrock", "bedrock", "bedrock"));
            Register(new BlockDefinition(BlockId.Planks, "planks", true, false, "oak_planks", "oak_planks", "oak_planks"));
            Register(new BlockDefinition(BlockId.Glass, "glass", true, true, "glass", "glass", "glass", renderMode: RenderMode.Cutout));
            Register(new BlockDefinition(BlockId.Torch, "torch", false, true, "torch", "torch", "torch", 14, renderMode: RenderMode.Torch));
            Register(new BlockDefinition(BlockId.TorchWallNorth, "torch_wall_north", false, true, "torch", "torch", "torch", 14, renderMode: RenderMode.Torch));
            Register(new BlockDefinition(BlockId.TorchWallSouth, "torch_wall_south", false, true, "torch", "torch", "torch", 14, renderMode: RenderMode.Torch));
            Register(new BlockDefinition(BlockId.TorchWallWest, "torch_wall_west", false, true, "torch", "torch", "torch", 14, renderMode: RenderMode.Torch));
            Register(new BlockDefinition(BlockId.TorchWallEast, "torch_wall_east", false, true, "torch", "torch", "torch", 14, renderMode: RenderMode.Torch));
            Register(new BlockDefinition(BlockId.Water1, "water_1", false, true, "water_still", "water_still", "water_flow", blocksSkyLight: false, renderMode: RenderMode.Transparent));
            Register(new BlockDefinition(BlockId.Water2, "water_2", false, true, "water_still", "water_still", "water_flow", blocksSkyLight: false, renderMode: RenderMode.Transparent));
            Register(new BlockDefinition(BlockId.Water3, "water_3", false, true, "water_still", "water_still", "water_flow", blocksSkyLight: false, renderMode: RenderMode.Transparent));
            Register(new BlockDefinition(BlockId.Water4, "water_4", false, true, "water_still", "water_still", "water_flow", blocksSkyLight: false, renderMode: RenderMode.Transparent));
            Register(new BlockDefinition(BlockId.Water5, "water_5", false, true, "water_still", "water_still", "water_flow", blocksSkyLight: false, renderMode: RenderMode.Transparent));
            Register(new BlockDefinition(BlockId.Water6, "water_6", false, true, "water_still", "water_still", "water_flow", blocksSkyLight: false, renderMode: RenderMode.Transparent));
            Register(new BlockDefinition(BlockId.Water7, "water_7", false, true, "water_still", "water_still", "water_flow", blocksSkyLight: false, renderMode: RenderMode.Transparent));
            Register(new BlockDefinition(BlockId.Cobblestone, "cobblestone", true, false, "cobblestone", "cobblestone", "cobblestone"));
            Register(new BlockDefinition(BlockId.Obsidian, "obsidian", true, false, "obsidian", "obsidian", "obsidian"));
            Register(new BlockDefinition(BlockId.Lava, "lava", false, true, "lava_still", "lava_still", "lava_flow", 15, blocksSkyLight: false, renderMode: RenderMode.Transparent));
            Register(new BlockDefinition(BlockId.Lava1, "lava_1", false, true, "lava_still", "lava_still", "lava_flow", 15, blocksSkyLight: false, renderMode: RenderMode.Transparent));
            Register(new BlockDefinition(BlockId.Lava2, "lava_2", false, true, "lava_still", "lava_still", "lava_flow", 15, blocksSkyLight: false, renderMode: RenderMode.Transparent));
            Register(new BlockDefinition(BlockId.Lava3, "lava_3", false, true, "lava_still", "lava_still", "lava_flow", 15, blocksSkyLight: false, renderMode: RenderMode.Transparent));
            Register(new BlockDefinition(BlockId.Lava4, "lava_4", false, true, "lava_still", "lava_still", "lava_flow", 15, blocksSkyLight: false, renderMode: RenderMode.Transparent));
            Register(new BlockDefinition(BlockId.Lava5, "lava_5", false, true, "lava_still", "lava_still", "lava_flow", 15, blocksSkyLight: false, renderMode: RenderMode.Transparent));
            Register(new BlockDefinition(BlockId.Lava6, "lava_6", false, true, "lava_still", "lava_still", "lava_flow", 15, blocksSkyLight: false, renderMode: RenderMode.Transparent));
            Register(new BlockDefinition(BlockId.Lava7, "lava_7", false, true, "lava_still", "lava_still", "lava_flow", 15, blocksSkyLight: false, renderMode: RenderMode.Transparent));
            Register(new BlockDefinition(BlockId.Tnt, "tnt", true, false, "tnt_top", "tnt_bottom", "tnt_side"));
            Register(new BlockDefinition(BlockId.CraftingTable, "crafting_table", true, false, "crafting_table_top", "crafting_table_top", "crafting_table_side"));
            Register(new BlockDefinition(BlockId.Furnace, "furnace", true, false, "furnace_top", "furnace_top", "furnace_side"));
            Register(new BlockDefinition(BlockId.Chest, "chest", true, false, "chest_top", "chest_top", "chest_side"));
            Register(new BlockDefinition(BlockId.Stick, "stick", false, true, "stick", "stick", "stick", isPlaceable: false));
            Register(new BlockDefinition(BlockId.Coal, "coal", false, true, "coal", "coal", "coal", isPlaceable: false));
            Register(new BlockDefinition(BlockId.Charcoal, "charcoal", false, true, "charcoal", "charcoal", "charcoal", isPlaceable: false));
            Register(new BlockDefinition(BlockId.Diamond, "diamond", false, true, "diamond", "diamond", "diamond", isPlaceable: false));
            Register(new BlockDefinition(BlockId.RawIron, "raw_iron", false, true, "raw_iron", "raw_iron", "raw_iron", isPlaceable: false));
            Register(new BlockDefinition(BlockId.IronIngot, "iron_ingot", false, true, "iron_ingot", "iron_ingot", "iron_ingot", isPlaceable: false));
            Register(new BlockDefinition(BlockId.RawGold, "raw_gold", false, true, "raw_gold", "raw_gold", "raw_gold", isPlaceable: false));
            Register(new BlockDefinition(BlockId.GoldIngot, "gold_ingot", false, true, "gold_ingot", "gold_ingot", "gold_ingot", isPlaceable: false));
            Register(new BlockDefinition(BlockId.WoodenPickaxe, "wooden_pickaxe", false, true, "wooden_pickaxe", "wooden_pickaxe", "wooden_pickaxe", isPlaceable: false));
            Register(new BlockDefinition(BlockId.StonePickaxe, "stone_pickaxe", false, true, "stone_pickaxe", "stone_pickaxe", "stone_pickaxe", isPlaceable: false));
            Register(new BlockDefinition(BlockId.IronPickaxe, "iron_pickaxe", false, true, "iron_pickaxe", "iron_pickaxe", "iron_pickaxe", isPlaceable: false));
            Register(new BlockDefinition(BlockId.DiamondPickaxe, "diamond_pickaxe", false, true, "diamond_pickaxe", "diamond_pickaxe", "diamond_pickaxe", isPlaceable: false));
            Register(new BlockDefinition(BlockId.WoodenAxe, "wooden_axe", false, true, "wooden_axe", "wooden_axe", "wooden_axe", isPlaceable: false));
            Register(new BlockDefinition(BlockId.StoneAxe, "stone_axe", false, true, "stone_axe", "stone_axe", "stone_axe", isPlaceable: false));
            Register(new BlockDefinition(BlockId.IronAxe, "iron_axe", false, true, "iron_axe", "iron_axe", "iron_axe", isPlaceable: false));
            Register(new BlockDefinition(BlockId.DiamondAxe, "diamond_axe", false, true, "diamond_axe", "diamond_axe", "diamond_axe", isPlaceable: false));
            Register(new BlockDefinition(BlockId.WoodenShovel, "wooden_shovel", false, true, "wooden_shovel", "wooden_shovel", "wooden_shovel", isPlaceable: false));
            Register(new BlockDefinition(BlockId.StoneShovel, "stone_shovel", false, true, "stone_shovel", "stone_shovel", "stone_shovel", isPlaceable: false));
            Register(new BlockDefinition(BlockId.IronShovel, "iron_shovel", false, true, "iron_shovel", "iron_shovel", "iron_shovel", isPlaceable: false));
            Register(new BlockDefinition(BlockId.DiamondShovel, "diamond_shovel", false, true, "diamond_shovel", "diamond_shovel", "diamond_shovel", isPlaceable: false));
            Register(new BlockDefinition(BlockId.WoodenSword, "wooden_sword", false, true, "wooden_sword", "wooden_sword", "wooden_sword", isPlaceable: false));
            Register(new BlockDefinition(BlockId.StoneSword, "stone_sword", false, true, "stone_sword", "stone_sword", "stone_sword", isPlaceable: false));
            Register(new BlockDefinition(BlockId.IronSword, "iron_sword", false, true, "iron_sword", "iron_sword", "iron_sword", isPlaceable: false));
            Register(new BlockDefinition(BlockId.DiamondSword, "diamond_sword", false, true, "diamond_sword", "diamond_sword", "diamond_sword", isPlaceable: false));
            Register(new BlockDefinition(BlockId.RottenFlesh, "rotten_flesh", false, true, "rotten_flesh", "rotten_flesh", "rotten_flesh", isPlaceable: false));
            Register(new BlockDefinition(BlockId.Gunpowder, "gunpowder", false, true, "gunpowder", "gunpowder", "gunpowder", isPlaceable: false));
            Register(new BlockDefinition(BlockId.Leather, "leather", false, true, "leather", "leather", "leather", isPlaceable: false));
            Register(new BlockDefinition(BlockId.Feather, "feather", false, true, "feather", "feather", "feather", isPlaceable: false));
            Register(new BlockDefinition(BlockId.String, "string", false, true, "string", "string", "string", isPlaceable: false));
            Register(new BlockDefinition(BlockId.Bread, "bread", false, true, "bread", "bread", "bread", isPlaceable: false));
            Register(new BlockDefinition(BlockId.Apple, "apple", false, true, "apple", "apple", "apple", isPlaceable: false));
            Register(new BlockDefinition(BlockId.RawBeef, "raw_beef", false, true, "beef", "beef", "beef", isPlaceable: false));
            Register(new BlockDefinition(BlockId.CookedBeef, "cooked_beef", false, true, "cooked_beef", "cooked_beef", "cooked_beef", isPlaceable: false));
            Register(new BlockDefinition(BlockId.RawChicken, "raw_chicken", false, true, "chicken_raw", "chicken_raw", "chicken_raw", isPlaceable: false));
            Register(new BlockDefinition(BlockId.CookedChicken, "cooked_chicken", false, true, "chicken_cooked", "chicken_cooked", "chicken_cooked", isPlaceable: false));
            Register(new BlockDefinition(BlockId.RawMutton, "raw_mutton", false, true, "mutton_raw", "mutton_raw", "mutton_raw", isPlaceable: false));
            Register(new BlockDefinition(BlockId.CookedMutton, "cooked_mutton", false, true, "mutton_cooked", "mutton_cooked", "mutton_cooked", isPlaceable: false));
            Register(new BlockDefinition(BlockId.MobTrophy, "mob_trophy", false, true, "totem_of_undying", "totem_of_undying", "totem_of_undying", isPlaceable: false));
            Register(new BlockDefinition(BlockId.Flower, "flower", false, true, "dandelion", "dandelion", "dandelion", renderMode: RenderMode.Cross));
            Register(new BlockDefinition(BlockId.TallGrass, "tall_grass", false, true, "short_grass", "short_grass", "short_grass", tintType: TintType.Grass, renderMode: RenderMode.Cross, isFoliage: true));
            Register(new BlockDefinition(BlockId.Snow, "snow", true, false, "snow", "snow", "snow"));
            Register(new BlockDefinition(BlockId.Clay, "clay", true, false, "clay", "clay", "clay"));
            Register(new BlockDefinition(BlockId.BirchWood, "birch_wood", true, false, "birch_log_top", "birch_log_top", "birch_log"));
            Register(new BlockDefinition(BlockId.BirchLeaves, "birch_leaves", true, true, "birch_leaves", "birch_leaves", "birch_leaves", blocksSkyLight: false, tintType: TintType.Foliage, renderMode: RenderMode.Cutout, isFoliage: true));
            Register(new BlockDefinition(BlockId.SpruceWood, "spruce_wood", true, false, "spruce_log_top", "spruce_log_top", "spruce_log"));
            Register(new BlockDefinition(BlockId.SpruceLeaves, "spruce_leaves", true, true, "spruce_leaves", "spruce_leaves", "spruce_leaves", blocksSkyLight: false, tintType: TintType.Foliage, renderMode: RenderMode.Cutout, isFoliage: true));
            Register(new BlockDefinition(BlockId.Cactus, "cactus", true, true, "cactus_top", "cactus_bottom", "cactus_side", blocksSkyLight: true, renderMode: RenderMode.Cutout));
            Register(new BlockDefinition(BlockId.DeadBush, "dead_bush", false, true, "dead_bush", "dead_bush", "dead_bush", renderMode: RenderMode.Cross));
            Register(new BlockDefinition(BlockId.SugarCane, "sugar_cane", false, true, "sugar_cane", "sugar_cane", "sugar_cane", renderMode: RenderMode.Cross));
            Register(new BlockDefinition(BlockId.Fire, "fire", false, true, "fire_0", "fire_0", "fire_0", 15, blocksSkyLight: false, renderMode: RenderMode.Cross, isPlaceable: false));

            _initialized = true;
        }
    }

    private static void Register(BlockDefinition definition)
    {
        Definitions[definition.Id] = definition;
    }

    public static BlockDefinition Get(BlockId id)
    {
        if (!_initialized)
        {
            Initialize();
        }

        return Definitions[id];
    }
}
