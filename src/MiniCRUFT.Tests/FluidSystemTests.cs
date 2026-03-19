using MiniCRUFT.Core;
using MiniCRUFT.World;
using Xunit;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Tests;

public sealed class FluidSystemTests
{
    [Fact]
    public void Update_PropagatesWaterDownwardIntoOpenSpace()
    {
        var world = new WorldType(123, new WorldGenSettings());
        var chunk = new Chunk(0, 0);
        chunk.SetBlock(1, 0, 1, BlockId.Stone);
        chunk.SetBlock(1, 2, 1, BlockId.Water);
        world.SetChunk(chunk);

        var editor = new WorldEditor(world, new WorldChangeQueue());
        var system = new FluidSystem(new FluidConfig
        {
            Enabled = true,
            MaxUpdatesPerFrame = 64,
            WaterMaxSpreadLevel = 7,
            LavaMaxSpreadLevel = 4,
            LavaUpdatesPerFrame = 64,
            LavaUpdateIntervalFrames = 1,
            InfiniteSources = true,
            ReplaceNonSolid = true
        });

        system.NotifyBlockChanged(new BlockChange(1, 2, 1, BlockId.Air, BlockId.Water));
        system.Update(world, editor);

        Assert.Equal(BlockId.Water1, world.GetBlock(1, 1, 1));
        Assert.Equal(BlockId.Water, world.GetBlock(1, 2, 1));
    }

    [Fact]
    public void Update_RemovesIsolatedFlowingWaterWithoutNeighbors()
    {
        var world = new WorldType(123, new WorldGenSettings());
        var chunk = new Chunk(0, 0);
        chunk.SetBlock(1, 0, 1, BlockId.Stone);
        chunk.SetBlock(1, 1, 1, BlockId.Water1);
        world.SetChunk(chunk);

        var editor = new WorldEditor(world, new WorldChangeQueue());
        var system = new FluidSystem(new FluidConfig
        {
            Enabled = true,
            MaxUpdatesPerFrame = 64,
            WaterMaxSpreadLevel = 7,
            LavaMaxSpreadLevel = 4,
            LavaUpdatesPerFrame = 64,
            LavaUpdateIntervalFrames = 1,
            InfiniteSources = false,
            ReplaceNonSolid = true
        });

        system.NotifyBlockChanged(new BlockChange(1, 1, 1, BlockId.Water, BlockId.Water1));
        system.Update(world, editor);

        Assert.Equal(BlockId.Air, world.GetBlock(1, 1, 1));
    }

    [Fact]
    public void Update_PropagatesLavaDownwardIntoOpenSpace()
    {
        var world = new WorldType(123, new WorldGenSettings());
        var chunk = new Chunk(0, 0);
        chunk.SetBlock(1, 0, 1, BlockId.Stone);
        chunk.SetBlock(1, 2, 1, BlockId.Lava);
        world.SetChunk(chunk);

        var editor = new WorldEditor(world, new WorldChangeQueue());
        var system = new FluidSystem(new FluidConfig
        {
            Enabled = true,
            MaxUpdatesPerFrame = 64,
            WaterMaxSpreadLevel = 7,
            LavaMaxSpreadLevel = 4,
            LavaUpdatesPerFrame = 64,
            LavaUpdateIntervalFrames = 1,
            InfiniteSources = true,
            ReplaceNonSolid = true
        });

        system.NotifyBlockChanged(new BlockChange(1, 2, 1, BlockId.Air, BlockId.Lava));
        system.Update(world, editor);

        Assert.Equal(BlockId.Lava1, world.GetBlock(1, 1, 1));
        Assert.Equal(BlockId.Lava, world.GetBlock(1, 2, 1));
    }

    [Fact]
    public void Update_HardensLavaToObsidianOrCobblestone_WhenTouchedByWater()
    {
        var world = new WorldType(123, new WorldGenSettings());
        var chunk = new Chunk(0, 0);
        chunk.SetBlock(1, 0, 1, BlockId.Stone);
        chunk.SetBlock(1, 1, 1, BlockId.Lava);
        chunk.SetBlock(2, 1, 1, BlockId.Water);
        world.SetChunk(chunk);

        var editor = new WorldEditor(world, new WorldChangeQueue());
        var system = new FluidSystem(new FluidConfig
        {
            Enabled = true,
            MaxUpdatesPerFrame = 64,
            WaterMaxSpreadLevel = 7,
            LavaMaxSpreadLevel = 4,
            LavaUpdatesPerFrame = 64,
            LavaUpdateIntervalFrames = 1,
            InfiniteSources = true,
            ReplaceNonSolid = true,
            LavaHardensOnWaterContact = true
        });

        system.NotifyBlockChanged(new BlockChange(2, 1, 1, BlockId.Air, BlockId.Water));
        system.Update(world, editor);

        Assert.Equal(BlockId.Obsidian, world.GetBlock(1, 1, 1));
    }
}
