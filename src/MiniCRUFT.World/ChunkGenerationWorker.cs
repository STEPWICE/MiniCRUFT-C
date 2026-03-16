using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MiniCRUFT.Core;

namespace MiniCRUFT.World;

public sealed class ChunkGenerationWorker : IDisposable
{
    private readonly World _world;
    private readonly WorldGenerator _generator;
    private readonly BlockingCollection<ChunkCoord> _queue = new();
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
        _queue.Add(coord);
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
                continue;
            }

            var chunk = new Chunk(coord.X, coord.Z);
            lock (chunk.SyncRoot)
            {
                _generator.GenerateChunk(chunk);
            }

            _world.SetChunk(chunk);
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
            // ignored
        }
        _cts.Dispose();
        _queue.Dispose();
    }
}
