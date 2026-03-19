using System.Collections.Generic;
using MiniCRUFT.Core;
using MiniCRUFT.Game;
using MiniCRUFT.World;
using WorldType = MiniCRUFT.World.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class TntSystemTests
{
    [Fact]
    public void Prime_ReprimingWithShorterFuse_UpdatesFuseDuration()
    {
        BlockRegistry.Initialize();
        var config = new TntConfig
        {
            Enabled = true,
            PrimeOnPlace = true,
            FuseSeconds = 4f,
            ChainReactionFuseSeconds = 0.35f,
            ExplosionRadius = 4.5f,
            ExplosionDamage = 20,
            KnockbackStrength = 8f,
            ResistanceScale = 0.35f,
            MaxAffectedBlocks = 512
        };

        var world = new WorldType(42, new WorldGenSettings());
        var chunk = new Chunk(0, 0);
        world.SetChunk(chunk);
        world.SetBlock(1, 10, 1, BlockId.Tnt);

        var system = new TntSystem(config);
        Assert.True(system.Prime(world, new BlockCoord(1, 10, 1), 4f, emitEvent: false));
        Assert.True(system.Prime(world, new BlockCoord(1, 10, 1), 1f, emitEvent: false));

        var renderInstances = new List<TntRenderInstance>();
        system.FillRenderInstances(renderInstances);

        Assert.Single(renderInstances);
        Assert.Equal(1f, renderInstances[0].FuseDuration);
        Assert.Equal(1f, renderInstances[0].FuseRemaining);
    }

    [Fact]
    public void Prime_RespectsConfiguredPrimedTntLimit()
    {
        BlockRegistry.Initialize();
        var config = new TntConfig
        {
            Enabled = true,
            PrimeOnPlace = true,
            FuseSeconds = 4f,
            ChainReactionFuseSeconds = 0.35f,
            ExplosionRadius = 4.5f,
            ExplosionDamage = 20,
            KnockbackStrength = 8f,
            ResistanceScale = 0.35f,
            MaxAffectedBlocks = 512,
            MaxPrimedTnt = 2,
            MaxEventQueue = 4
        };

        var world = new WorldType(42, new WorldGenSettings());
        var chunk = new Chunk(0, 0);
        world.SetChunk(chunk);
        world.SetBlock(1, 10, 1, BlockId.Tnt);
        world.SetBlock(2, 10, 1, BlockId.Tnt);
        world.SetBlock(3, 10, 1, BlockId.Tnt);

        var system = new TntSystem(config);
        Assert.True(system.Prime(world, new BlockCoord(1, 10, 1), 4f, emitEvent: false));
        Assert.True(system.Prime(world, new BlockCoord(2, 10, 1), 4f, emitEvent: false));
        Assert.False(system.Prime(world, new BlockCoord(3, 10, 1), 4f, emitEvent: false));

        var renderInstances = new List<TntRenderInstance>();
        system.FillRenderInstances(renderInstances);

        Assert.Equal(2, renderInstances.Count);
    }
}
