using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.Game;
using MiniCRUFT.World;
using WorldType = MiniCRUFT.World.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class WorldRaycasterTests
{
    [Fact]
    public void Raycast_HitsBlockAndAdjacent_PositiveDirection()
    {
        var settings = new WorldGenSettings();
        var world = new WorldType(0, settings);
        var chunk = new Chunk(0, 0);
        chunk.SetBlock(2, 1, 1, BlockId.Stone);
        world.SetChunk(chunk);

        var origin = new Vector3(0.5f, 1.5f, 1.5f);
        var result = WorldRaycaster.Raycast(world, origin, Vector3.UnitX, 10f);

        Assert.True(result.Hit);
        Assert.Equal(new BlockCoord(2, 1, 1), result.Block);
        Assert.Equal(new BlockCoord(1, 1, 1), result.Adjacent);
    }

    [Fact]
    public void Raycast_HitsBlockAndAdjacent_NegativeDirection()
    {
        var settings = new WorldGenSettings();
        var world = new WorldType(0, settings);
        var chunk = new Chunk(0, 0);
        chunk.SetBlock(0, 1, 1, BlockId.Stone);
        world.SetChunk(chunk);

        var origin = new Vector3(2.5f, 1.5f, 1.5f);
        var result = WorldRaycaster.Raycast(world, origin, -Vector3.UnitX, 10f);

        Assert.True(result.Hit);
        Assert.Equal(new BlockCoord(0, 1, 1), result.Block);
        Assert.Equal(new BlockCoord(1, 1, 1), result.Adjacent);
    }
}
