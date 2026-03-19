using System;
using MiniCRUFT.IO;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public sealed class Inventory
{
    private readonly bool _strictBetaMode;
    private readonly ToolConfig _toolConfig;

    public BlockId[] Hotbar { get; } = new BlockId[9];
    public int[] HotbarCounts { get; } = new int[9];
    public int[] HotbarDurability { get; } = new int[9];
    public int[] HotbarMaxDurability { get; } = new int[9];
    public int SelectedIndex { get; private set; }

    public Inventory(bool strictBetaMode = false, ToolConfig? toolConfig = null)
    {
        _strictBetaMode = strictBetaMode;
        _toolConfig = toolConfig ?? new ToolConfig();
        Reset();
    }

    public BlockId GetSelectedBlock() => Hotbar[SelectedIndex];

    public int GetSelectedCount() => HotbarCounts[SelectedIndex];

    public int GetSelectedDurability() => HotbarDurability[SelectedIndex];

    public int GetSelectedMaxDurability() => HotbarMaxDurability[SelectedIndex];

    public int CountItem(BlockId item)
    {
        if (item == BlockId.Air)
        {
            return 0;
        }

        int total = 0;
        for (int i = 0; i < Hotbar.Length; i++)
        {
            if (Hotbar[i] == item)
            {
                total += HotbarCounts[i];
            }
        }

        return total;
    }

    public bool CanAddItem(BlockId item, int amount)
    {
        if (item == BlockId.Air || amount <= 0)
        {
            return false;
        }

        int maxCount = BlockStackDefaults.GetMaxCount(item);
        if (maxCount <= 0)
        {
            return false;
        }

        int remaining = amount;
        for (int i = 0; i < Hotbar.Length; i++)
        {
            if (Hotbar[i] == item)
            {
                int space = maxCount - HotbarCounts[i];
                if (space > 0)
                {
                    remaining -= Math.Min(remaining, space);
                    if (remaining <= 0)
                    {
                        return true;
                    }
                }
            }
        }

        for (int i = 0; i < Hotbar.Length; i++)
        {
            if (Hotbar[i] == BlockId.Air)
            {
                remaining -= Math.Min(remaining, maxCount);
                if (remaining <= 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool TryAddItem(BlockId item, int amount)
    {
        if (!CanAddItem(item, amount))
        {
            return false;
        }

        int remaining = amount;
        int maxCount = BlockStackDefaults.GetMaxCount(item);
        for (int i = 0; i < Hotbar.Length && remaining > 0; i++)
        {
            if (Hotbar[i] != item)
            {
                continue;
            }

            int space = maxCount - HotbarCounts[i];
            if (space <= 0)
            {
                continue;
            }

            int added = Math.Min(remaining, space);
            HotbarCounts[i] += added;
            remaining -= added;
        }

        for (int i = 0; i < Hotbar.Length && remaining > 0; i++)
        {
            if (Hotbar[i] != BlockId.Air)
            {
                continue;
            }

            int added = Math.Min(remaining, maxCount);
            SetSlot(i, item, added);
            remaining -= added;
        }

        return remaining == 0;
    }

    public bool TryConsumeItem(BlockId item, int amount)
    {
        if (item == BlockId.Air || amount <= 0)
        {
            return false;
        }

        if (CountItem(item) < amount)
        {
            return false;
        }

        int remaining = amount;
        for (int i = 0; i < Hotbar.Length && remaining > 0; i++)
        {
            if (Hotbar[i] != item)
            {
                continue;
            }

            int taken = Math.Min(remaining, HotbarCounts[i]);
            HotbarCounts[i] -= taken;
            remaining -= taken;
            if (HotbarCounts[i] <= 0)
            {
                ClearSlot(i);
            }
        }

        return remaining == 0;
    }

    public bool TryConsumeSelected(int amount = 1)
    {
        if (amount < 1)
        {
            return false;
        }

        int count = HotbarCounts[SelectedIndex];
        if (count < amount || Hotbar[SelectedIndex] == BlockId.Air)
        {
            return false;
        }

        count -= amount;
        if (count <= 0)
        {
            ClearSlot(SelectedIndex);
        }
        else
        {
            HotbarCounts[SelectedIndex] = count;
        }

        return true;
    }

    public void Scroll(int delta)
    {
        if (delta == 0)
        {
            return;
        }

        int direction = delta > 0 ? -1 : 1;
        SelectedIndex = (SelectedIndex + direction + Hotbar.Length) % Hotbar.Length;
    }

    public void Reset()
    {
        Array.Fill(Hotbar, BlockId.Air);
        Array.Fill(HotbarCounts, 0);
        Array.Fill(HotbarDurability, 0);
        Array.Fill(HotbarMaxDurability, 0);
        if (!_strictBetaMode)
        {
            SetSlot(0, BlockId.Grass, BlockStackDefaults.GetDefaultCount(BlockId.Grass));
            SetSlot(1, BlockId.Dirt, BlockStackDefaults.GetDefaultCount(BlockId.Dirt));
            SetSlot(2, BlockId.Stone, BlockStackDefaults.GetDefaultCount(BlockId.Stone));
            SetSlot(3, BlockId.Wood, BlockStackDefaults.GetDefaultCount(BlockId.Wood));
            SetSlot(4, BlockId.Planks, BlockStackDefaults.GetDefaultCount(BlockId.Planks));
            SetSlot(5, BlockId.Glass, BlockStackDefaults.GetDefaultCount(BlockId.Glass));
            SetSlot(6, BlockId.Sand, BlockStackDefaults.GetDefaultCount(BlockId.Sand));
            SetSlot(7, BlockId.Torch, BlockStackDefaults.GetDefaultCount(BlockId.Torch));
            SetSlot(8, BlockId.Tnt, BlockStackDefaults.GetDefaultCount(BlockId.Tnt));
        }
        SelectedIndex = 0;
    }

    public void ApplySave(BlockId[] hotbar, int selectedIndex)
    {
        ApplySave(hotbar, BlockStackDefaults.CreateDefaultCounts(hotbar), null, selectedIndex);
    }

    public void ApplySave(BlockId[] hotbar, int[] counts, int selectedIndex)
    {
        ApplySave(hotbar, counts, null, selectedIndex);
    }

    public void ApplySave(BlockId[] hotbar, int[] counts, int[]? toolDurability, int selectedIndex)
    {
        hotbar ??= Array.Empty<BlockId>();
        counts ??= Array.Empty<int>();

        int count = Math.Min(Hotbar.Length, hotbar.Length);
        for (int i = 0; i < count; i++)
        {
            int slotCount = i < counts.Length
                ? counts[i]
                : BlockStackDefaults.GetDefaultCount(hotbar[i]);
            int slotDurability = toolDurability != null && i < toolDurability.Length
                ? toolDurability[i]
                : -1;
            SetSlot(i, hotbar[i], slotCount, slotDurability);
        }

        for (int i = count; i < Hotbar.Length; i++)
        {
            ClearSlot(i);
        }

        if (selectedIndex < 0 || selectedIndex >= Hotbar.Length)
        {
            selectedIndex = 0;
        }

        SelectedIndex = selectedIndex;
    }

    public bool TryDamageSelectedTool(int wearAmount, out bool broke)
    {
        broke = false;
        if (wearAmount < 1)
        {
            return false;
        }

        var block = Hotbar[SelectedIndex];
        if (block == BlockId.Air || !ToolCatalog.IsTool(block) || HotbarCounts[SelectedIndex] <= 0)
        {
            return false;
        }

        int currentDurability = HotbarDurability[SelectedIndex];
        int maxDurability = HotbarMaxDurability[SelectedIndex];
        if (currentDurability <= 0 || maxDurability <= 0)
        {
            return false;
        }

        currentDurability -= wearAmount;
        if (currentDurability <= 0)
        {
            broke = true;
            ClearSlot(SelectedIndex);
        }
        else
        {
            HotbarDurability[SelectedIndex] = currentDurability;
        }

        return true;
    }

    public bool TryRepairSelectedTool(int repairAmount, out int repairedAmount)
    {
        repairedAmount = 0;
        if (repairAmount < 1)
        {
            return false;
        }

        var block = Hotbar[SelectedIndex];
        if (block == BlockId.Air || !ToolCatalog.IsTool(block) || HotbarCounts[SelectedIndex] <= 0)
        {
            return false;
        }

        int currentDurability = HotbarDurability[SelectedIndex];
        int maxDurability = HotbarMaxDurability[SelectedIndex];
        if (currentDurability <= 0 || maxDurability <= 0 || currentDurability >= maxDurability)
        {
            return false;
        }

        repairedAmount = Math.Min(repairAmount, maxDurability - currentDurability);
        if (repairedAmount <= 0)
        {
            return false;
        }

        HotbarDurability[SelectedIndex] = currentDurability + repairedAmount;
        return true;
    }

    public void SetSlot(int index, BlockId block, int count, int durability = -1)
    {
        if ((uint)index >= (uint)Hotbar.Length)
        {
            return;
        }

        if (block == BlockId.Air || count <= 0)
        {
            ClearSlot(index);
            return;
        }

        count = Math.Min(count, BlockStackDefaults.GetMaxCount(block));
        if (ToolCatalog.IsTool(block))
        {
            int maxDurability = GetMaxToolDurability(block);
            int resolvedDurability = durability < 0
                ? maxDurability
                : Math.Clamp(durability, 0, maxDurability);
            if (maxDurability <= 0 || resolvedDurability <= 0)
            {
                ClearSlot(index);
                return;
            }

            Hotbar[index] = block;
            HotbarCounts[index] = 1;
            HotbarDurability[index] = resolvedDurability;
            HotbarMaxDurability[index] = maxDurability;
            return;
        }

        Hotbar[index] = block;
        HotbarCounts[index] = count;
        HotbarDurability[index] = 0;
        HotbarMaxDurability[index] = 0;
    }

    private int GetMaxToolDurability(BlockId item)
    {
        return ToolCatalog.GetProfile(item).Tier switch
        {
            ToolTier.Wood => _toolConfig.WoodMaxDurability,
            ToolTier.Stone => _toolConfig.StoneMaxDurability,
            ToolTier.Iron => _toolConfig.IronMaxDurability,
            ToolTier.Diamond => _toolConfig.DiamondMaxDurability,
            _ => 0
        };
    }

    private void ClearSlot(int index)
    {
        if ((uint)index >= (uint)Hotbar.Length)
        {
            return;
        }

        Hotbar[index] = BlockId.Air;
        HotbarCounts[index] = 0;
        HotbarDurability[index] = 0;
        HotbarMaxDurability[index] = 0;
    }
}
