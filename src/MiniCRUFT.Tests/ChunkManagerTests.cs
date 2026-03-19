using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.Game;
using MiniCRUFT.IO;
using MiniCRUFT.Renderer;
using MiniCRUFT.World;
using WorldType = MiniCRUFT.World.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class ChunkManagerTests
{
    [Fact]
    public void Update_QueuesMissingChunkOnlyOnce()
    {
        var world = new WorldType(42, new WorldGenSettings());
        var generator = new RecordingChunkGenerationQueue();
        var loadQueue = new RecordingChunkLoadQueue();
        var storage = new NullChunkStorage();
        using var saveQueue = new ChunkSaveQueue(storage, 1);
        var renderer = new RecordingChunkRenderQueue();
        var manager = new ChunkManager(world, generator, loadQueue, renderer, saveQueue, new SaveConfig());

        manager.Update(Vector3.Zero, renderRadius: 0, preloadExtra: 0);
        manager.Update(Vector3.Zero, renderRadius: 0, preloadExtra: 0);
        manager.Update(Vector3.Zero, renderRadius: 0, preloadExtra: 0);

        Assert.Equal(1, loadQueue.EnqueueCount);
        Assert.Single(loadQueue.EnqueuedCoords);
        Assert.Empty(renderer.EnqueuedCoords);
    }

    [Fact]
    public void Update_RefreshesVisibleNeighborWhenChunkAppears()
    {
        var world = new WorldType(42, new WorldGenSettings());
        var generator = new RecordingChunkGenerationQueue();
        var loadQueue = new RecordingChunkLoadQueue();
        var storage = new NullChunkStorage();
        using var saveQueue = new ChunkSaveQueue(storage, 1);
        var renderer = new RecordingChunkRenderQueue();
        var manager = new ChunkManager(world, generator, loadQueue, renderer, saveQueue, new SaveConfig());

        var center = new Chunk(0, 0);
        center.ClearDirty();
        center.ClearLightingDirty();
        world.SetChunk(center);

        manager.Update(new Vector3(8.5f, 64f, 8.5f), renderRadius: 1, preloadExtra: 0);

        center.ClearDirty();
        center.ClearLightingDirty();

        var east = new Chunk(1, 0);
        east.ClearDirty();
        east.ClearLightingDirty();
        world.SetChunk(east);

        manager.Update(new Vector3(8.5f, 64f, 8.5f), renderRadius: 1, preloadExtra: 0);

        Assert.True(center.IsDirty);
        Assert.True(center.LightingDirty);
        Assert.Equal(2, renderer.Count(new ChunkCoord(0, 0)));
        Assert.Equal(1, renderer.Count(new ChunkCoord(1, 0)));
    }

    [Fact]
    public void ProcessChanges_QueuesChangedChunkForRenderUpdate()
    {
        var world = new WorldType(42, new WorldGenSettings());
        var generator = new RecordingChunkGenerationQueue();
        var loadQueue = new RecordingChunkLoadQueue();
        var storage = new NullChunkStorage();
        using var saveQueue = new ChunkSaveQueue(storage, 1);
        var renderer = new RecordingChunkRenderQueue();
        var manager = new ChunkManager(world, generator, loadQueue, renderer, saveQueue, new SaveConfig());
        var changeQueue = new WorldChangeQueue();
        var editor = new WorldEditor(world, changeQueue);

        var chunk = new Chunk(0, 0);
        world.SetChunk(chunk);
        chunk.SetBlock(1, 10, 1, BlockId.Stone);
        chunk.ClearDirty();
        chunk.ClearLightingDirty();

        Assert.True(editor.SetBlock(1, 10, 1, BlockId.Air));

        manager.ProcessChanges(changeQueue, saveQueue);

        Assert.Equal(1, renderer.EnqueueCount);
        Assert.Equal(0, renderer.RefreshCount);
    }

    private sealed class RecordingChunkGenerationQueue : IChunkGenerationQueue
    {
        public int EnqueueCount => EnqueuedCoords.Count;
        public List<ChunkCoord> EnqueuedCoords { get; } = new();

        public void Enqueue(ChunkCoord coord)
        {
            EnqueuedCoords.Add(coord);
        }

        public bool TryDequeueCompletedChunk([NotNullWhen(true)] out Chunk? chunk)
        {
            chunk = null!;
            return false;
        }
    }

    private sealed class RecordingChunkLoadQueue : IChunkLoadQueue
    {
        public int EnqueueCount => EnqueuedCoords.Count;
        public List<ChunkCoord> EnqueuedCoords { get; } = new();

        public void Enqueue(ChunkCoord coord)
        {
            EnqueuedCoords.Add(coord);
        }

        public bool TryDequeueCompletedChunk([NotNullWhen(true)] out Chunk? chunk)
        {
            chunk = null!;
            return false;
        }

        public bool TryDequeueMissingChunk(out ChunkCoord coord)
        {
            coord = default;
            return false;
        }
    }

    private sealed class RecordingChunkRenderQueue : IChunkRenderQueue
    {
        public List<ChunkCoord> EnqueuedCoords { get; } = new();
        public int EnqueueCount => EnqueuedCoords.Count;
        public int RefreshCount { get; private set; }

        public void EnqueueChunk(Chunk chunk, ChunkNeighborhood neighbors, bool highPriority = false)
        {
            EnqueuedCoords.Add(new ChunkCoord(chunk.ChunkX, chunk.ChunkZ));
        }

        public void RefreshChunk(Chunk chunk, ChunkNeighborhood neighbors)
        {
            RefreshCount++;
        }

        public void RemoveChunkMesh(ChunkCoord coord)
        {
        }

        public int Count(ChunkCoord coord)
        {
            return EnqueuedCoords.Count(value => value.Equals(coord));
        }
    }

    private sealed class NullChunkStorage : IChunkStorage
    {
        public void SaveChunk(Chunk chunk)
        {
        }

        public Chunk? LoadChunk(ChunkCoord coord)
        {
            return null;
        }
    }
}
