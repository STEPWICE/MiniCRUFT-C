using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class WorldLightingEdgeTests
{
    [Fact]
    public void TorchLight_PropagatesAcrossChunkEdge()
    {
        BlockRegistry.Initialize();
        var west = new Chunk(0, 0);
        var east = new Chunk(1, 0);

        west.SetBlock(Chunk.SizeX - 1, 10, 8, BlockId.Torch);
        WorldLighting.RecalculateChunkLighting(west);

        WorldLighting.RecalculateChunkLighting(east, null, null, null, west);

        byte light = east.GetTorchLight(0, 10, 8);
        Assert.True(light > 0);
    }
}
