using System.Collections.Generic;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.World;
using Xunit;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Tests;

public sealed class FireSystemTests
{
    [Fact]
    public void Update_SpreadsFireToFlammableTreeBlock()
    {
        var world = new WorldType(123, new WorldGenSettings());
        var chunk = new Chunk(0, 0);
        chunk.SetBlock(0, 1, 1, BlockId.Stone);
        chunk.SetBlock(1, 0, 1, BlockId.Stone);
        chunk.SetBlock(1, 1, 0, BlockId.Stone);
        chunk.SetBlock(1, 1, 1, BlockId.Wood);
        chunk.SetBlock(1, 1, 2, BlockId.Stone);
        chunk.SetBlock(2, 1, 1, BlockId.Fire);
        world.SetChunk(chunk);

        var editor = new WorldEditor(world, new WorldChangeQueue());
        var system = new FireSystem(1, new FireConfig
        {
            Enabled = true,
            MaxUpdatesPerFrame = 8,
            MaxAgeSeconds = 10f,
            SpreadIntervalSeconds = 0.01f,
            SpreadChance = 1f,
            BurnStartSeconds = 10f,
            BurnChance = 0f,
            ExplosionIgniteRadius = 4.5f,
            ExplosionIgniteChance = 1f,
            RainExtinguishMultiplier = 0f
        });

        system.NotifyBlockChanged(new BlockChange(2, 1, 1, BlockId.Air, BlockId.Fire));
        system.Update(world, editor, 1f);
        var events = DrainEvents(system);

        Assert.Equal(BlockId.Wood, world.GetBlock(1, 1, 1));
        Assert.Equal(BlockId.Fire, world.GetBlock(1, 2, 1));
        Assert.Contains(events, fireEvent => fireEvent.Kind == FireEventKind.Crackle && fireEvent.Position == new Vector3(2.5f, 1.5f, 1.5f));
        Assert.Contains(events, fireEvent => fireEvent.Kind == FireEventKind.Ignited && fireEvent.Position == new Vector3(1.5f, 2.5f, 1.5f));
    }

    [Fact]
    public void Update_ConsumesFlammableBlock_AndEmitsEvent()
    {
        var world = new WorldType(123, new WorldGenSettings());
        var chunk = new Chunk(0, 0);
        chunk.SetBlock(0, 1, 1, BlockId.Stone);
        chunk.SetBlock(1, 0, 1, BlockId.Stone);
        chunk.SetBlock(1, 1, 0, BlockId.Stone);
        chunk.SetBlock(1, 1, 1, BlockId.Wood);
        chunk.SetBlock(1, 1, 2, BlockId.Stone);
        chunk.SetBlock(2, 1, 1, BlockId.Fire);
        world.SetChunk(chunk);

        var editor = new WorldEditor(world, new WorldChangeQueue());
        var system = new FireSystem(1, new FireConfig
        {
            Enabled = true,
            MaxUpdatesPerFrame = 8,
            MaxAgeSeconds = 10f,
            SpreadIntervalSeconds = 0.01f,
            SpreadChance = 1f,
            BurnStartSeconds = 10f,
            BurnChance = 1f,
            ExplosionIgniteRadius = 4.5f,
            ExplosionIgniteChance = 1f,
            RainExtinguishMultiplier = 0f
        });

        system.NotifyBlockChanged(new BlockChange(2, 1, 1, BlockId.Air, BlockId.Fire));
        system.Update(world, editor, 1f);
        var events = DrainEvents(system);

        Assert.Equal(BlockId.Air, world.GetBlock(1, 1, 1));
        Assert.Contains(events, fireEvent => fireEvent.Kind == FireEventKind.Consumed && fireEvent.Position == new Vector3(1.5f, 1.5f, 1.5f));
    }

    [Fact]
    public void IgniteExplosion_SeedsFireAboveFlammableBlock()
    {
        var world = new WorldType(123, new WorldGenSettings());
        var chunk = new Chunk(0, 0);
        chunk.SetBlock(0, 1, 1, BlockId.Stone);
        chunk.SetBlock(1, 0, 1, BlockId.Stone);
        chunk.SetBlock(1, 1, 0, BlockId.Stone);
        chunk.SetBlock(1, 1, 1, BlockId.Wood);
        chunk.SetBlock(1, 1, 2, BlockId.Stone);
        world.SetChunk(chunk);

        var editor = new WorldEditor(world, new WorldChangeQueue());
        var system = new FireSystem(1, new FireConfig
        {
            Enabled = true,
            MaxUpdatesPerFrame = 8,
            MaxAgeSeconds = 10f,
            SpreadIntervalSeconds = 0.01f,
            SpreadChance = 1f,
            BurnStartSeconds = 10f,
            BurnChance = 0f,
            ExplosionIgniteRadius = 4.5f,
            ExplosionIgniteChance = 1f,
            RainExtinguishMultiplier = 0f
        });

        int ignited = system.IgniteExplosion(world, editor, new Vector3(1f, 1f, 1f), 1, 1f);
        var events = DrainEvents(system);

        Assert.Equal(1, ignited);
        Assert.Equal(BlockId.Fire, world.GetBlock(1, 2, 1));
        Assert.Contains(events, fireEvent => fireEvent.Kind == FireEventKind.Ignited && fireEvent.Position == new Vector3(1.5f, 2.5f, 1.5f));
    }

