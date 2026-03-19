using System;
using System.Text;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public sealed class ProgressionMilestoneSystem
{
    private static readonly string[] MilestoneLabels =
    [
        "Gather wood",
        "Craft a table",
        "Make a furnace and torches",
        "Upgrade to stone tools",
        "Smelt iron",
        "Explore caves and structures"
    ];

    public string BuildMilestoneText(ProgressionGuideContext context)
    {
        if (context.StrictBetaMode)
        {
            return string.Empty;
        }

        int currentIndex = GetCurrentMilestoneIndex(context);
        var builder = new StringBuilder(256);
        builder.AppendLine("Milestones:");
        for (int i = 0; i < MilestoneLabels.Length; i++)
        {
            string marker = i < currentIndex ? "[x]" : i == currentIndex ? "[>]" : "[ ]";
            builder.Append(marker);
            builder.Append(' ');
            builder.AppendLine(MilestoneLabels[i]);
        }

        if (currentIndex >= MilestoneLabels.Length)
        {
            builder.AppendLine("Route complete: push deeper and bring home better loot.");
        }
        else
        {
            builder.Append("Current focus: ");
            builder.AppendLine(MilestoneLabels[currentIndex]);
        }

        return builder.ToString();
    }

    private static int GetCurrentMilestoneIndex(ProgressionGuideContext context)
    {
        if (!HasWood(context.Inventory))
        {
            return 0;
        }

        if (!HasItem(context.Inventory, BlockId.CraftingTable))
        {
            return 1;
        }

        if (!HasItem(context.Inventory, BlockId.Furnace) || !HasItem(context.Inventory, BlockId.Torch))
        {
            return 2;
        }

        if (!HasPickaxeAtLeast(context.Inventory, ToolTier.Stone))
        {
            return 3;
        }

        if (!HasItem(context.Inventory, BlockId.IronIngot) && !HasPickaxeAtLeast(context.Inventory, ToolTier.Iron))
        {
            return 4;
        }

        if (!HasCaveReadyLoadout(context.Inventory))
        {
            return 5;
        }

        return MilestoneLabels.Length;
    }

    private static bool HasCaveReadyLoadout(Inventory inventory)
    {
        return HasPickaxeAtLeast(inventory, ToolTier.Stone) &&
               HasItem(inventory, BlockId.Torch) &&
               HasFood(inventory);
    }

    private static bool HasWood(Inventory inventory)
    {
        return HasItem(inventory, BlockId.Wood) ||
               HasItem(inventory, BlockId.BirchWood) ||
               HasItem(inventory, BlockId.SpruceWood) ||
               HasItem(inventory, BlockId.Planks);
    }

    private static bool HasItem(Inventory inventory, BlockId item)
    {
        return inventory.CountItem(item) > 0;
    }

    private static bool HasPickaxeAtLeast(Inventory inventory, ToolTier tier)
    {
        for (int i = 0; i < inventory.Hotbar.Length; i++)
        {
            BlockId item = inventory.Hotbar[i];
            if (!ToolCatalog.IsPickaxe(item))
            {
                continue;
            }

            if (ToolCatalog.GetTier(item) >= tier)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasFood(Inventory inventory)
    {
        for (int i = 0; i < inventory.Hotbar.Length; i++)
        {
            if (FoodCatalog.IsFood(inventory.Hotbar[i]))
            {
                return true;
            }
        }

        return false;
    }
}
