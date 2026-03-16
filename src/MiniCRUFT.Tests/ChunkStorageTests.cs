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
}
