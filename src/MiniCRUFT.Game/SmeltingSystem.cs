using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public sealed class SmeltingRecipe
{
    public string Id { get; }
    public BlockId Input { get; }
    public BlockId Output { get; }
    public int OutputCount { get; }

    public SmeltingRecipe(string id, BlockId input, BlockId output, int outputCount)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Recipe id must be non-empty.", nameof(id));
        }

        if (outputCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(outputCount));
        }

        Id = id;
        Input = input;
        Output = output;
        OutputCount = outputCount;
    }
}

public sealed class SmeltingSystem
{
    private readonly IReadOnlyList<SmeltingRecipe> _recipes;
    private static readonly BlockId[] FuelPriority =
    {
        BlockId.Coal,
        BlockId.Charcoal,
        BlockId.Wood,
        BlockId.BirchWood,
        BlockId.SpruceWood,
        BlockId.Planks,
        BlockId.Stick
    };

    public SmeltingSystem(IReadOnlyList<SmeltingRecipe>? recipes = null)
    {
        _recipes = recipes ?? BuildDefaultRecipes();
    }

    public bool TryFindRecipe(BlockId input, [NotNullWhen(true)] out SmeltingRecipe? recipe)
    {
        for (int i = 0; i < _recipes.Count; i++)
        {
            var candidate = _recipes[i];
            if (candidate.Input == input)
            {
                recipe = candidate;
                return true;
            }
        }

        recipe = null;
        return false;
    }

    public bool TrySmelt(BlockId input, out BlockId output, out int outputCount)
    {
        if (TryFindRecipe(input, out var recipe))
        {
            output = recipe.Output;
            outputCount = recipe.OutputCount;
            return true;
        }

        output = BlockId.Air;
        outputCount = 0;
        return false;
    }

    public bool TryGetSmeltableRecipe(Inventory inventory, [NotNullWhen(true)] out SmeltingRecipe? recipe)
    {
        return TrySmeltInventory(inventory, consume: false, out recipe);
    }

    public bool TrySmelt(Inventory inventory, [NotNullWhen(true)] out SmeltingRecipe? recipe)
    {
        return TrySmeltInventory(inventory, consume: true, out recipe);
    }

    private bool TrySmeltInventory(Inventory inventory, bool consume, [NotNullWhen(true)] out SmeltingRecipe? recipe)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        for (int i = 0; i < _recipes.Count; i++)
        {
            var candidate = _recipes[i];
            if (inventory.CountItem(candidate.Input) <= 0)
            {
                continue;
            }

            if (!TrySelectFuel(inventory, candidate.Input, out BlockId fuel))
            {
                continue;
            }

            if (!inventory.CanAddItem(candidate.Output, candidate.OutputCount))
            {
                continue;
            }

            if (!consume)
            {
                recipe = candidate;
                return true;
            }

            if (!inventory.TryConsumeItem(candidate.Input, 1))
            {
                continue;
            }

            if (!inventory.TryConsumeItem(fuel, 1))
            {
                // Restore the input so a failed fuel consumption does not eat resources.
                inventory.TryAddItem(candidate.Input, 1);
                continue;
            }

            if (!inventory.TryAddItem(candidate.Output, candidate.OutputCount))
            {
                Log.Warn($"SmeltingSystem: failed to add smelted output {candidate.Output} after consuming ingredients.");
                inventory.TryAddItem(candidate.Input, 1);
                inventory.TryAddItem(fuel, 1);
                recipe = null;
                return false;
            }

            recipe = candidate;
            return true;
        }

        recipe = null;
        return false;
    }

    private static bool TrySelectFuel(Inventory inventory, BlockId input, out BlockId fuel)
    {
        for (int i = 0; i < FuelPriority.Length; i++)
        {
            BlockId candidate = FuelPriority[i];
            if (candidate == input && inventory.CountItem(candidate) < 2)
            {
                continue;
            }

            if (inventory.CountItem(candidate) > 0)
            {
                fuel = candidate;
                return true;
            }
        }

        fuel = BlockId.Air;
        return false;
    }

    private static IReadOnlyList<SmeltingRecipe> BuildDefaultRecipes()
    {
        return new[]
        {
            new SmeltingRecipe("smelt_raw_iron", BlockId.RawIron, BlockId.IronIngot, 1),
            new SmeltingRecipe("smelt_raw_gold", BlockId.RawGold, BlockId.GoldIngot, 1),
            new SmeltingRecipe("smelt_raw_beef", BlockId.RawBeef, BlockId.CookedBeef, 1),
            new SmeltingRecipe("smelt_raw_chicken", BlockId.RawChicken, BlockId.CookedChicken, 1),
            new SmeltingRecipe("smelt_raw_mutton", BlockId.RawMutton, BlockId.CookedMutton, 1),
            new SmeltingRecipe("smelt_sand", BlockId.Sand, BlockId.Glass, 1),
            new SmeltingRecipe("smelt_wood", BlockId.Wood, BlockId.Charcoal, 1),
            new SmeltingRecipe("smelt_birch_wood", BlockId.BirchWood, BlockId.Charcoal, 1),
            new SmeltingRecipe("smelt_spruce_wood", BlockId.SpruceWood, BlockId.Charcoal, 1)
        };
    }
}
