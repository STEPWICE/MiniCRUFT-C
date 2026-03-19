using System;
using MiniCRUFT.Core;
using MiniCRUFT.Game;
using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class CraftingSystemTests
{
    [Theory]
    [InlineData(BlockId.Wood)]
    [InlineData(BlockId.BirchWood)]
    [InlineData(BlockId.SpruceWood)]
    public void CraftingSystem_CraftsPlanksFromAnyLog(BlockId log)
    {
        var system = new CraftingSystem();
        var grid = Grid(
            BlockId.Air, BlockId.Air, BlockId.Air,
            BlockId.Air, log, BlockId.Air,
            BlockId.Air, BlockId.Air, BlockId.Air);

        Assert.True(system.TryCraft(grid, 3, 3, out var output, out var count));
        Assert.Equal(BlockId.Planks, output);
        Assert.Equal(4, count);
    }

    [Fact]
    public void CraftingSystem_CraftsSticksFromOffsetPlanks()
    {
        var system = new CraftingSystem();
        var grid = Grid(
            BlockId.Air, BlockId.Air, BlockId.Air,
            BlockId.Air, BlockId.Planks, BlockId.Air,
            BlockId.Air, BlockId.Planks, BlockId.Air);

        Assert.True(system.TryCraft(grid, 3, 3, out var output, out var count));
        Assert.Equal(BlockId.Stick, output);
        Assert.Equal(4, count);
    }

    [Fact]
    public void CraftingSystem_CraftsCoreBlocks()
    {
        var system = new CraftingSystem();

        var craftingTableGrid = Grid(
            BlockId.Planks, BlockId.Planks, BlockId.Air,
            BlockId.Planks, BlockId.Planks, BlockId.Air,
            BlockId.Air, BlockId.Air, BlockId.Air);

        Assert.True(system.TryCraft(craftingTableGrid, 3, 3, out var craftingTable, out var craftingTableCount));
        Assert.Equal(BlockId.CraftingTable, craftingTable);
        Assert.Equal(1, craftingTableCount);

        var furnaceGrid = Grid(
            BlockId.Cobblestone, BlockId.Cobblestone, BlockId.Cobblestone,
            BlockId.Cobblestone, BlockId.Air, BlockId.Cobblestone,
            BlockId.Cobblestone, BlockId.Cobblestone, BlockId.Cobblestone);

        Assert.True(system.TryCraft(furnaceGrid, 3, 3, out var furnace, out var furnaceCount));
        Assert.Equal(BlockId.Furnace, furnace);
        Assert.Equal(1, furnaceCount);

        var chestGrid = Grid(
            BlockId.Planks, BlockId.Planks, BlockId.Planks,
            BlockId.Planks, BlockId.Air, BlockId.Planks,
            BlockId.Planks, BlockId.Planks, BlockId.Planks);

        Assert.True(system.TryCraft(chestGrid, 3, 3, out var chest, out var chestCount));
        Assert.Equal(BlockId.Chest, chest);
        Assert.Equal(1, chestCount);
    }

    [Fact]
    public void CraftingSystem_PrioritizesCraftingTableOverLooseMaterials()
    {
        var system = new CraftingSystem();
        var inventory = new Inventory(strictBetaMode: true, new ToolConfig());
        Assert.True(inventory.TryAddItem(BlockId.Wood, 1));
        Assert.True(inventory.TryAddItem(BlockId.Planks, 4));

        Assert.True(system.TryGetCraftableRecipe(inventory, out var recipe));
        Assert.NotNull(recipe);
        Assert.Equal(BlockId.CraftingTable, recipe!.Output);
    }

    [Fact]
    public void CraftingSystem_CraftsTools()
    {
        var system = new CraftingSystem();

        var pickaxeGrid = Grid(
            BlockId.Wood, BlockId.Wood, BlockId.Wood,
            BlockId.Air, BlockId.Stick, BlockId.Air,
            BlockId.Air, BlockId.Stick, BlockId.Air);
        Assert.True(system.TryCraft(pickaxeGrid, 3, 3, out var pickaxe, out var pickaxeCount));
        Assert.Equal(BlockId.WoodenPickaxe, pickaxe);
        Assert.Equal(1, pickaxeCount);

        var mirroredAxeGrid = Grid(
            BlockId.Air, BlockId.Wood, BlockId.Wood,
            BlockId.Air, BlockId.Stick, BlockId.Wood,
            BlockId.Air, BlockId.Stick, BlockId.Air);
        Assert.True(system.TryCraft(mirroredAxeGrid, 3, 3, out var axe, out var axeCount));
        Assert.Equal(BlockId.WoodenAxe, axe);
        Assert.Equal(1, axeCount);

        var shovelGrid = Grid(
            BlockId.Air, BlockId.Cobblestone, BlockId.Air,
            BlockId.Air, BlockId.Stick, BlockId.Air,
            BlockId.Air, BlockId.Stick, BlockId.Air);
        Assert.True(system.TryCraft(shovelGrid, 3, 3, out var shovel, out var shovelCount));
        Assert.Equal(BlockId.StoneShovel, shovel);
        Assert.Equal(1, shovelCount);

        var swordGrid = Grid(
            BlockId.Air, BlockId.IronIngot, BlockId.Air,
            BlockId.Air, BlockId.IronIngot, BlockId.Air,
            BlockId.Air, BlockId.Stick, BlockId.Air);
        Assert.True(system.TryCraft(swordGrid, 3, 3, out var sword, out var swordCount));
        Assert.Equal(BlockId.IronSword, sword);
        Assert.Equal(1, swordCount);
    }

    [Fact]
    public void CraftingSystem_CraftsTorchesAndTnt()
    {
        var system = new CraftingSystem();

        var torchFromCoal = Grid(
            BlockId.Air, BlockId.Coal, BlockId.Air,
            BlockId.Air, BlockId.Stick, BlockId.Air,
            BlockId.Air, BlockId.Air, BlockId.Air);
        Assert.True(system.TryCraft(torchFromCoal, 3, 3, out var torch, out var torchCount));
        Assert.Equal(BlockId.Torch, torch);
        Assert.Equal(4, torchCount);

        var torchFromCharcoal = Grid(
            BlockId.Air, BlockId.Charcoal, BlockId.Air,
            BlockId.Air, BlockId.Stick, BlockId.Air,
            BlockId.Air, BlockId.Air, BlockId.Air);
        Assert.True(system.TryCraft(torchFromCharcoal, 3, 3, out var torch2, out var torchCount2));
        Assert.Equal(BlockId.Torch, torch2);
        Assert.Equal(4, torchCount2);

        var tntGrid = Grid(
            BlockId.Gunpowder, BlockId.Sand, BlockId.Gunpowder,
            BlockId.Sand, BlockId.Gunpowder, BlockId.Sand,
            BlockId.Gunpowder, BlockId.Sand, BlockId.Gunpowder);
        Assert.True(system.TryCraft(tntGrid, 3, 3, out var tnt, out var tntCount));
        Assert.Equal(BlockId.Tnt, tnt);
        Assert.Equal(1, tntCount);
    }

    private static BlockId[] Grid(params BlockId[] slots)
    {
        if (slots.Length != 9)
        {
            throw new ArgumentException("Expected a 3x3 crafting grid.");
        }

        return slots;
    }
}
