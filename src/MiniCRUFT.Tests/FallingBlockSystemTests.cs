using MiniCRUFT.Core;
using MiniCRUFT.World;
using Xunit;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Tests;

public sealed class FallingBlockSystemTests
{
    [Fact]
    public void Update_DropsSandThroughAirAndWaterToSupport()
    {
        var world = new WorldType(123, new WorldGenSettings());
        var chunk = new Chunk(0, 0);
        chunk.SetBlock(1, 0, 1, BlockId.Stone);
        chunk.SetBlock(1, 1, 1, BlockId.Water);
        chunk.SetBlock(1, 3, 1, BlockId.Sand);
        world.SetChunk(chunk);

        var editor = new WorldEditor(world, new WorldChangeQueue());
        var system = new FallingBlockSystem(new FallingConfig
        {
            Enabled = true,
            MaxUpdatesPerFrame = 64
        });

        system.NotifyBlockChanged(new BlockChange(1, 3, 1, BlockId.Air, BlockId.Sand));
        system.Update(world, editor);

        Assert.Equal(BlockId.Air, world.GetBlock(1, 3, 1));
        Assert.Equal(BlockId.Sand, world.GetBlock(1, 1, 1));
        Assert.Equal(BlockId.Stone, world.GetBlock(1, 0, 1));
    }
}
