using System;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public readonly struct ToolRepairInfo
{
    public BlockId Tool { get; }
    public BlockId Material { get; }
    public int MaterialCount { get; }
    public int RepairAmount { get; }
    public int CurrentDurability { get; }
    public int NewDurability { get; }
    public int MaxDurability { get; }

    public ToolRepairInfo(BlockId tool, BlockId material, int materialCount, int repairAmount, int currentDurability, int newDurability, int maxDurability)
    {
        Tool = tool;
        Material = material;
        MaterialCount = materialCount;
        RepairAmount = repairAmount;
        CurrentDurability = currentDurability;
        NewDurability = newDurability;
        MaxDurability = maxDurability;
    }
}

public sealed class ToolRepairSystem
{
    private readonly ToolConfig _config;

    public ToolRepairSystem(ToolConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public bool TryGetRepairInfo(Inventory inventory, out ToolRepairInfo info)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        BlockId tool = inventory.GetSelectedBlock();
        if (!ToolCatalog.IsTool(tool))
        {
            info = default;
            return false;
        }

        int currentDurability = Math.Clamp(inventory.GetSelectedDurability(), 0, inventory.GetSelectedMaxDurability());
        int maxDurability = Math.Max(0, inventory.GetSelectedMaxDurability());
        if (currentDurability <= 0 || maxDurability <= 0 || currentDurability >= maxDurability)
        {
            info = default;
            return false;
        }

        ToolProfile profile = ToolCatalog.GetProfile(tool);
        var repairProfile = GetRepairProfile(profile.Tier);
        if (repairProfile.Material == BlockId.Air || repairProfile.RepairAmount <= 0)
        {
            info = default;
            return false;
        }

        if (inventory.CountItem(repairProfile.Material) < repairProfile.MaterialCount)
        {
            info = default;
            return false;
        }

        int repairedAmount = Math.Min(repairProfile.RepairAmount, maxDurability - currentDurability);
        if (repairedAmount <= 0)
        {
            info = default;
            return false;
        }

        info = new ToolRepairInfo(
            tool,
            repairProfile.Material,
            repairProfile.MaterialCount,
            repairedAmount,
            currentDurability,
            currentDurability + repairedAmount,
            maxDurability);
        return true;
    }

    public bool TryRepairSelected(Inventory inventory, out ToolRepairInfo info)
    {
        if (!TryGetRepairInfo(inventory, out info))
        {
            return false;
        }

        if (!inventory.TryConsumeItem(info.Material, info.MaterialCount))
        {
            info = default;
            return false;
        }

        if (!inventory.TryRepairSelectedTool(info.RepairAmount, out int repairedAmount))
        {
            inventory.TryAddItem(info.Material, info.MaterialCount);
            info = default;
            return false;
        }

        if (repairedAmount != info.RepairAmount)
        {
            inventory.TryAddItem(info.Material, info.MaterialCount);
            info = default;
            return false;
        }

        info = new ToolRepairInfo(
            info.Tool,
            info.Material,
            info.MaterialCount,
            repairedAmount,
            info.CurrentDurability,
            info.CurrentDurability + repairedAmount,
            info.MaxDurability);
        return true;
    }

    private (BlockId Material, int RepairAmount, int MaterialCount) GetRepairProfile(ToolTier tier)
    {
        return tier switch
        {
            ToolTier.Wood => (BlockId.Planks, _config.WoodRepairDurability, 1),
            ToolTier.Stone => (BlockId.Cobblestone, _config.StoneRepairDurability, 1),
            ToolTier.Iron => (BlockId.IronIngot, _config.IronRepairDurability, 1),
            ToolTier.Diamond => (BlockId.Diamond, _config.DiamondRepairDurability, 1),
            _ => (BlockId.Air, 0, 0)
        };
    }
}
