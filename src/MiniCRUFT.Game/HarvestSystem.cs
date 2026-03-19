using System;
using System.Collections.Generic;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public sealed class HarvestRule
{
    public string Id { get; }
    public BlockId Block { get; }
    public BlockId Drop { get; }
    public int Count { get; }
    public ToolType RequiredToolType { get; }
    public ToolTier MinimumTier { get; }

    public HarvestRule(string id, BlockId block, BlockId drop, int count, ToolType requiredToolType, ToolTier minimumTier)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Rule id must be non-empty.", nameof(id));
        }

        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        Id = id;
        Block = block;
        Drop = drop;
        Count = count;
        RequiredToolType = requiredToolType;
        MinimumTier = minimumTier;
    }
}

public sealed class HarvestSystem
{
    private readonly ToolConfig _toolConfig;
    private readonly IReadOnlyList<HarvestRule> _rules;

    public HarvestSystem(ToolConfig? toolConfig = null, IReadOnlyList<HarvestRule>? rules = null)
    {
        _toolConfig = toolConfig ?? new ToolConfig();
        _rules = rules ?? BuildDefaultRules();
    }

    public bool TryHarvest(BlockId block, BlockId tool, out BlockId drop, out int count)
    {
        return TryHarvest(block, tool, out drop, out count, out _);
    }

    public bool TryHarvest(BlockId block, BlockId tool, out BlockId drop, out int count, out bool toolUsed)
    {
        return TryHarvest(block, tool, out drop, out count, out toolUsed, out _);
    }

    public bool TryHarvest(BlockId block, BlockId tool, out BlockId drop, out int count, out bool toolUsed, out float breakSeconds)
    {
        ToolProfile toolProfile = ToolCatalog.GetProfile(tool);
        toolUsed = toolProfile.IsTool;

        if (!TryFindRule(block, out HarvestRule? rule))
        {
            drop = BlockId.Air;
            count = 0;
            breakSeconds = CalculateBreakSeconds(block, toolProfile, rule: null);
            return false;
        }

        bool meetsToolRequirement = MeetsToolRequirement(rule, toolProfile);
        breakSeconds = CalculateBreakSeconds(block, toolProfile, rule);
        if (!meetsToolRequirement)
        {
            drop = BlockId.Air;
            count = 0;
            return false;
        }

        drop = rule.Drop;
        count = rule.Count;
        return true;
    }

    public bool TryHarvest(Inventory inventory, BlockId block, out BlockId drop, out int count)
    {
        BlockId tool = inventory.GetSelectedBlock();
        return TryHarvest(block, tool, out drop, out count, out _, out _);
    }

    private bool TryFindRule(BlockId block, out HarvestRule rule)
    {
        for (int i = 0; i < _rules.Count; i++)
        {
            HarvestRule candidate = _rules[i];
            if (candidate.Block != block)
            {
                continue;
            }

            rule = candidate;
            return true;
        }

        rule = null!;
        return false;
    }

    private bool MeetsToolRequirement(HarvestRule rule, ToolProfile toolProfile)
    {
        if (rule.RequiredToolType == ToolType.None)
        {
            return true;
        }

        if (!toolProfile.IsTool || toolProfile.Type != rule.RequiredToolType)
        {
            return false;
        }

        return toolProfile.Tier >= rule.MinimumTier;
    }

    private float CalculateBreakSeconds(BlockId block, ToolProfile toolProfile, HarvestRule? rule)
    {
        float resistance = ExplosionResistance.Get(block);
        if (float.IsPositiveInfinity(resistance))
        {
            return float.PositiveInfinity;
        }

        float breakSeconds = MathF.Sqrt(Math.Max(0f, resistance)) * _toolConfig.BlockBreakTimeScale;
        breakSeconds = Math.Max(_toolConfig.MinBreakSeconds, breakSeconds);

        if (!toolProfile.IsTool)
        {
            return breakSeconds;
        }

        bool matchingType = rule == null || rule.RequiredToolType == ToolType.None || toolProfile.Type == rule.RequiredToolType;
        if (matchingType)
        {
            breakSeconds /= GetMiningSpeedMultiplier(toolProfile.Tier);
        }
        else
        {
            breakSeconds *= _toolConfig.WrongToolBreakPenalty;
        }

        return Math.Max(_toolConfig.MinBreakSeconds, breakSeconds);
    }

