using MiniCRUFT.Core;
using MiniCRUFT.Game;
using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class ToolRepairSystemTests
{
    [Fact]
    public void TryGetRepairInfo_ReportsRequiredMaterialAndRepairAmount()
    {
        var config = new ToolConfig
        {
            StoneMaxDurability = 20,
            StoneRepairDurability = 16
        };
        var inventory = new Inventory(strictBetaMode: true, config);
        Assert.True(inventory.TryAddItem(BlockId.StonePickaxe, 1));
        Assert.True(inventory.TryDamageSelectedTool(9, out _));
        Assert.True(inventory.TryAddItem(BlockId.Cobblestone, 1));

        var system = new ToolRepairSystem(config);

        Assert.True(system.TryGetRepairInfo(inventory, out var info));
        Assert.Equal(BlockId.StonePickaxe, info.Tool);
        Assert.Equal(BlockId.Cobblestone, info.Material);
        Assert.Equal(1, info.MaterialCount);
        Assert.Equal(9, info.RepairAmount);
        Assert.Equal(11, info.CurrentDurability);
        Assert.Equal(20, info.NewDurability);
        Assert.Equal(20, info.MaxDurability);
    }

    [Fact]
    public void TryRepairSelected_ConsumesMaterialAndRestoresTool()
    {
        var config = new ToolConfig
        {
            WoodMaxDurability = 12,
            WoodRepairDurability = 5
        };
        var inventory = new Inventory(strictBetaMode: true, config);
        Assert.True(inventory.TryAddItem(BlockId.WoodenPickaxe, 1));
        Assert.True(inventory.TryDamageSelectedTool(7, out _));
        Assert.True(inventory.TryAddItem(BlockId.Planks, 1));

        var system = new ToolRepairSystem(config);

        Assert.True(system.TryRepairSelected(inventory, out var info));
        Assert.Equal(5, info.RepairAmount);
        Assert.Equal(10, inventory.GetSelectedDurability());
        Assert.Equal(0, inventory.CountItem(BlockId.Planks));
    }
}
