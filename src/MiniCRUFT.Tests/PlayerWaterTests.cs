using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.Game;
using MiniCRUFT.World;
using Xunit;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Tests;

public sealed class PlayerWaterTests
{
    [Fact]
    public void Update_EntersWaterAndSwimsUpward()
    {
        WorldType world = CreateWaterPoolWorld();
        var player = new Player(new Vector3(8.5f, 64.1f, 8.5f), new PhysicsConfig());
        var input = new InputState
        {
            Jump = true
        };

        float startY = player.Position.Y;

        player.Update(0.1f, input, world, 6f, 0f);

        Assert.True(player.InWater);
        Assert.False(player.InLava);
        Assert.True(player.WaterCoverage > 0f);
        Assert.True(player.Velocity.Y > 0f);
        Assert.True(player.Position.Y > startY);
    }

    [Fact]
    public void Update_IsPushedByFlowingWaterCurrent()
    {
        WorldType world = CreateFlowingWaterWorld();
        var physics = new PhysicsConfig
        {
            WaterCurrentMultiplier = 2.5f
        };
        var player = new Player(new Vector3(7.5f, 64.1f, 8.5f), physics);
        var input = new InputState();

        float startX = player.Position.X;

        player.Update(0.2f, input, world, 0f, 0f);

        Assert.True(player.InWater);
        Assert.True(player.Position.X > startX + 0.02f, $"Expected flowing water to push the player east, but got {player.Position}.");
    }

    private static WorldType CreateWaterPoolWorld()
    {
        var world = new WorldType(4242, new WorldGenSettings());
        var chunk = new Chunk(0, 0);

        for (int x = 0; x < Chunk.SizeX; x++)
        {
            for (int z = 0; z < Chunk.SizeZ; z++)
            {
                chunk.SetBlock(x, 63, z, BlockId.Stone);
            }
        }

        for (int x = 6; x <= 10; x++)
        {
            for (int z = 6; z <= 10; z++)
            {
                chunk.SetBlock(x, 64, z, BlockId.Water);
                chunk.SetBlock(x, 65, z, BlockId.Water1);
            }
        }

        world.SetChunk(chunk);
        return world;
    }

    private static WorldType CreateFlowingWaterWorld()
    {
        var world = new WorldType(4242, new WorldGenSettings());
        var chunk = new Chunk(0, 0);

        for (int x = 0; x < Chunk.SizeX; x++)
        {
            for (int z = 0; z < Chunk.SizeZ; z++)
            {
                chunk.SetBlock(x, 63, z, BlockId.Stone);
            }
        }

        BlockId[] levels =
        {
            BlockId.Water,
            BlockId.Water1,
            BlockId.Water2,
            BlockId.Water3,
            BlockId.Water4
        };

        for (int i = 0; i < levels.Length; i++)
        {
            int x = 6 + i;
            chunk.SetBlock(x, 64, 8, levels[i]);
            chunk.SetBlock(x, 65, 8, levels[i]);
        }

        world.SetChunk(chunk);
        return world;
    }
}