    private float GetMiningSpeedMultiplier(ToolTier tier)
    {
        return tier switch
        {
            ToolTier.Wood => _toolConfig.WoodMiningSpeedMultiplier,
            ToolTier.Stone => _toolConfig.StoneMiningSpeedMultiplier,
            ToolTier.Iron => _toolConfig.IronMiningSpeedMultiplier,
            ToolTier.Diamond => _toolConfig.DiamondMiningSpeedMultiplier,
            _ => 1f
        };
    }

    private static IReadOnlyList<HarvestRule> BuildDefaultRules()
    {
        return new[]
        {
            new HarvestRule("grass_to_dirt", BlockId.Grass, BlockId.Dirt, 1, ToolType.None, ToolTier.None),
            new HarvestRule("dirt", BlockId.Dirt, BlockId.Dirt, 1, ToolType.None, ToolTier.None),
            new HarvestRule("sand", BlockId.Sand, BlockId.Sand, 1, ToolType.None, ToolTier.None),
            new HarvestRule("gravel", BlockId.Gravel, BlockId.Gravel, 1, ToolType.None, ToolTier.None),
            new HarvestRule("clay", BlockId.Clay, BlockId.Clay, 1, ToolType.None, ToolTier.None),
            new HarvestRule("snow", BlockId.Snow, BlockId.Snow, 1, ToolType.None, ToolTier.None),
            new HarvestRule("cactus", BlockId.Cactus, BlockId.Cactus, 1, ToolType.None, ToolTier.None),

            new HarvestRule("wood", BlockId.Wood, BlockId.Wood, 1, ToolType.None, ToolTier.None),
            new HarvestRule("birch_wood", BlockId.BirchWood, BlockId.BirchWood, 1, ToolType.None, ToolTier.None),
            new HarvestRule("spruce_wood", BlockId.SpruceWood, BlockId.SpruceWood, 1, ToolType.None, ToolTier.None),
            new HarvestRule("planks", BlockId.Planks, BlockId.Planks, 1, ToolType.None, ToolTier.None),

            new HarvestRule("leaves", BlockId.Leaves, BlockId.Stick, 1, ToolType.None, ToolTier.None),
            new HarvestRule("birch_leaves", BlockId.BirchLeaves, BlockId.Stick, 1, ToolType.None, ToolTier.None),
            new HarvestRule("spruce_leaves", BlockId.SpruceLeaves, BlockId.Stick, 1, ToolType.None, ToolTier.None),
            new HarvestRule("dead_bush", BlockId.DeadBush, BlockId.Stick, 2, ToolType.None, ToolTier.None),

            new HarvestRule("stone", BlockId.Stone, BlockId.Cobblestone, 1, ToolType.Pickaxe, ToolTier.Wood),
            new HarvestRule("cobblestone", BlockId.Cobblestone, BlockId.Cobblestone, 1, ToolType.Pickaxe, ToolTier.Wood),
            new HarvestRule("coal_ore", BlockId.CoalOre, BlockId.Coal, 1, ToolType.Pickaxe, ToolTier.Wood),
            new HarvestRule("iron_ore", BlockId.IronOre, BlockId.RawIron, 1, ToolType.Pickaxe, ToolTier.Stone),
            new HarvestRule("gold_ore", BlockId.GoldOre, BlockId.RawGold, 1, ToolType.Pickaxe, ToolTier.Iron),
            new HarvestRule("diamond_ore", BlockId.DiamondOre, BlockId.Diamond, 1, ToolType.Pickaxe, ToolTier.Iron),
            new HarvestRule("obsidian", BlockId.Obsidian, BlockId.Obsidian, 1, ToolType.Pickaxe, ToolTier.Diamond),

            new HarvestRule("crafting_table", BlockId.CraftingTable, BlockId.CraftingTable, 1, ToolType.None, ToolTier.None),
            new HarvestRule("furnace", BlockId.Furnace, BlockId.Furnace, 1, ToolType.None, ToolTier.None),
            new HarvestRule("chest", BlockId.Chest, BlockId.Chest, 1, ToolType.None, ToolTier.None),
            new HarvestRule("torch", BlockId.Torch, BlockId.Torch, 1, ToolType.None, ToolTier.None),
            new HarvestRule("tnt", BlockId.Tnt, BlockId.Tnt, 1, ToolType.None, ToolTier.None),

            new HarvestRule("flower", BlockId.Flower, BlockId.Flower, 1, ToolType.None, ToolTier.None),
            new HarvestRule("tall_grass", BlockId.TallGrass, BlockId.TallGrass, 1, ToolType.None, ToolTier.None),
            new HarvestRule("sugar_cane", BlockId.SugarCane, BlockId.SugarCane, 1, ToolType.None, ToolTier.None)
        };
    }
}
