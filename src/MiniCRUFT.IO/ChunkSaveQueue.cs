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
            _queue.Add(chunk);
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
        while (!_pending.IsEmpty)
        {
            Thread.Sleep(10);
        }
    }

    private void WorkerLoop()
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
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _queue.CompleteAdding();
        try
        {
            Task.WaitAll(_workers.ToArray(), 2000);
        }
        catch
        {
        }
        _cts.Dispose();
        _queue.Dispose();
    }
}
