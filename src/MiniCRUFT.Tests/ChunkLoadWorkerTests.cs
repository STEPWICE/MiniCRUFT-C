using System;
using System.IO;
using System.Threading;
using MiniCRUFT.Core;
using MiniCRUFT.IO;
using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class ChunkLoadWorkerTests
{
    [Fact]
    public void LoadWorker_DequeueCompletedChunk_ReturnsLoadedChunk()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_chunkload_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            BlockRegistry.Initialize();
            BiomeRegistry.Initialize();

            var storage = new FileChunkStorage(root);
            var chunk = new Chunk(2, -3);
            chunk.SetBlock(1, 2, 3, BlockId.Stone);
            storage.SaveChunk(chunk);

            using var worker = new ChunkLoadWorker(storage, 1);
            worker.Enqueue(new ChunkCoord(2, -3));

            Chunk? loaded = null;
            bool completed = SpinWait.SpinUntil(() => worker.TryDequeueCompletedChunk(out loaded), TimeSpan.FromSeconds(2));

            Assert.True(completed);
            Assert.NotNull(loaded);
            Assert.Equal(2, loaded!.ChunkX);
            Assert.Equal(-3, loaded.ChunkZ);
            Assert.Equal(BlockId.Stone, loaded.GetBlock(1, 2, 3));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void LoadWorker_DequeueMissingChunk_ReturnsMissingCoord()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_chunkload_missing_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            BlockRegistry.Initialize();
            BiomeRegistry.Initialize();

            var storage = new FileChunkStorage(root);
            using var worker = new ChunkLoadWorker(storage, 1);
            var coord = new ChunkCoord(10, 20);
            worker.Enqueue(coord);

            ChunkCoord missing = default;
            bool completed = SpinWait.SpinUntil(() => worker.TryDequeueMissingChunk(out missing), TimeSpan.FromSeconds(2));

            Assert.True(completed);
            Assert.Equal(coord, missing);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