    [Fact]
    public void IgniteExplosion_RespectsConfiguredIgnitionLimit()
    {
        var (world, editor, system) = CreateIgnitionSetup(maxEventQueue: 16, maxExplosionIgnitedBlocks: 2);

        int ignited = system.IgniteExplosion(world, editor, new Vector3(6f, 1f, 1f), 1, 1f);
        var events = DrainEvents(system);

        Assert.Equal(2, ignited);
        Assert.Equal(2, CountFireBlocks(world));
        Assert.Equal(2, CountIgnitedEvents(events));
    }

    [Fact]
    public void IgniteExplosion_DropsOldestEventsWhenQueueIsFull()
    {
        var (world, editor, system) = CreateIgnitionSetup(maxEventQueue: 2, maxExplosionIgnitedBlocks: 4);

        int ignited = system.IgniteExplosion(world, editor, new Vector3(6f, 1f, 1f), 1, 1f);
        var events = DrainEvents(system);

        Assert.Equal(3, ignited);
        Assert.Equal(2, events.Count);
        Assert.Equal(2, CountIgnitedEvents(events));
    }

    [Fact]
    public void Update_EmitsExtinguishedEvent_WhenFireRunsOut()
    {
        var world = new WorldType(123, new WorldGenSettings());
        var chunk = new Chunk(0, 0);
        chunk.SetBlock(0, 1, 1, BlockId.Stone);
        chunk.SetBlock(1, 0, 1, BlockId.Stone);
        chunk.SetBlock(1, 1, 0, BlockId.Stone);
        chunk.SetBlock(1, 1, 1, BlockId.Fire);
        chunk.SetBlock(1, 1, 2, BlockId.Stone);
        chunk.SetBlock(2, 1, 1, BlockId.Stone);
        world.SetChunk(chunk);

        var editor = new WorldEditor(world, new WorldChangeQueue());
        var system = new FireSystem(1, new FireConfig
        {
            Enabled = true,
            MaxUpdatesPerFrame = 8,
            MaxAgeSeconds = 0.5f,
            SpreadIntervalSeconds = 0.01f,
            SpreadChance = 0f,
            BurnStartSeconds = 0f,
            BurnChance = 0f,
            ExplosionIgniteRadius = 4.5f,
            ExplosionIgniteChance = 0f,
            RainExtinguishMultiplier = 0f
        });

        system.NotifyBlockChanged(new BlockChange(1, 1, 1, BlockId.Air, BlockId.Fire));
        system.Update(world, editor, 1f);
        var events = DrainEvents(system);

        Assert.Equal(BlockId.Air, world.GetBlock(1, 1, 1));
        Assert.Contains(events, fireEvent => fireEvent.Kind == FireEventKind.Extinguished && fireEvent.Position == new Vector3(1.5f, 1.5f, 1.5f));
    }

    private static List<FireEvent> DrainEvents(FireSystem system)
    {
        var events = new List<FireEvent>();
        while (system.TryDequeueEvent(out var fireEvent))
        {
            events.Add(fireEvent);
        }

        return events;
    }

    private static (WorldType world, WorldEditor editor, FireSystem system) CreateIgnitionSetup(int maxEventQueue, int maxExplosionIgnitedBlocks)
    {
        var world = new WorldType(123, new WorldGenSettings());
        var chunk = new Chunk(0, 0);
        PlaceIgnitionCandidate(chunk, 2);
        PlaceIgnitionCandidate(chunk, 6);
        PlaceIgnitionCandidate(chunk, 10);
        world.SetChunk(chunk);

        var editor = new WorldEditor(world, new WorldChangeQueue());
        var system = new FireSystem(1, new FireConfig
        {
            Enabled = true,
            MaxUpdatesPerFrame = 8,
            MaxEventQueue = maxEventQueue,
            MaxExplosionIgnitedBlocks = maxExplosionIgnitedBlocks,
            MaxAgeSeconds = 10f,
            SpreadIntervalSeconds = 0.01f,
            SpreadChance = 1f,
            BurnStartSeconds = 10f,
            BurnChance = 0f,
            ExplosionIgniteRadius = 4.5f,
            ExplosionIgniteChance = 1f,
            RainExtinguishMultiplier = 0f
        });

        return (world, editor, system);
    }

    private static void PlaceIgnitionCandidate(Chunk chunk, int x)
    {
        chunk.SetBlock(x, 1, 1, BlockId.Leaves);
        chunk.SetBlock(x - 1, 1, 1, BlockId.Stone);
        chunk.SetBlock(x + 1, 1, 1, BlockId.Stone);
        chunk.SetBlock(x, 0, 1, BlockId.Stone);
        chunk.SetBlock(x, 1, 0, BlockId.Stone);
        chunk.SetBlock(x, 1, 2, BlockId.Stone);
    }

    private static int CountFireBlocks(WorldType world)
    {
        int count = 0;
        foreach (int x in new[] { 2, 6, 10 })
        {
            if (world.GetBlock(x, 2, 1) == BlockId.Fire)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountIgnitedEvents(IEnumerable<FireEvent> events)
    {
        int count = 0;
        foreach (var fireEvent in events)
        {
            if (fireEvent.Kind == FireEventKind.Ignited)
            {
                count++;
            }
        }

        return count;
    }
}
