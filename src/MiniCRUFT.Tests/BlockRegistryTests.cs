using MiniCRUFT.World;
using MiniCRUFT.Renderer;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class BlockRegistryTests
{
    [Fact]
    public void LeafBlocks_DoNotBlockSkyLight()
    {
        Assert.False(BlockRegistry.Get(BlockId.Leaves).BlocksSkyLight);
        Assert.False(BlockRegistry.Get(BlockId.BirchLeaves).BlocksSkyLight);
        Assert.False(BlockRegistry.Get(BlockId.SpruceLeaves).BlocksSkyLight);
    }

    [Fact]
    public void TorchBlocks_AreRegisteredForTorchRendering()
    {
        Assert.Equal(RenderMode.Torch, BlockRegistry.Get(BlockId.Torch).RenderMode);
        Assert.Equal(RenderMode.Torch, BlockRegistry.Get(BlockId.TorchWallNorth).RenderMode);
        Assert.Equal(RenderMode.Torch, BlockRegistry.Get(BlockId.TorchWallSouth).RenderMode);
        Assert.Equal(RenderMode.Torch, BlockRegistry.Get(BlockId.TorchWallWest).RenderMode);
        Assert.Equal(RenderMode.Torch, BlockRegistry.Get(BlockId.TorchWallEast).RenderMode);
        Assert.Equal(7f, RenderMaterial.Torch);
    }

    [Fact]
    public void FireBlock_UsesCrossRendering_AndEmitsLight()
    {
        var fire = BlockRegistry.Get(BlockId.Fire);

        Assert.Equal(RenderMode.Cross, fire.RenderMode);
        Assert.False(fire.IsSolid);
        Assert.False(fire.IsPlaceable);
        Assert.Equal(15, fire.LightEmission);
    }
}
