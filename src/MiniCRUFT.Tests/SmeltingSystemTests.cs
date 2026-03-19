using MiniCRUFT.Game;
using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class SmeltingSystemTests
{
    [Theory]
    [InlineData(BlockId.RawIron, BlockId.IronIngot)]
    [InlineData(BlockId.RawGold, BlockId.GoldIngot)]
    [InlineData(BlockId.RawBeef, BlockId.CookedBeef)]
    [InlineData(BlockId.RawChicken, BlockId.CookedChicken)]
    [InlineData(BlockId.RawMutton, BlockId.CookedMutton)]
    [InlineData(BlockId.Sand, BlockId.Glass)]
    [InlineData(BlockId.Wood, BlockId.Charcoal)]
    [InlineData(BlockId.BirchWood, BlockId.Charcoal)]
    [InlineData(BlockId.SpruceWood, BlockId.Charcoal)]
    public void SmeltingSystem_ResolvesUsefulOutputs(BlockId input, BlockId expectedOutput)
    {
        var system = new SmeltingSystem();

        Assert.True(system.TrySmelt(input, out var output, out var count));
        Assert.Equal(expectedOutput, output);
        Assert.Equal(1, count);
    }

    [Fact]
    public void SmeltingSystem_RejectsUnsupportedInput()
    {
        var system = new SmeltingSystem();

        Assert.False(system.TrySmelt(BlockId.Stone, out var output, out var count));
        Assert.Equal(BlockId.Air, output);
        Assert.Equal(0, count);
    }
}
