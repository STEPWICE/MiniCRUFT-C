using System;
using System.Collections.Generic;
using MiniCRUFT.Core;

namespace MiniCRUFT.World;

public sealed class FallingBlockSystem
{
    private readonly FallingConfig _config;
    private readonly Queue<BlockCoord> _queue = new();
    private readonly HashSet<BlockCoord> _queued = new();

    public FallingBlockSystem(FallingConfig config)
    {
        _config = config;
    }

    public void NotifyBlockChanged(BlockChange change)
    {
        if (!_config.Enabled)
        {
            return;
        }

        Enqueue(new BlockCoord(change.X, change.Y, change.Z));
        Enqueue(new BlockCoord(change.X, change.Y + 1, change.Z));
    }

    public void Update(World world, WorldEditor editor)
    {
        if (!_config.Enabled)
        {
            _queue.Clear();
            _queued.Clear();
            return;
        }

        int budget = Math.Max(1, _config.MaxUpdatesPerFrame);
        int processed = 0;
        while (processed < budget && _queue.Count > 0)
        {
            var coord = _queue.Dequeue();
            _queued.Remove(coord);
            ProcessCoord(world, editor, coord);
            processed++;
        }
    }

    private void ProcessCoord(World world, WorldEditor editor, BlockCoord coord)
    {
        if (coord.Y <= 0 || coord.Y >= Chunk.SizeY)
        {
            return;
        }

        var id = world.GetBlock(coord.X, coord.Y, coord.Z);
        if (!IsFallingBlock(id))
        {
            return;
        }

        int targetY = FindLandingY(world, coord.X, coord.Y, coord.Z);
        if (targetY == coord.Y)
        {
            return;
        }

        editor.SetBlock(coord.X, coord.Y, coord.Z, BlockId.Air);
        editor.SetBlock(coord.X, targetY, coord.Z, id);

        Enqueue(new BlockCoord(coord.X, coord.Y + 1, coord.Z));
        Enqueue(new BlockCoord(coord.X, targetY, coord.Z));
    }

    private static int FindLandingY(World world, int x, int y, int z)
    {
        int target = y;
        while (target > 0 && IsEmptyForFalling(world.GetBlock(x, target - 1, z)))
        {
            target--;
        }
        return target;
    }

    private static bool IsEmptyForFalling(BlockId id)
    {
        if (id == BlockId.Air)
        {
            return true;
        }

        if (LiquidBlocks.IsLiquid(id))
        {
            return true;
        }

        var def = BlockRegistry.Get(id);
        return !def.IsSolid;
    }

    private void Enqueue(BlockCoord coord)
    {
        if (coord.Y < 0 || coord.Y >= Chunk.SizeY)
        {
            return;
        }

        if (_queued.Add(coord))
        {
            _queue.Enqueue(coord);
        }
    }

    private static bool IsFallingBlock(BlockId id)
    {
        return id == BlockId.Sand || id == BlockId.Gravel;
    }
}
