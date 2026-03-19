using System;
using System.IO;
using MiniCRUFT.Core;
using MiniCRUFT.IO;
using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class ChunkStorageTests
{
    [Fact]
    public void FileChunkStorage_RoundTrip_PreservesBlocks()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_chunk_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            BlockRegistry.Initialize();
            BiomeRegistry.Initialize();
            var storage = new FileChunkStorage(root);
            var chunk = new Chunk(0, 0);
            chunk.SetBlock(1, 1, 1, BlockId.Stone);
            chunk.SetBlock(2, 2, 2, BlockId.Dirt);
            storage.SaveChunk(chunk);

            var loaded = storage.LoadChunk(new ChunkCoord(0, 0));
            Assert.NotNull(loaded);
            Assert.Equal(BlockId.Stone, loaded!.GetBlock(1, 1, 1));
            Assert.Equal(BlockId.Dirt, loaded.GetBlock(2, 2, 2));
            Assert.False(loaded.SaveDirty);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void LoadChunk_ReadsLegacyVersion1Format()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_chunk_legacy_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            BlockRegistry.Initialize();
            BiomeRegistry.Initialize();

            var storage = new FileChunkStorage(root);
            string path = Path.Combine(root, "chunks", "chunk_3_-1.dat");
            var blocks = new byte[Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ];
            var skylight = new byte[blocks.Length];
            var torchlight = new byte[blocks.Length];
            var biomes = new byte[Chunk.SizeX * Chunk.SizeZ];

            blocks[Chunk.GetIndex(1, 1, 1)] = (byte)BlockId.Stone;
            biomes[Chunk.GetBiomeIndex(4, 5)] = (byte)BiomeId.Forest;

            using (var writer = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write)))
            {
                writer.Write(1);
                writer.Write(3);
                writer.Write(-1);
                writer.Write(blocks.Length);
                writer.Write(blocks);
                writer.Write(skylight.Length);
                writer.Write(skylight);
                writer.Write(torchlight.Length);
                writer.Write(torchlight);
                writer.Write(biomes.Length);
                writer.Write(biomes);
            }

            var loaded = storage.LoadChunk(new ChunkCoord(3, -1));

            Assert.NotNull(loaded);
            Assert.Equal(3, loaded!.ChunkX);
            Assert.Equal(-1, loaded.ChunkZ);
            Assert.Equal(BlockId.Stone, loaded.GetBlock(1, 1, 1));
            Assert.Equal(BiomeId.Forest, loaded.GetBiome(4, 5));
            Assert.False(loaded.SaveDirty);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void LoadChunk_RejectsUnsupportedVersionZeroFormat()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_chunk_v0_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            BlockRegistry.Initialize();
            BiomeRegistry.Initialize();

            var storage = new FileChunkStorage(root);
            string path = Path.Combine(root, "chunks", "chunk_0_0.dat");

            using (var writer = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write)))
            {
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
            }

            var loaded = storage.LoadChunk(new ChunkCoord(0, 0));

            Assert.Null(loaded);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void LoadChunk_RejectsCoordinateMismatch()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_chunk_coord_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            BlockRegistry.Initialize();
            BiomeRegistry.Initialize();

            var storage = new FileChunkStorage(root);
            string path = Path.Combine(root, "chunks", "chunk_3_-1.dat");
            var blocks = new byte[Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ];
            var skylight = new byte[blocks.Length];
            var torchlight = new byte[blocks.Length];
            var biomes = new byte[Chunk.SizeX * Chunk.SizeZ];

            using (var writer = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write)))
            {
                writer.Write(2);
                writer.Write(99);
                writer.Write(-1);
                writer.Write(blocks.Length);
                writer.Write(blocks);
                writer.Write(skylight.Length);
                writer.Write(skylight);
                writer.Write(torchlight.Length);
                writer.Write(torchlight);
                writer.Write(biomes.Length);
                writer.Write(biomes);
                writer.Write(DateTime.UtcNow.Ticks);
            }

            var loaded = storage.LoadChunk(new ChunkCoord(3, -1));

            Assert.Null(loaded);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void LoadChunk_RejectsTruncatedLightingData()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_chunk_truncated_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            BlockRegistry.Initialize();
            BiomeRegistry.Initialize();

            var storage = new FileChunkStorage(root);
            string path = Path.Combine(root, "chunks", "chunk_1_1.dat");
            var blocks = new byte[Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ];
            var skylight = new byte[blocks.Length];

            using (var writer = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write)))
            {
                writer.Write(2);
                writer.Write(1);
                writer.Write(1);
                writer.Write(blocks.Length);
                writer.Write(blocks);
                writer.Write(skylight.Length);
                writer.Write(new byte[skylight.Length / 2]);
            }

            var loaded = storage.LoadChunk(new ChunkCoord(1, 1));

            Assert.Null(loaded);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
