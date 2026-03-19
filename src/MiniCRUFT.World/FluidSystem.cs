using System;
using System.Collections.Generic;
using MiniCRUFT.Core;

namespace MiniCRUFT.World;

public sealed class FluidSystem
{
    private readonly FluidConfig _config;
    private readonly LiquidQueue _waterQueue = new();
    private readonly LiquidQueue _lavaQueue = new();
    private int _frameIndex;

    public FluidSystem(FluidConfig config)
    {
        _config = config;
    }

    public void NotifyBlockChanged(BlockChange change)
    {
        if (!_config.Enabled)
        {
            return;
        }

        EnqueueNeighbors(change.X, change.Y, change.Z);
    }

    public void Update(World world, WorldEditor editor)
    {
        if (!_config.Enabled)
        {
            ClearQueues();
            return;
        }

        ProcessQueue(world, editor, LiquidKind.Water, Math.Max(1, _config.MaxUpdatesPerFrame), _waterQueue);

        if (ShouldUpdateLavaThisFrame())
        {
            ProcessQueue(world, editor, LiquidKind.Lava, Math.Max(1, _config.LavaUpdatesPerFrame), _lavaQueue);
        }

        _frameIndex++;
    }

    private bool ShouldUpdateLavaThisFrame()
    {
        int interval = Math.Max(1, _config.LavaUpdateIntervalFrames);
        return _frameIndex % interval == 0;
    }

    private void ProcessQueue(World world, WorldEditor editor, LiquidKind kind, int budget, LiquidQueue queue)
    {
        int processed = 0;
        while (processed < budget && queue.Queue.Count > 0)
        {
            var coord = queue.Queue.Dequeue();
            queue.Queued.Remove(coord);
            ProcessCoord(world, editor, coord, kind);
            processed++;
        }
    }

    private void ProcessCoord(World world, WorldEditor editor, BlockCoord coord, LiquidKind kind)
    {
        if (coord.Y <= 0 || coord.Y >= Chunk.SizeY - 1)
        {
            return;
        }

        if (IsMissingNeighborChunk(world, coord))
        {
            return;
        }

        var current = world.GetBlock(coord.X, coord.Y, coord.Z);
        var currentKind = LiquidBlocks.TryGetKind(current);

        if (currentKind != kind)
        {
            if (currentKind.HasValue)
            {
                return;
            }

            if (!IsReplaceable(current))
            {
                return;
            }

            int replacementLevel = ComputeTargetLevel(world, coord.X, coord.Y, coord.Z, kind);
            if (replacementLevel < 0)
            {
                return;
            }

            if (editor.SetBlock(coord.X, coord.Y, coord.Z, LiquidBlocks.FromLevel(kind, replacementLevel)))
            {
                EnqueueNeighbors(coord.X, coord.Y, coord.Z);
            }

            return;
        }

        if (_config.LavaHardensOnWaterContact)
        {
            if (kind == LiquidKind.Water)
            {
                HardenAdjacentLava(world, editor, coord);
            }
            else if (HasAdjacentWater(world, coord))
            {
                HardenCurrentLava(editor, coord, current);
                return;
            }
        }

        if (LiquidBlocks.IsSource(current, kind))
        {
            return;
        }

        int level = LiquidBlocks.GetLevel(current);
        int targetLevel = ComputeTargetLevel(world, coord.X, coord.Y, coord.Z, kind);
        if (targetLevel < 0)
        {
            if (kind == LiquidKind.Water && !LiquidBlocks.IsSource(current, kind) && level <= LiquidBlocks.MaxLevel)
            {
                if (editor.SetBlock(coord.X, coord.Y, coord.Z, BlockId.Air))
                {
                    EnqueueNeighbors(coord.X, coord.Y, coord.Z);
                }
            }

            return;
        }

        BlockId targetId = LiquidBlocks.FromLevel(kind, targetLevel);
        if (current != targetId)
        {
            if (editor.SetBlock(coord.X, coord.Y, coord.Z, targetId))
            {
                EnqueueNeighbors(coord.X, coord.Y, coord.Z);
            }
        }
    }

