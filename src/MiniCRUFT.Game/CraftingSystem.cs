using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public sealed class CraftingSystem
{
    private readonly IReadOnlyList<CraftingRecipe> _recipes;

    public CraftingSystem(IReadOnlyList<CraftingRecipe>? recipes = null)
    {
        _recipes = recipes ?? RecipeCatalog.CraftingRecipes;
    }

    public bool TryFindRecipe(ReadOnlySpan<BlockId> grid, int width, int height, [NotNullWhen(true)] out CraftingRecipe? recipe)
    {
        if (!IsValidGrid(grid, width, height))
        {
            recipe = null;
            return false;
        }

        for (int i = 0; i < _recipes.Count; i++)
        {
            var candidate = _recipes[i];
            if (Matches(candidate, grid, width, height))
            {
                recipe = candidate;
                return true;
            }
        }

        recipe = null;
        return false;
    }

    public bool TryCraft(ReadOnlySpan<BlockId> grid, int width, int height, out BlockId output, out int outputCount)
    {
        if (TryFindRecipe(grid, width, height, out var recipe))
        {
            output = recipe.Output;
            outputCount = recipe.OutputCount;
            return true;
        }

        output = BlockId.Air;
        outputCount = 0;
        return false;
    }

    public bool TryGetCraftableRecipe(Inventory inventory, [NotNullWhen(true)] out CraftingRecipe? recipe)
    {
        return TryCraftInventory(inventory, consume: false, out recipe);
    }

    public bool TryCraft(Inventory inventory, [NotNullWhen(true)] out CraftingRecipe? recipe)
    {
        return TryCraftInventory(inventory, consume: true, out recipe);
    }

    private bool TryCraftInventory(Inventory inventory, bool consume, [NotNullWhen(true)] out CraftingRecipe? recipe)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        int bestIndex = -1;
        int bestPriority = int.MaxValue;
        // Prefer the craftable recipe that advances the survival path most directly.
        for (int i = 0; i < _recipes.Count; i++)
        {
            var candidate = _recipes[i];
            if (!CanCraft(inventory, candidate))
            {
                continue;
            }

            if (!inventory.CanAddItem(candidate.Output, candidate.OutputCount))
            {
                continue;
            }

            if (candidate.Priority < bestPriority)
            {
                bestPriority = candidate.Priority;
                bestIndex = i;
            }
        }

        if (bestIndex < 0)
        {
            recipe = null;
            return false;
        }

        var selected = _recipes[bestIndex];
        if (!consume)
        {
            recipe = selected;
            return true;
        }

        if (!ConsumeIngredients(inventory, selected))
        {
            recipe = null;
            return false;
        }

        if (!inventory.TryAddItem(selected.Output, selected.OutputCount))
        {
            Log.Warn($"CraftingSystem: failed to add crafted output {selected.Output} after consuming ingredients.");
            recipe = null;
            return false;
        }

        recipe = selected;
        return true;
    }

    private static bool CanCraft(Inventory inventory, CraftingRecipe recipe)
    {
        var required = GetRequiredIngredients(recipe);
        foreach (var pair in required)
        {
            if (inventory.CountItem(pair.Key) < pair.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ConsumeIngredients(Inventory inventory, CraftingRecipe recipe)
    {
        var required = GetRequiredIngredients(recipe);
        foreach (var pair in required)
        {
            if (!inventory.TryConsumeItem(pair.Key, pair.Value))
            {
                return false;
            }
        }

        return true;
    }

    private static Dictionary<BlockId, int> GetRequiredIngredients(CraftingRecipe recipe)
    {
        var required = new Dictionary<BlockId, int>();
        for (int i = 0; i < recipe.Pattern.Length; i++)
        {
            BlockId? cell = recipe.Pattern[i];
            if (!cell.HasValue)
            {
                continue;
            }

            required.TryGetValue(cell.Value, out int count);
            required[cell.Value] = count + 1;
        }

        return required;
    }

    private static bool IsValidGrid(ReadOnlySpan<BlockId> grid, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        return grid.Length == width * height;
    }

    private static bool Matches(CraftingRecipe recipe, ReadOnlySpan<BlockId> grid, int gridWidth, int gridHeight)
    {
        if (recipe.Width > gridWidth || recipe.Height > gridHeight)
        {
            return false;
        }

        int maxOffsetX = gridWidth - recipe.Width;
        int maxOffsetY = gridHeight - recipe.Height;

        for (int offsetY = 0; offsetY <= maxOffsetY; offsetY++)
        {
            for (int offsetX = 0; offsetX <= maxOffsetX; offsetX++)
            {
                if (MatchesAt(recipe, grid, gridWidth, gridHeight, offsetX, offsetY, mirrored: false))
                {
                    return true;
                }

                if (recipe.AllowMirror && MatchesAt(recipe, grid, gridWidth, gridHeight, offsetX, offsetY, mirrored: true))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool MatchesAt(CraftingRecipe recipe, ReadOnlySpan<BlockId> grid, int gridWidth, int gridHeight, int offsetX, int offsetY, bool mirrored)
    {
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                int gridIndex = Index(x, y, gridWidth);
                BlockId actual = grid[gridIndex];
                bool insidePattern = x >= offsetX && x < offsetX + recipe.Width &&
                                     y >= offsetY && y < offsetY + recipe.Height;

                if (!insidePattern)
                {
                    if (actual != BlockId.Air)
                    {
                        return false;
                    }
                    continue;
                }

                int localX = x - offsetX;
                int localY = y - offsetY;
                if (mirrored)
                {
                    localX = recipe.Width - 1 - localX;
                }

                int recipeIndex = Index(localX, localY, recipe.Width);
                BlockId? expected = recipe.Pattern[recipeIndex];
                if (expected.HasValue)
                {
                    if (actual != expected.Value)
                    {
                        return false;
                    }
                }
                else if (actual != BlockId.Air)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static int Index(int x, int y, int width)
    {
        return y * width + x;
    }
}
