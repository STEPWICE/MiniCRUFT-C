using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.IO;

public sealed class ChunkSaveQueue : IDisposable
{
    private readonly IChunkStorage _storage;
    private readonly BlockingCollection<Chunk> _queue = new();
    private readonly ConcurrentDictionary<ChunkCoord, byte> _pending = new();
    private readonly List<Task> _workers = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly ManualResetEventSlim _drained = new(true);
    private int _pendingCount;

    public ChunkSaveQueue(IChunkStorage storage, int workerCount)
    {
        _storage = storage;
        int count = Math.Max(1, workerCount);
        for (int i = 0; i < count; i++)
        {
            _workers.Add(Task.Run(WorkerLoop));
        }
    }

    public void Enqueue(Chunk chunk)
    {
        if (!chunk.SaveDirty)
        {
            return;
        }

        var coord = new ChunkCoord(chunk.ChunkX, chunk.ChunkZ);
        if (_pending.TryAdd(coord, 0))
        {
            IncrementPending();
            try
            {
                _queue.Add(chunk);
            }
            catch
            {
                _pending.TryRemove(coord, out _);
                DecrementPending();
                throw;
            }
        }
    }

    public void EnqueueDirty(IEnumerable<Chunk> chunks)
    {
        foreach (var chunk in chunks)
        {
            Enqueue(chunk);
        }
    }

    public void Flush()
    {
        _drained.Wait();
    }

    private void WorkerLoop()
    {
        try
        {
            foreach (var chunk in _queue.GetConsumingEnumerable(_cts.Token))
            {
                if (_cts.IsCancellationRequested)
                {
                    break;
                }

                var coord = new ChunkCoord(chunk.ChunkX, chunk.ChunkZ);
                try
                {
                    lock (chunk.SyncRoot)
                    {
                        if (chunk.SaveDirty)
                        {
                            _storage.SaveChunk(chunk);
                            chunk.ClearSaveDirty();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"Chunk save failed for {coord.X},{coord.Z}: {ex.Message}");
                }
                finally
                {
                    _pending.TryRemove(coord, out _);
                    DecrementPending();
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void Dispose()
    {
        Flush();
        _cts.Cancel();
        _queue.CompleteAdding();
        try
        {
            WaitForWorkers(2000);
        }
        catch
        {
        }
        _cts.Dispose();
        _drained.Dispose();
        _queue.Dispose();
    }

    private void IncrementPending()
    {
        if (Interlocked.Increment(ref _pendingCount) == 1)
        {
            _drained.Reset();
        }
    }

    private void DecrementPending()
    {
        if (Interlocked.Decrement(ref _pendingCount) == 0)
        {
            _drained.Set();
        }
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
            }
        }
    }
}