    private bool HardenCurrentLava(WorldEditor editor, BlockCoord coord, BlockId current)
    {
        if (!LiquidBlocks.IsLava(current))
        {
            return false;
        }

        BlockId hardened = LiquidBlocks.IsSource(current, LiquidKind.Lava) ? BlockId.Obsidian : BlockId.Cobblestone;
        if (!editor.SetBlock(coord.X, coord.Y, coord.Z, hardened))
        {
            return false;
        }

        EnqueueNeighbors(coord.X, coord.Y, coord.Z);
        return true;
    }

    private void HardenAdjacentLava(World world, WorldEditor editor, BlockCoord coord)
    {
        HardenLavaNeighbor(world, editor, coord.X + 1, coord.Y, coord.Z);
        HardenLavaNeighbor(world, editor, coord.X - 1, coord.Y, coord.Z);
        HardenLavaNeighbor(world, editor, coord.X, coord.Y, coord.Z + 1);
        HardenLavaNeighbor(world, editor, coord.X, coord.Y, coord.Z - 1);
        HardenLavaNeighbor(world, editor, coord.X, coord.Y + 1, coord.Z);
        HardenLavaNeighbor(world, editor, coord.X, coord.Y - 1, coord.Z);
    }

    private void HardenLavaNeighbor(World world, WorldEditor editor, int x, int y, int z)
    {
        if (y <= 0 || y >= Chunk.SizeY - 1)
        {
            return;
        }

        var id = world.GetBlock(x, y, z);
        if (!LiquidBlocks.IsLava(id))
        {
            return;
        }

        BlockId hardened = LiquidBlocks.IsSource(id, LiquidKind.Lava) ? BlockId.Obsidian : BlockId.Cobblestone;
        if (editor.SetBlock(x, y, z, hardened))
        {
            EnqueueNeighbors(x, y, z);
        }
    }

    private bool HasAdjacentWater(World world, BlockCoord coord)
    {
        return LiquidBlocks.IsWater(world.GetBlock(coord.X + 1, coord.Y, coord.Z)) ||
               LiquidBlocks.IsWater(world.GetBlock(coord.X - 1, coord.Y, coord.Z)) ||
               LiquidBlocks.IsWater(world.GetBlock(coord.X, coord.Y, coord.Z + 1)) ||
               LiquidBlocks.IsWater(world.GetBlock(coord.X, coord.Y, coord.Z - 1)) ||
               LiquidBlocks.IsWater(world.GetBlock(coord.X, coord.Y + 1, coord.Z)) ||
               LiquidBlocks.IsWater(world.GetBlock(coord.X, coord.Y - 1, coord.Z));
    }

    private int ComputeTargetLevel(World world, int x, int y, int z, LiquidKind kind)
    {
        var above = world.GetBlock(x, y + 1, z);
        var aboveKind = LiquidBlocks.TryGetKind(above);
        if (aboveKind == kind)
        {
            int aboveLevel = LiquidBlocks.GetLevel(above);
            int maxFlowLevel = GetMaxFlowLevel(kind);
            int targetFromAbove = aboveLevel + 1;
            if (targetFromAbove > maxFlowLevel)
            {
                return -1;
            }

            return targetFromAbove;
        }

        int best = int.MaxValue;
        int sourceCount = 0;
        SampleNeighbor(world.GetBlock(x + 1, y, z), kind, ref best, ref sourceCount);
        SampleNeighbor(world.GetBlock(x - 1, y, z), kind, ref best, ref sourceCount);
        SampleNeighbor(world.GetBlock(x, y, z + 1), kind, ref best, ref sourceCount);
        SampleNeighbor(world.GetBlock(x, y, z - 1), kind, ref best, ref sourceCount);

        bool allowInfiniteSources = kind == LiquidKind.Water ? _config.InfiniteSources : _config.LavaInfiniteSources;
        if (allowInfiniteSources && sourceCount >= 2)
        {
            return 0;
        }

        if (best == int.MaxValue)
        {
            return -1;
        }

        int target = best + 1;
        int maxLevel = GetMaxFlowLevel(kind);
        if (target > maxLevel)
        {
            return -1;
        }

        return target;
    }

