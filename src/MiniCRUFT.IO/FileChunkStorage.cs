using System;
using System.IO;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.IO;

public sealed class FileChunkStorage : IChunkStorage
{
    private const int Version = 2;
    private const int LockStripeCount = 64;
    private readonly string _chunkDir;
    private readonly object[] _chunkLocks = CreateChunkLocks();

    public FileChunkStorage(string worldPath)
    {
        _chunkDir = Path.Combine(worldPath, "chunks");
        Directory.CreateDirectory(_chunkDir);
    }

    public void SaveChunk(Chunk chunk)
    {
        lock (GetChunkLock(chunk.ChunkX, chunk.ChunkZ))
        {
            string path = GetChunkPath(chunk.ChunkX, chunk.ChunkZ);
            string tempPath = GetChunkTempPath(chunk.ChunkX, chunk.ChunkZ);
            Directory.CreateDirectory(_chunkDir);

            try
            {
                using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new BinaryWriter(stream))
                {
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
                    writer.Flush();
                    stream.Flush(true);
                }

                File.Move(tempPath, path, true);
            }
            catch
            {
                TryDeleteFile(tempPath);
                throw;
            }
        }
    }

    public Chunk? LoadChunk(ChunkCoord coord)
    {
        lock (GetChunkLock(coord.X, coord.Z))
        {
            string path = GetChunkPath(coord.X, coord.Z);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
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
                if (chunkX != coord.X || chunkZ != coord.Z)
                {
                    Log.Warn($"Chunk file {coord.X},{coord.Z} stores mismatched coordinates {chunkX},{chunkZ}. Skipping.");
                    return null;
                }

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
                int skyRead = reader.Read(chunk.SkyLight, 0, skyCount);
                if (skyRead != skyCount)
                {
                    Log.Warn($"Chunk {coord.X},{coord.Z} sky light data ended early. Expected {skyCount}, got {skyRead}.");
                    return null;
                }

                int torchCount = reader.ReadInt32();
                if (torchCount != chunk.TorchLight.Length)
                {
                    Log.Warn($"Chunk {coord.X},{coord.Z} torch light mismatch. Expected {chunk.TorchLight.Length}, got {torchCount}.");
                    return null;
                }
                int torchRead = reader.Read(chunk.TorchLight, 0, torchCount);
                if (torchRead != torchCount)
                {
                    Log.Warn($"Chunk {coord.X},{coord.Z} torch light data ended early. Expected {torchCount}, got {torchRead}.");
                    return null;
                }

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
            catch (EndOfStreamException)
            {
                Log.Warn($"Chunk {coord.X},{coord.Z} ended unexpectedly while loading.");
                return null;
            }
            catch (IOException ex)
            {
                Log.Warn($"Failed to load chunk {coord.X},{coord.Z}: {ex.Message}");
                return null;
            }
        }
    }

    private string GetChunkPath(int x, int z) => Path.Combine(_chunkDir, $"chunk_{x}_{z}.dat");

    private string GetChunkTempPath(int x, int z) => Path.Combine(_chunkDir, $"chunk_{x}_{z}.dat.tmp");

    private object GetChunkLock(int x, int z)
    {
        int stripe = (HashCode.Combine(x, z) & int.MaxValue) % LockStripeCount;
        return _chunkLocks[stripe];
    }

    private static object[] CreateChunkLocks()
    {
        var locks = new object[LockStripeCount];
        for (int i = 0; i < locks.Length; i++)
        {
            locks[i] = new object();
        }

        return locks;
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }
}
