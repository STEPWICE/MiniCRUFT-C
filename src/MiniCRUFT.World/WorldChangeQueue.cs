using System.Collections.Concurrent;

namespace MiniCRUFT.World;

public interface IWorldChangeQueue
{
    void Enqueue(BlockChange change);
    bool TryDequeue(out BlockChange change);
    void Clear();
}

public readonly struct BlockChange
{
    public readonly int X;
    public readonly int Y;
    public readonly int Z;
    public readonly BlockId OldId;
    public readonly BlockId NewId;

    public BlockChange(int x, int y, int z, BlockId oldId, BlockId newId)
    {
        X = x;
        Y = y;
        Z = z;
        OldId = oldId;
        NewId = newId;
    }
}

public sealed class WorldChangeQueue : IWorldChangeQueue
{
    private readonly ConcurrentQueue<BlockChange> _queue = new();

    public void Enqueue(BlockChange change)
    {
        _queue.Enqueue(change);
    }

    public bool TryDequeue(out BlockChange change)
    {
        return _queue.TryDequeue(out change);
    }

    public void Clear()
    {
        while (_queue.TryDequeue(out _))
        {
        }
    }
}