    private int GetMaxFlowLevel(LiquidKind kind)
    {
        return kind == LiquidKind.Water ? Math.Max(1, _config.WaterMaxSpreadLevel) : Math.Max(1, _config.LavaMaxSpreadLevel);
    }

    private static void SampleNeighbor(BlockId id, LiquidKind kind, ref int best, ref int sourceCount)
    {
        var neighborKind = LiquidBlocks.TryGetKind(id);
        if (neighborKind != kind)
        {
            return;
        }

        int level = LiquidBlocks.GetLevel(id);
        if (level == 0)
        {
            sourceCount++;
        }

        if (level < best)
        {
            best = level;
        }
    }

    private bool IsReplaceable(BlockId id)
    {
        if (id == BlockId.Air)
        {
            return true;
        }

        if (!_config.ReplaceNonSolid)
        {
            return false;
        }

        if (LiquidBlocks.IsLiquid(id))
        {
            return false;
        }

        var def = BlockRegistry.Get(id);
        if (def.IsSolid)
        {
            return false;
        }

        return def.RenderMode is RenderMode.Cross or RenderMode.Torch;
    }

    private static bool IsMissingNeighborChunk(World world, BlockCoord coord)
    {
        var chunkCoord = World.ToChunkCoord(coord.X, coord.Z);
        var chunk = world.GetChunk(chunkCoord);
        if (chunk == null)
        {
            return true;
        }

        var (localX, localZ) = World.ToLocalCoord(coord.X, coord.Z);
        if (localX == 0 && world.GetChunk(new ChunkCoord(chunkCoord.X - 1, chunkCoord.Z)) == null)
        {
            return true;
        }
        if (localX == Chunk.SizeX - 1 && world.GetChunk(new ChunkCoord(chunkCoord.X + 1, chunkCoord.Z)) == null)
        {
            return true;
        }
        if (localZ == 0 && world.GetChunk(new ChunkCoord(chunkCoord.X, chunkCoord.Z - 1)) == null)
        {
            return true;
        }
        if (localZ == Chunk.SizeZ - 1 && world.GetChunk(new ChunkCoord(chunkCoord.X, chunkCoord.Z + 1)) == null)
        {
            return true;
        }

        return false;
    }

    private void EnqueueNeighbors(int x, int y, int z)
    {
        Enqueue(_waterQueue, new BlockCoord(x, y, z));
        Enqueue(_waterQueue, new BlockCoord(x + 1, y, z));
        Enqueue(_waterQueue, new BlockCoord(x - 1, y, z));
        Enqueue(_waterQueue, new BlockCoord(x, y + 1, z));
        Enqueue(_waterQueue, new BlockCoord(x, y - 1, z));
        Enqueue(_waterQueue, new BlockCoord(x, y, z + 1));
        Enqueue(_waterQueue, new BlockCoord(x, y, z - 1));

        Enqueue(_lavaQueue, new BlockCoord(x, y, z));
        Enqueue(_lavaQueue, new BlockCoord(x + 1, y, z));
        Enqueue(_lavaQueue, new BlockCoord(x - 1, y, z));
        Enqueue(_lavaQueue, new BlockCoord(x, y + 1, z));
        Enqueue(_lavaQueue, new BlockCoord(x, y - 1, z));
        Enqueue(_lavaQueue, new BlockCoord(x, y, z + 1));
        Enqueue(_lavaQueue, new BlockCoord(x, y, z - 1));
    }

    private static void Enqueue(LiquidQueue queue, BlockCoord coord)
    {
        if (coord.Y < 0 || coord.Y >= Chunk.SizeY)
        {
            return;
        }

        if (queue.Queued.Add(coord))
        {
            queue.Queue.Enqueue(coord);
        }
    }

    private void ClearQueues()
    {
        _waterQueue.Clear();
        _lavaQueue.Clear();
    }

    private sealed class LiquidQueue
    {
        public Queue<BlockCoord> Queue { get; } = new();
        public HashSet<BlockCoord> Queued { get; } = new();

        public void Clear()
        {
            Queue.Clear();
            Queued.Clear();
        }
    }
}
