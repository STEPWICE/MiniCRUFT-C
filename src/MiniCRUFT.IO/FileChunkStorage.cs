using System;
using System.IO;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.IO;

public sealed class FileChunkStorage : IChunkStorage
{
    private const int Version = 2;
    private readonly string _chunkDir;

    public FileChunkStorage(string worldPath)
    {
        _chunkDir = Path.Combine(worldPath, "chunks");
        Directory.CreateDirectory(_chunkDir);
    }

    public void SaveChunk(Chunk chunk)
    {
        string path = GetChunkPath(chunk.ChunkX, chunk.ChunkZ);
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(stream);

        writer.Write(Version);
        writer.Write(chunk.ChunkX);
        writer.Write(chunk.ChunkZ);
        writer.Write(chunk.Blocks.Length);
        for (int i = 0; i < chunk.Blocks.Length; i++)
        {
            writer.Write((byte)chunk.Blocks[i]);
        }

        writer.Write(chunk.SkyLight.Length);
        writer.Write(chunk.SkyLight);
        writer.Write(chunk.TorchLight.Length);
        writer.Write(chunk.TorchLight);

        writer.Write(chunk.Biomes.Length);
        for (int i = 0; i < chunk.Biomes.Length; i++)
        {
            writer.Write((byte)chunk.Biomes[i]);
        }

        writer.Write(DateTime.UtcNow.Ticks);
    }

    public Chunk? LoadChunk(ChunkCoord coord)
    {
        string path = GetChunkPath(coord.X, coord.Z);
        if (!File.Exists(path))
        {
            return null;
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(stream);

        int version = reader.ReadInt32();
        if (version != 1 && version != Version)
        {
            Log.Warn($"Chunk {coord.X},{coord.Z} has unsupported version {version}. Skipping.");
            return null;
        }

        int chunkX = reader.ReadInt32();
        int chunkZ = reader.ReadInt32();
        var chunk = new Chunk(chunkX, chunkZ);

        int blockCount = reader.ReadInt32();
        if (blockCount != chunk.Blocks.Length)
        {
            Log.Warn($"Chunk {coord.X},{coord.Z} block count mismatch. Expected {chunk.Blocks.Length}, got {blockCount}.");
            return null;
        }
        for (int i = 0; i < blockCount; i++)
        {
            chunk.Blocks[i] = (BlockId)reader.ReadByte();
        }

        int skyCount = reader.ReadInt32();
        if (skyCount != chunk.SkyLight.Length)
        {
            Log.Warn($"Chunk {coord.X},{coord.Z} sky light mismatch. Expected {chunk.SkyLight.Length}, got {skyCount}.");
            return null;
        }
        reader.Read(chunk.SkyLight, 0, skyCount);
        int torchCount = reader.ReadInt32();
        if (torchCount != chunk.TorchLight.Length)
        {
            Log.Warn($"Chunk {coord.X},{coord.Z} torch light mismatch. Expected {chunk.TorchLight.Length}, got {torchCount}.");
            return null;
        }
        reader.Read(chunk.TorchLight, 0, torchCount);

        int biomeCount = reader.ReadInt32();
        if (biomeCount != chunk.Biomes.Length)
        {
            Log.Warn($"Chunk {coord.X},{coord.Z} biome count mismatch. Expected {chunk.Biomes.Length}, got {biomeCount}.");
            return null;
        }
        for (int i = 0; i < biomeCount; i++)
        {
            chunk.Biomes[i] = (BiomeId)reader.ReadByte();
        }

        if (version >= 2)
        {
            _ = reader.ReadInt64();
        }

        WorldLighting.RecalculateChunkLighting(chunk);
        chunk.ClearDirty();
        chunk.ClearSaveDirty();
        return chunk;
    }

    private string GetChunkPath(int x, int z) => Path.Combine(_chunkDir, $"chunk_{x}_{z}.dat");
}
