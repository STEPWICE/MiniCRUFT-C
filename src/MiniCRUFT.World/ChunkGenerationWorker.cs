using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MiniCRUFT.Core;

namespace MiniCRUFT.World;

public sealed class ChunkGenerationWorker : IDisposable, IChunkGenerationQueue
{
    private readonly World _world;
    private readonly WorldGenerator _generator;
    private readonly BlockingCollection<ChunkCoord> _queue = new();
    private readonly ConcurrentDictionary<ChunkCoord, byte> _queued = new();
    private readonly ConcurrentQueue<Chunk> _completedChunks = new();
    private readonly List<Task> _workers = new();
    private readonly CancellationTokenSource _cts = new();

    public ChunkGenerationWorker(World world, int workerCount)
    {
        _world = world;
        _generator = new WorldGenerator(world.Seed, world.Settings);

        for (int i = 0; i < workerCount; i++)
        {
            _workers.Add(Task.Run(WorkerLoop));
        }
    }

    public void Enqueue(ChunkCoord coord)
    {
        if (_world.GetChunk(coord) != null)
        {
            return;
        }

        if (!_queued.TryAdd(coord, 0))
        {
            return;
        }

        try
        {
            _queue.Add(coord);
        }
        catch
        {
            _queued.TryRemove(coord, out _);
            throw;
        }
    }

    public bool TryDequeueCompletedChunk([NotNullWhen(true)] out Chunk? chunk)
    {
        return _completedChunks.TryDequeue(out chunk);
    }

    private void WorkerLoop()
    {
        foreach (var coord in _queue.GetConsumingEnumerable(_cts.Token))
        {
            if (_cts.IsCancellationRequested)
            {
                break;
            }

            if (_world.GetChunk(coord) != null)
            {
                _queued.TryRemove(coord, out _);
                continue;
            }

            var chunk = new Chunk(coord.X, coord.Z);
            lock (chunk.SyncRoot)
            {
                _generator.GenerateChunk(chunk);
            }

            _completedChunks.Enqueue(chunk);
            _queued.TryRemove(coord, out _);
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _queue.CompleteAdding();
        try
        {
            WaitForWorkers(2000);
        }
        catch
        {
            // ignored
        }
        _cts.Dispose();
        _queue.Dispose();
    }

    private void WaitForWorkers(int timeoutMilliseconds)
    {
        long deadline = Environment.TickCount64 + timeoutMilliseconds;
        foreach (var worker in _workers)
        {
            int remaining = (int)Math.Max(0, deadline - Environment.TickCount64);
            if (remaining == 0)
            {
                break;
            }

            try
            {
                worker.Wait(remaining);
            }
            catch
            {
                // ignored
            }
        }
    }
}
