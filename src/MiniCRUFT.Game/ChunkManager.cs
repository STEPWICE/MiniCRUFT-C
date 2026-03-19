using System;
using System.Collections.Generic;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.IO;
using MiniCRUFT.Renderer;
using MiniCRUFT.World;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Game;

public sealed class ChunkManager
{
    private readonly WorldType _world;
    private readonly IChunkGenerationQueue _generator;
    private readonly IChunkLoadQueue _loader;
    private readonly IChunkRenderQueue _renderer;
    private readonly ChunkSaveQueue _saveQueue;
    private readonly SaveConfig _saveConfig;
    private readonly ChunkRequestTracker _chunkStates = new();
    private readonly HashSet<ChunkCoord> _renderRequested = new();
    private readonly List<ChunkCoord> _loadedThisFrame = new();
    private DateTime _lastMissingLog = DateTime.MinValue;

    public ChunkManager(WorldType world, IChunkGenerationQueue generator, IChunkLoadQueue loader, IChunkRenderQueue renderer, ChunkSaveQueue saveQueue, SaveConfig saveConfig)
    {
        _world = world;
        _generator = generator;
        _loader = loader;
        _renderer = renderer;
        _saveQueue = saveQueue;
        _saveConfig = saveConfig;
    }

    public void Update(Vector3 playerPosition, int renderRadius, int preloadExtra)
    {
        var playerChunk = WorldType.ToChunkCoord(MathUtil.FloorToInt(playerPosition.X), MathUtil.FloorToInt(playerPosition.Z));
        int generationRadius = renderRadius + Math.Max(0, preloadExtra);

        _loadedThisFrame.Clear();

        ProcessCompletedChunks();
        EnsureChunksAround(playerChunk, generationRadius);
        RefreshLoadedChunks();
        UpdateVisibleChunks(playerChunk, renderRadius);
        UnloadChunks(playerChunk, renderRadius);
    }

    public void SaveAll()
    {
        _saveQueue.EnqueueDirty(_world.Chunks);
    }

    public void ProcessChanges(IWorldChangeQueue changeQueue, ChunkSaveQueue saveQueue, FallingBlockSystem? fallingSystem = null, FluidSystem? fluidSystem = null, FireSystem? fireSystem = null, TntSystem? tntSystem = null)
    {
        var refreshCoords = new HashSet<ChunkCoord>();
        int budget = Math.Max(1, _saveConfig.MaxBlockChangesPerFrame);
        while (budget-- > 0 && changeQueue.TryDequeue(out var change))
        {
            fallingSystem?.NotifyBlockChanged(change);
            fluidSystem?.NotifyBlockChanged(change);
            fireSystem?.NotifyBlockChanged(change);
            tntSystem?.NotifyBlockChanged(change);
            var coord = WorldType.ToChunkCoord(change.X, change.Z);
            var chunk = _world.GetChunk(coord);
            if (chunk != null)
            {
                saveQueue.Enqueue(chunk);
                refreshCoords.Add(coord);
                AddRefreshCoords(refreshCoords, change, coord);
            }
        }

        foreach (var coord in refreshCoords)
        {
            var chunk = _world.GetChunk(coord);
            if (chunk == null)
            {
                continue;
            }

            _renderer.EnqueueChunk(chunk, ChunkNeighborhood.FromWorld(_world, chunk), highPriority: true);
        }
    }

    private void EnsureChunksAround(ChunkCoord center, int radius)
    {
        int queued = 0;
        for (int dz = -radius; dz <= radius; dz++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                var coord = new ChunkCoord(center.X + dx, center.Z + dz);
                var chunk = _world.GetChunk(coord);
                if (chunk != null)
                {
                    if (_chunkStates.MarkObservedLoaded(coord))
                    {
                        _loadedThisFrame.Add(coord);
                    }
                    continue;
                }

                if (_chunkStates.IsPending(coord))
                {
                    continue;
                }

                if (!_chunkStates.TryMarkRequested(coord))
                {
                    continue;
                }

                try
                {
                    _loader.Enqueue(coord);
                    _chunkStates.TryMarkInFlight(coord);
                    queued++;
                }
                catch
                {
                    _chunkStates.Forget(coord);
                    throw;
                }
            }
        }

