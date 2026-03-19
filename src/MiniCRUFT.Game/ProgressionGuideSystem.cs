using System;
using System.Text;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public readonly record struct ProgressionGuideContext(
    Player Player,
    Inventory Inventory,
    HungerSystem Hunger,
    SleepSystem Sleep,
    DayNightCycle DayNight,
    MobSystem Mobs,
    CraftingSystem Crafting,
    SmeltingSystem Smelting,
    ToolRepairSystem Repair,
    SurvivalConfig Survival,
    bool StrictBetaMode);

public sealed class ProgressionGuideSystem
{
    public string BuildGuideText(ProgressionGuideContext context)
    {
        if (context.StrictBetaMode)
        {
            return string.Empty;
        }

        if (TryBuildHungerHint(context, out string hungerHint))
        {
            return hungerHint;
        }

        if (TryBuildRestHint(context, out string restHint))
        {
            return restHint;
        }

        if (TryBuildRepairHint(context, out string repairHint))
        {
            return repairHint;
        }

        if (TryBuildCraftingHint(context, out string craftingHint))
        {
            return craftingHint;
        }

        if (TryBuildSmeltingHint(context, out string smeltingHint))
        {
            return smeltingHint;
        }

        return "Next procedure: explore caves, ruins and mine shafts for ore and loot.\nGoal: bring back resources that unlock stronger tools and longer trips.";
    }

    private static bool TryBuildHungerHint(ProgressionGuideContext context, out string text)
    {
        if (!context.Survival.Enabled || context.Hunger.CanSprint)
        {
            text = string.Empty;
            return false;
        }

        if (HasFood(context.Inventory))
        {
            text = "Next procedure: eat food before the next fight.\nGoal: keep enough hunger to sprint, mine and survive the night.";
            return true;
        }

        text = "Next procedure: find food before you go deeper.\nGoal: hunger is already limiting your sprint and survival buffer.";
        return true;
    }

    private static bool TryBuildRestHint(ProgressionGuideContext context, out string text)
    {
        if (context.Sleep.CanRest(context.Player, context.Mobs, context.DayNight))
        {
            text = "Next procedure: press R to rest until morning.\nGoal: skip the dangerous part of the night and recover safely.";
            return true;
        }

        float sunIntensity = context.DayNight.GetSunIntensity();
        if (sunIntensity <= context.Survival.RestMinSunIntensity &&
            context.Mobs.HasHostileNearby(context.Player.Position, context.Survival.RestThreatRadius))
        {
            text = "Night is unsafe. Clear nearby hostiles before resting.\nGoal: make the area safe enough for sleep.";
            return true;
        }

        text = string.Empty;
        return false;
    }

    private static bool TryBuildRepairHint(ProgressionGuideContext context, out string text)
    {
        if (!context.Repair.TryGetRepairInfo(context.Inventory, out ToolRepairInfo repairInfo))
        {
            text = string.Empty;
            return false;
        }

        text = $"Next procedure: open inventory and press R to repair {FormatItemName(repairInfo.Tool)} with {FormatItemName(repairInfo.Material)}.\nGoal: keep your tools alive so mining stays fast.";
        return true;
    }

