using MiniCRUFT.Game;
using MiniCRUFT.Core;
using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class InventoryTests
{
    [Fact]
    public void Reset_UsesClassicLoadout_InNonStrictMode()
    {
        var inventory = new Inventory();

        Assert.Equal(BlockId.Grass, inventory.Hotbar[0]);
        Assert.Equal(BlockId.Dirt, inventory.Hotbar[1]);
        Assert.Equal(BlockId.Stone, inventory.Hotbar[2]);
        Assert.Equal(BlockId.Planks, inventory.Hotbar[4]);
        Assert.Equal(13, inventory.HotbarCounts[4]);
        Assert.Equal(16, inventory.HotbarCounts[7]);
        Assert.Equal(4, inventory.HotbarCounts[8]);
        Assert.Equal(0, inventory.SelectedIndex);
    }

    [Fact]
    public void Reset_StartsEmpty_InStrictBetaMode()
    {
        var inventory = new Inventory(strictBetaMode: true);

        Assert.All(inventory.Hotbar, item => Assert.Equal(BlockId.Air, item));
        Assert.All(inventory.HotbarCounts, count => Assert.Equal(0, count));
        Assert.Equal(0, inventory.SelectedIndex);
    }

    [Fact]
    public void ApplySave_UsesExplicitCounts()
    {
        var inventory = new Inventory(strictBetaMode: true);
        var hotbar = new[]
        {
            BlockId.Stone,
            BlockId.Torch,
            BlockId.Tnt
        };
        var counts = new[]
        {
            7,
            3,
            2
        };

        inventory.ApplySave(hotbar, counts, 2);

        Assert.Equal(BlockId.Stone, inventory.Hotbar[0]);
        Assert.Equal(7, inventory.HotbarCounts[0]);
        Assert.Equal(BlockId.Torch, inventory.Hotbar[1]);
        Assert.Equal(3, inventory.HotbarCounts[1]);
        Assert.Equal(BlockId.Tnt, inventory.Hotbar[2]);
        Assert.Equal(2, inventory.HotbarCounts[2]);
        Assert.Equal(2, inventory.SelectedIndex);
    }

    [Fact]
    public void TryConsumeSelected_DecrementsAndClearsSlot()
    {
        var inventory = new Inventory(strictBetaMode: true);
        inventory.SetSlot(0, BlockId.Stone, 2);

        Assert.True(inventory.TryConsumeSelected());
        Assert.Equal(BlockId.Stone, inventory.Hotbar[0]);
        Assert.Equal(1, inventory.HotbarCounts[0]);

        Assert.True(inventory.TryConsumeSelected());
        Assert.Equal(BlockId.Air, inventory.Hotbar[0]);
        Assert.Equal(0, inventory.HotbarCounts[0]);

        Assert.False(inventory.TryConsumeSelected());
    }

    [Fact]
    public void TryAddItem_AssignsFullDurabilityToTools()
    {
        var toolConfig = new ToolConfig
        {
            WoodMaxDurability = 10,
            StoneMaxDurability = 20,
            IronMaxDurability = 30,
            DiamondMaxDurability = 40,
            ToolWearPerAction = 2
        };
        var inventory = new Inventory(strictBetaMode: true, toolConfig);

        Assert.True(inventory.TryAddItem(BlockId.WoodenPickaxe, 1));
        Assert.Equal(BlockId.WoodenPickaxe, inventory.Hotbar[0]);
        Assert.Equal(1, inventory.HotbarCounts[0]);
        Assert.Equal(10, inventory.HotbarDurability[0]);
        Assert.Equal(10, inventory.HotbarMaxDurability[0]);
    }

    [Fact]
    public void TryDamageSelectedTool_ReducesDurabilityAndBreaksSlot()
    {
        var toolConfig = new ToolConfig
        {
            WoodMaxDurability = 10,
            StoneMaxDurability = 20,
            IronMaxDurability = 30,
            DiamondMaxDurability = 40,
            ToolWearPerAction = 2
        };
        var inventory = new Inventory(strictBetaMode: true, toolConfig);
        inventory.SetSlot(0, BlockId.StonePickaxe, 1);

        Assert.Equal(20, inventory.HotbarDurability[0]);
        Assert.True(inventory.TryDamageSelectedTool(6, out var broke));
        Assert.False(broke);
        Assert.Equal(14, inventory.HotbarDurability[0]);

        Assert.True(inventory.TryDamageSelectedTool(14, out broke));
        Assert.True(broke);
        Assert.Equal(BlockId.Air, inventory.Hotbar[0]);
        Assert.Equal(0, inventory.HotbarCounts[0]);
        Assert.Equal(0, inventory.HotbarDurability[0]);
        Assert.Equal(0, inventory.HotbarMaxDurability[0]);
    }

    [Fact]
    public void TryRepairSelectedTool_RestoresDurability()
    {
        var toolConfig = new ToolConfig
        {
            WoodMaxDurability = 10,
            StoneMaxDurability = 20,
            IronMaxDurability = 30,
            DiamondMaxDurability = 40,
            ToolWearPerAction = 2
        };
        var inventory = new Inventory(strictBetaMode: true, toolConfig);
        inventory.SetSlot(0, BlockId.StonePickaxe, 1);

        Assert.True(inventory.TryDamageSelectedTool(6, out var broke));
        Assert.False(broke);
        Assert.True(inventory.TryRepairSelectedTool(4, out int repaired));
        Assert.Equal(4, repaired);
        Assert.Equal(18, inventory.HotbarDurability[0]);

        Assert.True(inventory.TryRepairSelectedTool(20, out repaired));
        Assert.Equal(2, repaired);
        Assert.Equal(20, inventory.HotbarDurability[0]);
    }

    [Fact]
    public void ApplySave_AssignsFullDurabilityToToolSlots_WhenDurabilityIsMissing()
    {
        var toolConfig = new ToolConfig
        {
            WoodMaxDurability = 10,
            StoneMaxDurability = 20,
            IronMaxDurability = 30,
            DiamondMaxDurability = 40,
            ToolWearPerAction = 2
        };
        var inventory = new Inventory(strictBetaMode: true, toolConfig);

        inventory.ApplySave(
            new[] { BlockId.WoodenPickaxe, BlockId.Stone },
            new[] { 1, 4 },
            selectedIndex: 0);

        Assert.Equal(BlockId.WoodenPickaxe, inventory.Hotbar[0]);
        Assert.Equal(1, inventory.HotbarCounts[0]);
        Assert.Equal(10, inventory.HotbarDurability[0]);
        Assert.Equal(10, inventory.HotbarMaxDurability[0]);
        Assert.Equal(BlockId.Stone, inventory.Hotbar[1]);
        Assert.Equal(4, inventory.HotbarCounts[1]);
        Assert.Equal(0, inventory.HotbarDurability[1]);
        Assert.Equal(0, inventory.HotbarMaxDurability[1]);
    }
}
