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
    private readonly ChunkGenerationWorker _generator;
    private readonly IChunkStorage _storage;
    private readonly WorldRenderer _renderer;
    private readonly ChunkSaveQueue _saveQueue;
    private readonly SaveConfig _saveConfig;
    private readonly HashSet<ChunkCoord> _requested = new();
    private readonly HashSet<ChunkCoord> _renderRequested = new();
    private readonly HashSet<ChunkCoord> _knownChunks = new();
    private readonly HashSet<ChunkCoord> _missingLogged = new();

    public ChunkManager(WorldType world, ChunkGenerationWorker generator, IChunkStorage storage, WorldRenderer renderer, ChunkSaveQueue saveQueue, SaveConfig saveConfig)
    {
        _world = world;
        _generator = generator;
        _storage = storage;
        _renderer = renderer;
        _saveQueue = saveQueue;
        _saveConfig = saveConfig;
    }

    public void Update(Vector3 playerPosition, int renderRadius, int preloadExtra)
    {
        var playerChunk = WorldType.ToChunkCoord(MathUtil.FloorToInt(playerPosition.X), MathUtil.FloorToInt(playerPosition.Z));
        int genRadius = renderRadius + Math.Max(0, preloadExtra);

        foreach (var coord in EnumerateSpiral(genRadius))
        {
            var worldCoord = new ChunkCoord(playerChunk.X + coord.X, playerChunk.Z + coord.Z);
            if (_world.GetChunk(worldCoord) != null)
            {
                if (IsWithinRadius(coord.X, coord.Z, renderRadius) && !_renderRequested.Contains(worldCoord))
                {
                    var chunk = _world.GetChunk(worldCoord);
                    if (chunk != null)
                    {
                        _renderer.EnqueueChunk(chunk, ChunkNeighborhood.FromWorld(_world, chunk));
                        _renderRequested.Add(worldCoord);
                    }
                }
                continue;
            }

            if (_requested.Contains(worldCoord))
            {
                continue;
            }

            var loaded = _storage.LoadChunk(worldCoord);
            if (loaded != null)
            {
                _world.SetChunk(loaded);
                if (IsWithinRadius(coord.X, coord.Z, renderRadius))
                {
                    _renderer.EnqueueChunk(loaded, ChunkNeighborhood.FromWorld(_world, loaded));
                    _renderRequested.Add(worldCoord);
                }
            }
            else
            {
                _generator.Enqueue(worldCoord);
            }

            _requested.Add(worldCoord);
        }

        for (int dz = -renderRadius; dz <= renderRadius; dz++)
        {
            for (int dx = -renderRadius; dx <= renderRadius; dx++)
            {
                var coord = new ChunkCoord(playerChunk.X + dx, playerChunk.Z + dz);
                var chunk = _world.GetChunk(coord);
                if (chunk == null)
                {
                    continue;
                }

                if (_knownChunks.Add(coord))
                {
                    MarkNeighborDirty(coord);
                }

                if (chunk.IsDirty)
                {
                    _renderer.EnqueueChunk(chunk, ChunkNeighborhood.FromWorld(_world, chunk));
                }
            }
        }

        EnsureChunksAround(playerChunk, renderRadius);
        UnloadChunks(playerChunk, renderRadius);
    }

    public void SaveAll()
    {
        _saveQueue.EnqueueDirty(_world.Chunks);
    }

    public void ProcessChanges(IWorldChangeQueue changeQueue, ChunkSaveQueue saveQueue)
    {
        while (changeQueue.TryDequeue(out var change))
        {
            NotifyBlockChanged(change.X, change.Y, change.Z);
            var coord = WorldType.ToChunkCoord(change.X, change.Z);
            var chunk = _world.GetChunk(coord);
            if (chunk != null)
            {
                saveQueue.Enqueue(chunk);
            }
        }
    }

    private static IEnumerable<(int X, int Z)> EnumerateSpiral(int radius)
    {
        yield return (0, 0);
        for (int r = 1; r <= radius; r++)
        {
            for (int x = -r; x <= r; x++)
            {
                yield return (x, -r);
                if (r != 0)
                {
                    yield return (x, r);
                }
            }
            for (int z = -r + 1; z <= r - 1; z++)
            {
                yield return (-r, z);
                if (r != 0)
                {
                    yield return (r, z);
                }
            }
        }
    }

    private static bool IsWithinRadius(int dx, int dz, int radius)
    {
        return Math.Abs(dx) <= radius && Math.Abs(dz) <= radius;
    }

    private void MarkNeighborDirty(ChunkCoord coord)
    {
        var north = _world.GetChunk(new ChunkCoord(coord.X, coord.Z - 1));
        var south = _world.GetChunk(new ChunkCoord(coord.X, coord.Z + 1));
        var east = _world.GetChunk(new ChunkCoord(coord.X + 1, coord.Z));
        var west = _world.GetChunk(new ChunkCoord(coord.X - 1, coord.Z));

        north?.MarkDirty();
        south?.MarkDirty();
        east?.MarkDirty();
        west?.MarkDirty();
    }

    private void EnsureChunksAround(ChunkCoord center, int radius)
    {
        for (int dz = -radius; dz <= radius; dz++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                var coord = new ChunkCoord(center.X + dx, center.Z + dz);
                if (_world.GetChunk(coord) != null)
                {
                    continue;
                }

                _generator.Enqueue(coord);
                if (_missingLogged.Add(coord))
                {
                    Log.Warn($"Missing chunk {coord.X},{coord.Z} detected. Re-queued for generation.");
                }
            }
        }
    }

    private void UnloadChunks(ChunkCoord center, int renderRadius)
    {
        int unloadRadius = Math.Max(renderRadius, renderRadius + _saveConfig.UnloadExtraRadius);
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
            _requested.Remove(coord);
            _renderRequested.Remove(coord);
            _knownChunks.Remove(coord);
            _missingLogged.Remove(coord);
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
}
