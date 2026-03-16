using System.Collections.Concurrent;
using MiniCRUFT.Core;

namespace MiniCRUFT.World;

public sealed class World
{
    private readonly ConcurrentDictionary<ChunkCoord, Chunk> _chunks = new();

    public int Seed { get; }
    public WorldGenSettings Settings { get; }

    public World(int seed, WorldGenSettings settings)
    {
        Seed = seed;
        Settings = settings;
        BlockRegistry.Initialize();
        BiomeRegistry.Initialize();
    }

    public IEnumerable<Chunk> Chunks => _chunks.Values;

    public bool TryGetChunk(ChunkCoord coord, out Chunk? chunk) => _chunks.TryGetValue(coord, out chunk);

    public void SetChunk(Chunk chunk)
    {
        _chunks[new ChunkCoord(chunk.ChunkX, chunk.ChunkZ)] = chunk;
    }

    public bool RemoveChunk(ChunkCoord coord)
    {
        return _chunks.TryRemove(coord, out _);
    }

    public Chunk? GetChunk(ChunkCoord coord)
    {
        _chunks.TryGetValue(coord, out var chunk);
        return chunk;
    }

    public static ChunkCoord ToChunkCoord(int worldX, int worldZ)
    {
        int chunkX = worldX >= 0 ? worldX / Chunk.SizeX : (worldX - (Chunk.SizeX - 1)) / Chunk.SizeX;
        int chunkZ = worldZ >= 0 ? worldZ / Chunk.SizeZ : (worldZ - (Chunk.SizeZ - 1)) / Chunk.SizeZ;
        return new ChunkCoord(chunkX, chunkZ);
    }

    public static (int localX, int localZ) ToLocalCoord(int worldX, int worldZ)
    {
        int localX = worldX - MathUtil.FloorToInt((float)worldX / Chunk.SizeX) * Chunk.SizeX;
        int localZ = worldZ - MathUtil.FloorToInt((float)worldZ / Chunk.SizeZ) * Chunk.SizeZ;
        if (localX < 0) localX += Chunk.SizeX;
        if (localZ < 0) localZ += Chunk.SizeZ;
        return (localX, localZ);
    }

    public bool HasChunkAt(int worldX, int worldZ)
    {
        var chunkCoord = ToChunkCoord(worldX, worldZ);
        return _chunks.ContainsKey(chunkCoord);
    }

    public BlockId GetBlock(int worldX, int worldY, int worldZ)
    {
        var chunkCoord = ToChunkCoord(worldX, worldZ);
        if (!_chunks.TryGetValue(chunkCoord, out var chunk))
        {
            return BlockId.Air;
        }

        var (localX, localZ) = ToLocalCoord(worldX, worldZ);
        return chunk.GetBlock(localX, worldY, localZ);
    }

    public void SetBlock(int worldX, int worldY, int worldZ, BlockId id)
    {
        var chunkCoord = ToChunkCoord(worldX, worldZ);
        if (!_chunks.TryGetValue(chunkCoord, out var chunk))
        {
            return;
        }

        var (localX, localZ) = ToLocalCoord(worldX, worldZ);
        lock (chunk.SyncRoot)
        {
            chunk.SetBlock(localX, worldY, localZ, id);
            chunk.MarkLightingDirty();
        }

        if (localX == 0)
        {
            var neighbor = GetChunk(new ChunkCoord(chunkCoord.X - 1, chunkCoord.Z));
            neighbor?.MarkDirty();
            neighbor?.MarkLightingDirty();
        }
        else if (localX == Chunk.SizeX - 1)
        {
            var neighbor = GetChunk(new ChunkCoord(chunkCoord.X + 1, chunkCoord.Z));
            neighbor?.MarkDirty();
            neighbor?.MarkLightingDirty();
        }

        if (localZ == 0)
        {
            var neighbor = GetChunk(new ChunkCoord(chunkCoord.X, chunkCoord.Z - 1));
            neighbor?.MarkDirty();
            neighbor?.MarkLightingDirty();
        }
        else if (localZ == Chunk.SizeZ - 1)
        {
            var neighbor = GetChunk(new ChunkCoord(chunkCoord.X, chunkCoord.Z + 1));
            neighbor?.MarkDirty();
            neighbor?.MarkLightingDirty();
        }
    }
}