    private static bool TryBuildCraftingHint(ProgressionGuideContext context, out string text)
    {
        if (!context.Crafting.TryGetCraftableRecipe(context.Inventory, out CraftingRecipe? recipe) || recipe is null)
        {
            text = string.Empty;
            return false;
        }

        string action = recipe.Output switch
        {
            BlockId.Planks => $"turn wood into {FormatQuantity(recipe.Output, recipe.OutputCount)}",
            BlockId.Stick => $"craft {FormatQuantity(recipe.Output, recipe.OutputCount)}",
            BlockId.CraftingTable => $"craft {FormatQuantity(recipe.Output, recipe.OutputCount)}",
            BlockId.Furnace => $"craft {FormatQuantity(recipe.Output, recipe.OutputCount)}",
            BlockId.WoodenPickaxe => $"craft {FormatQuantity(recipe.Output, recipe.OutputCount)}",
            BlockId.StonePickaxe => $"craft {FormatQuantity(recipe.Output, recipe.OutputCount)}",
            BlockId.IronPickaxe => $"craft {FormatQuantity(recipe.Output, recipe.OutputCount)}",
            BlockId.DiamondPickaxe => $"craft {FormatQuantity(recipe.Output, recipe.OutputCount)}",
            BlockId.Torch => $"craft {FormatQuantity(recipe.Output, recipe.OutputCount)}",
            BlockId.Tnt => $"craft {FormatQuantity(recipe.Output, recipe.OutputCount)}",
            _ => $"craft {FormatQuantity(recipe.Output, recipe.OutputCount)}"
        };

        string goal = recipe.Output switch
        {
            BlockId.Planks => "Goal: keep moving toward a crafting table and the first tool set.",
            BlockId.Stick => "Goal: turn the sticks into a crafting table or basic tools.",
            BlockId.CraftingTable => "Goal: unlock the full early-game tool chain.",
            BlockId.Furnace => "Goal: smelt ore and food for the next step up.",
            BlockId.WoodenPickaxe => "Goal: mine cobblestone so you can upgrade to stone tools.",
            BlockId.StonePickaxe => "Goal: mine faster and push toward iron.",
            BlockId.IronPickaxe => "Goal: reach deeper ore and tougher threats with better gear.",
            BlockId.DiamondPickaxe => "Goal: this is late-game mining territory.",
            BlockId.Torch => "Goal: light caves before you go deeper.",
            BlockId.Tnt => "Goal: use explosives only when you need a quick shortcut.",
            _ => "Goal: keep building toward tools, food and safer exploration."
        };

        text = $"Next procedure: press C to {action}.\n{goal}";
        return true;
    }

    private static bool TryBuildSmeltingHint(ProgressionGuideContext context, out string text)
    {
        if (!context.Smelting.TryGetSmeltableRecipe(context.Inventory, out SmeltingRecipe? recipe) || recipe is null)
        {
            text = string.Empty;
            return false;
        }

        string action = $"smelt {FormatQuantity(recipe.Input, 1)} into {FormatQuantity(recipe.Output, recipe.OutputCount)}";
        string goal = recipe.Output switch
        {
            BlockId.IronIngot => "Goal: upgrade your tools and unlock stronger progression.",
            BlockId.GoldIngot => "Goal: keep metal for later builds and trading-style progression.",
            BlockId.CookedBeef => "Goal: turn raw food into a better travel supply.",
            BlockId.CookedChicken => "Goal: turn raw food into a better travel supply.",
            BlockId.CookedMutton => "Goal: turn raw food into a better travel supply.",
            BlockId.Glass => "Goal: convert spare sand into utility blocks.",
            BlockId.Charcoal => "Goal: keep fuel flowing when coal is scarce.",
            _ => "Goal: convert raw materials into useful supplies."
        };

        text = $"Next procedure: press V to {action}.\n{goal}";
        return true;
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

    private static string FormatQuantity(BlockId item, int count)
    {
        string name = FormatItemName(item);
        return count > 1 ? $"{name} x{count}" : name;
    }

    private static string FormatItemName(BlockId item)
    {
        if (item == BlockId.Air)
        {
            return string.Empty;
        }

        if (item == BlockId.Tnt)
        {
            return "TNT";
        }

        if (LiquidBlocks.IsWater(item))
        {
            return "Water";
        }

        if (LiquidBlocks.IsLava(item))
        {
            return "Lava";
        }

        string raw = item.ToString();
        if (raw.Length <= 3)
        {
            return raw.ToUpperInvariant();
        }

        var builder = new StringBuilder(raw.Length + 4);
        builder.Append(char.ToUpperInvariant(raw[0]));
        for (int i = 1; i < raw.Length; i++)
        {
            char current = raw[i];
            char previous = raw[i - 1];
            if (char.IsUpper(current) && char.IsLower(previous))
            {
                builder.Append(' ');
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}
