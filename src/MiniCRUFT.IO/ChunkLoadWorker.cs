using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.IO;

public sealed class ChunkLoadWorker : IDisposable, IChunkLoadQueue
{
    private readonly IChunkStorage _storage;
    private readonly BlockingCollection<ChunkCoord> _queue = new();
    private readonly ConcurrentDictionary<ChunkCoord, byte> _queued = new();
    private readonly ConcurrentQueue<Chunk> _completedChunks = new();
    private readonly ConcurrentQueue<ChunkCoord> _missingChunks = new();
    private readonly List<Task> _workers = new();
    private readonly CancellationTokenSource _cts = new();

    public ChunkLoadWorker(IChunkStorage storage, int workerCount)
    {
        _storage = storage;

        for (int i = 0; i < workerCount; i++)
        {
            _workers.Add(Task.Run(WorkerLoop));
        }
    }

    public void Enqueue(ChunkCoord coord)
    {
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

    public bool TryDequeueMissingChunk(out ChunkCoord coord)
    {
        return _missingChunks.TryDequeue(out coord);
    }

    private void WorkerLoop()
    {
        try
        {
            foreach (var coord in _queue.GetConsumingEnumerable(_cts.Token))
            {
                if (_cts.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    var chunk = _storage.LoadChunk(coord);
                    if (chunk != null)
                    {
                        _completedChunks.Enqueue(chunk);
                    }
                    else
                    {
                        _missingChunks.Enqueue(coord);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.Warn($"Chunk load failed for {coord.X},{coord.Z}: {ex.Message}");
                    _missingChunks.Enqueue(coord);
                }
                finally
                {
                    _queued.TryRemove(coord, out _);
                }
            }
        }
        catch (OperationCanceledException)
        {
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
