using System;
using System.Threading;
using System.Threading.Tasks;
using MiniCRUFT.Core;
using MiniCRUFT.IO;
using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class ChunkSaveQueueTests
{
    [Fact]
    public async Task Flush_WaitsForPendingSaveToFinish()
    {
        var started = new ManualResetEventSlim(false);
        var release = new ManualResetEventSlim(false);
        var storage = new BlockingChunkStorage(started, release);

        using var queue = new ChunkSaveQueue(storage, 1);
        var chunk = new Chunk(0, 0);

        queue.Enqueue(chunk);

        Assert.True(started.Wait(TimeSpan.FromSeconds(1)));

        var flushTask = Task.Run(() => queue.Flush());

        await Task.Delay(100);
        Assert.False(flushTask.IsCompleted);

        release.Set();

        await flushTask.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.Equal(1, storage.SaveCount);
    }

    private sealed class BlockingChunkStorage : IChunkStorage
    {
        private readonly ManualResetEventSlim _started;
        private readonly ManualResetEventSlim _release;

        public int SaveCount { get; private set; }

        public BlockingChunkStorage(ManualResetEventSlim started, ManualResetEventSlim release)
        {
            _started = started;
            _release = release;
        }

        public void SaveChunk(Chunk chunk)
        {
            SaveCount++;
            _started.Set();
            _release.Wait();
        }

        public Chunk? LoadChunk(ChunkCoord coord)
        {
            return null;
        }
    }
}
