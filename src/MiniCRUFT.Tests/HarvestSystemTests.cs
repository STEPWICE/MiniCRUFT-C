using MiniCRUFT.Game;
using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class HarvestSystemTests
{
    [Theory]
    [InlineData(BlockId.Stone, BlockId.WoodenPickaxe, BlockId.Cobblestone, 1)]
    [InlineData(BlockId.Cobblestone, BlockId.WoodenPickaxe, BlockId.Cobblestone, 1)]
    [InlineData(BlockId.IronOre, BlockId.StonePickaxe, BlockId.RawIron, 1)]
    [InlineData(BlockId.GoldOre, BlockId.IronPickaxe, BlockId.RawGold, 1)]
    [InlineData(BlockId.DiamondOre, BlockId.IronPickaxe, BlockId.Diamond, 1)]
    [InlineData(BlockId.Obsidian, BlockId.DiamondPickaxe, BlockId.Obsidian, 1)]
    [InlineData(BlockId.Wood, BlockId.WoodenAxe, BlockId.Wood, 1)]
    [InlineData(BlockId.BirchWood, BlockId.WoodenAxe, BlockId.BirchWood, 1)]
    [InlineData(BlockId.SpruceWood, BlockId.WoodenAxe, BlockId.SpruceWood, 1)]
    [InlineData(BlockId.Leaves, BlockId.WoodenAxe, BlockId.Stick, 1)]
    [InlineData(BlockId.Sand, BlockId.WoodenShovel, BlockId.Sand, 1)]
    [InlineData(BlockId.CraftingTable, BlockId.Air, BlockId.CraftingTable, 1)]
    [InlineData(BlockId.DeadBush, BlockId.Air, BlockId.Stick, 2)]
    public void HarvestSystem_ResolvesCoreDrops(BlockId block, BlockId tool, BlockId expectedDrop, int expectedCount)
    {
        var system = new HarvestSystem();

        Assert.True(system.TryHarvest(block, tool, out var drop, out var count));
        Assert.Equal(expectedDrop, drop);
        Assert.Equal(expectedCount, count);
    }

    [Fact]
    public void HarvestSystem_RejectsWrongToolTier()
    {
        var system = new HarvestSystem();

        Assert.False(system.TryHarvest(BlockId.GoldOre, BlockId.StonePickaxe, out var goldDrop, out var goldCount));
        Assert.Equal(BlockId.Air, goldDrop);
        Assert.Equal(0, goldCount);

        Assert.False(system.TryHarvest(BlockId.Obsidian, BlockId.IronPickaxe, out var obsidianDrop, out var obsidianCount));
        Assert.Equal(BlockId.Air, obsidianDrop);
        Assert.Equal(0, obsidianCount);

        Assert.False(system.TryHarvest(BlockId.Stone, BlockId.WoodenAxe, out var stoneDrop, out var stoneCount));
        Assert.Equal(BlockId.Air, stoneDrop);
        Assert.Equal(0, stoneCount);
    }

    [Fact]
    public void HarvestSystem_ReportsToolUseOnlyForToolGatedBlocks()
    {
        var system = new HarvestSystem();

        Assert.True(system.TryHarvest(BlockId.Stone, BlockId.WoodenPickaxe, out _, out _, out var stoneUsedTool));
        Assert.True(stoneUsedTool);

        Assert.True(system.TryHarvest(BlockId.CraftingTable, BlockId.Air, out _, out _, out var tableUsedTool));
        Assert.False(tableUsedTool);
    }

    [Fact]
    public void HarvestSystem_UsesFasterBreakTimes_ForHigherTierTools()
    {
        var system = new HarvestSystem();

        Assert.True(system.TryHarvest(BlockId.Stone, BlockId.WoodenPickaxe, out _, out _, out _, out float woodBreakSeconds));
        Assert.True(system.TryHarvest(BlockId.Stone, BlockId.DiamondPickaxe, out _, out _, out _, out float diamondBreakSeconds));
        Assert.False(system.TryHarvest(BlockId.Stone, BlockId.WoodenAxe, out _, out _, out _, out float wrongToolBreakSeconds));

        Assert.True(diamondBreakSeconds < woodBreakSeconds);
        Assert.True(wrongToolBreakSeconds > woodBreakSeconds);
    }
}
