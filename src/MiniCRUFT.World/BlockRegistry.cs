using System.Collections.Generic;

namespace MiniCRUFT.World;

public static class BlockRegistry
{
    private static readonly Dictionary<BlockId, BlockDefinition> Definitions = new();
    private static bool _initialized;

    public static IReadOnlyDictionary<BlockId, BlockDefinition> All => Definitions;

    public static void Initialize()
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
        Register(new BlockDefinition(BlockId.Leaves, "leaves", true, true, "oak_leaves", "oak_leaves", "oak_leaves", blocksSkyLight: true, tintType: TintType.Foliage, renderMode: RenderMode.Cutout, isFoliage: true));
        Register(new BlockDefinition(BlockId.Water, "water", false, true, "water_still", "water_still", "water_still", blocksSkyLight: true, renderMode: RenderMode.Transparent));
        Register(new BlockDefinition(BlockId.CoalOre, "coal_ore", true, false, "coal_ore", "coal_ore", "coal_ore"));
        Register(new BlockDefinition(BlockId.IronOre, "iron_ore", true, false, "iron_ore", "iron_ore", "iron_ore"));
        Register(new BlockDefinition(BlockId.GoldOre, "gold_ore", true, false, "gold_ore", "gold_ore", "gold_ore"));
        Register(new BlockDefinition(BlockId.DiamondOre, "diamond_ore", true, false, "diamond_ore", "diamond_ore", "diamond_ore"));
        Register(new BlockDefinition(BlockId.Bedrock, "bedrock", true, false, "bedrock", "bedrock", "bedrock"));
        Register(new BlockDefinition(BlockId.Planks, "planks", true, false, "oak_planks", "oak_planks", "oak_planks"));
        Register(new BlockDefinition(BlockId.Glass, "glass", true, true, "glass", "glass", "glass", renderMode: RenderMode.Transparent));
        Register(new BlockDefinition(BlockId.Torch, "torch", false, true, "torch", "torch", "torch", 14, renderMode: RenderMode.Cutout));
        Register(new BlockDefinition(BlockId.Flower, "flower", false, true, "dandelion", "dandelion", "dandelion", renderMode: RenderMode.Cross));
        Register(new BlockDefinition(BlockId.TallGrass, "tall_grass", false, true, "short_grass", "short_grass", "short_grass", tintType: TintType.Grass, renderMode: RenderMode.Cross, isFoliage: true));
        Register(new BlockDefinition(BlockId.Snow, "snow", true, false, "snow", "snow", "snow"));
        Register(new BlockDefinition(BlockId.Clay, "clay", true, false, "clay", "clay", "clay"));
        Register(new BlockDefinition(BlockId.BirchWood, "birch_wood", true, false, "birch_log_top", "birch_log_top", "birch_log"));
        Register(new BlockDefinition(BlockId.BirchLeaves, "birch_leaves", true, true, "birch_leaves", "birch_leaves", "birch_leaves", blocksSkyLight: true, tintType: TintType.Foliage, renderMode: RenderMode.Cutout, isFoliage: true));
        Register(new BlockDefinition(BlockId.SpruceWood, "spruce_wood", true, false, "spruce_log_top", "spruce_log_top", "spruce_log"));
        Register(new BlockDefinition(BlockId.SpruceLeaves, "spruce_leaves", true, true, "spruce_leaves", "spruce_leaves", "spruce_leaves", blocksSkyLight: true, tintType: TintType.Foliage, renderMode: RenderMode.Cutout, isFoliage: true));

        _initialized = true;
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