        if (queued > 0)
        {
            var now = DateTime.UtcNow;
            if ((now - _lastMissingLog).TotalSeconds > 2)
            {
                _lastMissingLog = now;
                Log.Info($"Chunk requests queued: {queued} around {center.X},{center.Z}.");
            }
        }
    }

    private void RefreshLoadedChunks()
    {
        for (int i = 0; i < _loadedThisFrame.Count; i++)
        {
            RefreshNeighborChunks(_loadedThisFrame[i]);
        }
    }

    private void RefreshNeighborChunks(ChunkCoord coord)
    {
        MarkNeighborChunkDirty(coord.X, coord.Z - 1);
        MarkNeighborChunkDirty(coord.X, coord.Z + 1);
        MarkNeighborChunkDirty(coord.X + 1, coord.Z);
        MarkNeighborChunkDirty(coord.X - 1, coord.Z);
    }

    private void MarkNeighborChunkDirty(int chunkX, int chunkZ)
    {
        var chunk = _world.GetChunk(new ChunkCoord(chunkX, chunkZ));
        if (chunk == null)
        {
            return;
        }

        chunk.MarkDirty();
        chunk.MarkLightingDirty();
    }

    private void UpdateVisibleChunks(ChunkCoord center, int renderRadius)
    {
        for (int dz = -renderRadius; dz <= renderRadius; dz++)
        {
            for (int dx = -renderRadius; dx <= renderRadius; dx++)
            {
                var coord = new ChunkCoord(center.X + dx, center.Z + dz);
                var chunk = _world.GetChunk(coord);
                if (chunk == null)
                {
                    continue;
                }

                bool firstRender = _renderRequested.Add(coord);
                if (!chunk.IsDirty && !firstRender)
                {
                    continue;
                }

                _renderer.EnqueueChunk(chunk, ChunkNeighborhood.FromWorld(_world, chunk), highPriority: true);
            }
        }
    }

    private void UnloadChunks(ChunkCoord center, int renderRadius)
    {
        int unloadRadius = renderRadius + _saveConfig.UnloadExtraRadius;
        foreach (var chunk in _world.Chunks)
        {
            int dx = chunk.ChunkX - center.X;
            int dz = chunk.ChunkZ - center.Z;
            if (Math.Abs(dx) <= unloadRadius && Math.Abs(dz) <= unloadRadius)
            {
                continue;
            }

            var coord = new ChunkCoord(chunk.ChunkX, chunk.ChunkZ);
            _saveQueue.Enqueue(chunk);
            _renderer.RemoveChunkMesh(coord);
            _world.RemoveChunk(coord);
            _chunkStates.Forget(coord);
            _renderRequested.Remove(coord);
        }
    }

    public void NotifyBlockChanged(int worldX, int worldY, int worldZ)
    {
        var chunkCoord = WorldType.ToChunkCoord(worldX, worldZ);
        var chunk = _world.GetChunk(chunkCoord);
        if (chunk != null)
        {
            _renderer.EnqueueChunk(chunk, ChunkNeighborhood.FromWorld(_world, chunk), highPriority: true);
        }

        var (localX, localZ) = WorldType.ToLocalCoord(worldX, worldZ);
        if (localX == 0)
        {
            EnqueueNeighbor(chunkCoord.X - 1, chunkCoord.Z);
        }
        else if (localX == Chunk.SizeX - 1)
        {
            EnqueueNeighbor(chunkCoord.X + 1, chunkCoord.Z);
        }

        if (localZ == 0)
        {
            EnqueueNeighbor(chunkCoord.X, chunkCoord.Z - 1);
        }
        else if (localZ == Chunk.SizeZ - 1)
        {
            EnqueueNeighbor(chunkCoord.X, chunkCoord.Z + 1);
        }
    }

    private static void AddRefreshCoords(HashSet<ChunkCoord> refreshCoords, BlockChange change, ChunkCoord chunkCoord)
    {
        var (localX, localZ) = WorldType.ToLocalCoord(change.X, change.Z);

        if (localX == 0)
        {
            refreshCoords.Add(new ChunkCoord(chunkCoord.X - 1, chunkCoord.Z));
        }
        else if (localX == Chunk.SizeX - 1)
        {
            refreshCoords.Add(new ChunkCoord(chunkCoord.X + 1, chunkCoord.Z));
        }

        if (localZ == 0)
        {
            refreshCoords.Add(new ChunkCoord(chunkCoord.X, chunkCoord.Z - 1));
        }
        else if (localZ == Chunk.SizeZ - 1)
        {
            refreshCoords.Add(new ChunkCoord(chunkCoord.X, chunkCoord.Z + 1));
        }
    }

    private void EnqueueNeighbor(int chunkX, int chunkZ)
    {
        var coord = new ChunkCoord(chunkX, chunkZ);
        var chunk = _world.GetChunk(coord);
        if (chunk == null)
        {
            return;
        }

        _renderer.EnqueueChunk(chunk, ChunkNeighborhood.FromWorld(_world, chunk), highPriority: true);
    }

    private void ProcessCompletedChunks()
    {
        while (_loader.TryDequeueCompletedChunk(out Chunk? loadedChunk))
        {
            if (loadedChunk == null)
            {
                continue;
            }

            var coord = new ChunkCoord(loadedChunk.ChunkX, loadedChunk.ChunkZ);
            var existing = _world.GetChunk(coord);
            if (existing != null)
            {
                if (_chunkStates.MarkObservedLoaded(coord))
                {
                    _loadedThisFrame.Add(coord);
                }
                continue;
            }

            if (!_chunkStates.TryAcceptGenerated(coord))
            {
                continue;
            }

            _world.SetChunk(loadedChunk);
            _loadedThisFrame.Add(coord);
        }

        while (_loader.TryDequeueMissingChunk(out ChunkCoord missingCoord))
        {
            if (_world.GetChunk(missingCoord) != null)
            {
                _chunkStates.MarkObservedLoaded(missingCoord);
                continue;
            }

            if (_chunkStates.IsPending(missingCoord))
            {
                try
                {
                    _generator.Enqueue(missingCoord);
                }
                catch
                {
                    _chunkStates.Forget(missingCoord);
                    throw;
                }
            }
        }

        while (_generator.TryDequeueCompletedChunk(out Chunk? chunk))
        {
            if (chunk == null)
            {
                continue;
            }

            var coord = new ChunkCoord(chunk.ChunkX, chunk.ChunkZ);
            var existing = _world.GetChunk(coord);
            if (existing != null)
            {
                if (_chunkStates.MarkObservedLoaded(coord))
                {
                    _loadedThisFrame.Add(coord);
                }
                continue;
            }

            if (!_chunkStates.TryAcceptGenerated(coord))
            {
                continue;
            }

            _world.SetChunk(chunk);
            _loadedThisFrame.Add(coord);
        }
    }
}
