using System;
using System.Collections.Generic;
using System.Numerics;
using MiniCRUFT.Core;

namespace MiniCRUFT.World;

public sealed class FireSystem
{
    private static readonly BlockCoord[] NeighborOffsets =
    {
        new(1, 0, 0),
        new(-1, 0, 0),
        new(0, 1, 0),
        new(0, -1, 0),
        new(0, 0, 1),
        new(0, 0, -1)
    };

    private readonly FireConfig _config;
    private readonly Random _random;
    private readonly Dictionary<BlockCoord, FireState> _fires = new();
    private readonly Queue<FireEvent> _events = new();
    private readonly List<BlockCoord> _scratch = new();
    private int _updateOffset;
    private bool _eventOverflowLogged;

    public FireSystem(int seed, FireConfig config)
    {
        _config = config;
        _random = new Random(seed ^ 0x6B6F5B3);
    }

    public void NotifyBlockChanged(BlockChange change)
    {
        if (!_config.Enabled)
        {
            return;
        }

        var coord = new BlockCoord(change.X, change.Y, change.Z);
        if (change.NewId == BlockId.Fire)
        {
            _fires.TryAdd(coord, default);
        }
        else if (change.OldId == BlockId.Fire)
        {
            _fires.Remove(coord);
        }
    }

    public bool TryDequeueEvent(out FireEvent fireEvent)
    {
        if (_events.Count > 0)
        {
            fireEvent = _events.Dequeue();
            if (_events.Count < Math.Max(1, _config.MaxEventQueue))
            {
                _eventOverflowLogged = false;
            }
            return true;
        }

        fireEvent = default;
        return false;
    }

    public void Update(World world, WorldEditor editor, float dt, float rainIntensity = 0f)
    {
        if (!_config.Enabled)
        {
            _fires.Clear();
            _events.Clear();
            _eventOverflowLogged = false;
            return;
        }

        if (dt <= 0f || _fires.Count == 0)
        {
            return;
        }

        float rain = Math.Clamp(rainIntensity, 0f, 1f);
        float rainExtinguish = rain * _config.RainExtinguishMultiplier;
        float maxAge = Math.Max(0.5f, _config.MaxAgeSeconds - rainExtinguish);
        float spreadInterval = Math.Max(0.05f, _config.SpreadIntervalSeconds * (1f + rain * 0.5f));
        float spreadChance = Math.Clamp(_config.SpreadChance * (1f - rain * 0.75f), 0f, 1f);
        float burnChance = Math.Clamp(_config.BurnChance * (1f - rain * 0.6f), 0f, 1f);
        float burnStart = Math.Clamp(_config.BurnStartSeconds - rainExtinguish * 0.5f, 0f, maxAge);

        _scratch.Clear();
        _scratch.AddRange(_fires.Keys);
        if (_scratch.Count == 0)
        {
            return;
        }

        int budget = Math.Max(1, _config.MaxUpdatesPerFrame);
        int startIndex = _updateOffset % _scratch.Count;
        int processed = 0;

        for (int i = 0; i < _scratch.Count && processed < budget; i++)
        {
            var coord = _scratch[(startIndex + i) % _scratch.Count];
            if (!_fires.TryGetValue(coord, out var state))
            {
                processed++;
                continue;
            }

            if (world.GetBlock(coord.X, coord.Y, coord.Z) != BlockId.Fire)
            {
                _fires.Remove(coord);
                processed++;
                continue;
            }

            state.Age += dt;
            state.SpreadAccumulator += dt;

            bool hasFuel = HasFlammableNeighbor(world, coord.X, coord.Y, coord.Z);
            if (state.Age >= maxAge || (!hasFuel && state.Age >= burnStart))
            {
                float extinguishIntensity = Math.Clamp(0.45f + state.Age / maxAge * 0.55f, 0.35f, 1f);
                Extinguish(editor, coord, extinguishIntensity);
                _fires.Remove(coord);
                processed++;
                continue;
            }

            if (state.SpreadAccumulator >= spreadInterval)
            {
                state.SpreadAccumulator = 0f;
                float crackleIntensity = Math.Clamp(0.4f + state.Age / maxAge * 0.6f, 0.35f, 1.2f);
                QueueEvent(FireEventKind.Crackle, coord, crackleIntensity);
                SpreadFrom(world, editor, coord, spreadChance, burnChance);
            }

            _fires[coord] = state;
            processed++;
        }

        _updateOffset = (startIndex + processed) % _scratch.Count;
    }

    public int IgniteExplosion(World world, WorldEditor editor, Vector3 position, int affectedBlocks, float intensity = 1f)
    {
        if (!_config.Enabled)
        {
            return 0;
        }

        int centerX = MathUtil.FloorToInt(position.X);
        int centerY = MathUtil.FloorToInt(position.Y);
        int centerZ = MathUtil.FloorToInt(position.Z);
        int maxIgnitedBlocks = Math.Max(1, _config.MaxExplosionIgnitedBlocks);
        int radius = Math.Clamp(
            (int)MathF.Ceiling(_config.ExplosionIgniteRadius + MathF.Sqrt(Math.Max(1, affectedBlocks)) * 0.35f + Math.Max(0f, intensity - 1f) * 2f),
            1,
            16);
        float chance = Math.Clamp(_config.ExplosionIgniteChance * Math.Clamp(intensity, 0.5f, 2f), 0f, 1f);

        int ignited = 0;
        for (int y = Math.Max(1, centerY - radius); y <= Math.Min(Chunk.SizeY - 2, centerY + radius) && ignited < maxIgnitedBlocks; y++)
        {
            for (int z = centerZ - radius; z <= centerZ + radius && ignited < maxIgnitedBlocks; z++)
            {
                for (int x = centerX - radius; x <= centerX + radius && ignited < maxIgnitedBlocks; x++)
                {
                    if (!world.HasChunkAt(x, z))
                    {
                        continue;
                    }

                    var block = world.GetBlock(x, y, z);
                    if (!IsFlammable(block))
                    {
                        continue;
                    }

                    float flammability = GetFlammability(block);
                    if (flammability <= 0f || NextFloat() > chance * flammability)
                    {
                        continue;
                    }

                    ignited += IgniteAroundBlock(world, editor, new BlockCoord(x, y, z));
                }
            }
        }

        return ignited;
    }

    private void SpreadFrom(World world, WorldEditor editor, BlockCoord source, float spreadChance, float burnChance)
    {
        for (int i = 0; i < NeighborOffsets.Length; i++)
        {
            var offset = NeighborOffsets[i];
            int x = source.X + offset.X;
            int y = source.Y + offset.Y;
            int z = source.Z + offset.Z;
            var block = world.GetBlock(x, y, z);
            float flammability = GetFlammability(block);
            if (flammability <= 0f)
            {
                continue;
            }

            if (NextFloat() <= spreadChance * flammability)
            {
                IgniteAroundBlock(world, editor, new BlockCoord(x, y, z));
            }

            if (burnChance > 0f && NextFloat() <= burnChance * flammability)
            {
                BurnBlock(editor, x, y, z, flammability);
            }
        }
    }

    private int IgniteAroundBlock(World world, WorldEditor editor, BlockCoord blockCoord)
    {
        int placed = 0;
        float intensity = Math.Clamp(GetFlammability(world.GetBlock(blockCoord.X, blockCoord.Y, blockCoord.Z)), 0.35f, 1f);
        int start = NextInt(NeighborOffsets.Length);
        for (int i = 0; i < NeighborOffsets.Length; i++)
        {
            var offset = NeighborOffsets[(start + i) % NeighborOffsets.Length];
            int x = blockCoord.X + offset.X;
            int y = blockCoord.Y + offset.Y;
            int z = blockCoord.Z + offset.Z;
            if (TryPlaceFire(world, editor, x, y, z, intensity))
            {
                placed++;
                break;
            }
        }

        return placed;
    }

    private bool TryPlaceFire(World world, WorldEditor editor, int x, int y, int z, float intensity = 1f)
    {
        if (y <= 0 || y >= Chunk.SizeY - 1)
        {
            return false;
        }

        if (!world.HasChunkAt(x, z))
        {
            return false;
        }

        var current = world.GetBlock(x, y, z);
        if (!IsFireSpace(current))
        {
            return false;
        }

        if (!HasFlammableNeighbor(world, x, y, z))
        {
            return false;
        }

        if (!editor.SetBlock(x, y, z, BlockId.Fire))
        {
            return false;
        }

        var coord = new BlockCoord(x, y, z);
        _fires[coord] = default;
        QueueEvent(FireEventKind.Ignited, coord, intensity);
        return true;
    }

    private void BurnBlock(WorldEditor editor, int x, int y, int z, float flammability)
    {
        if (editor.SetBlock(x, y, z, BlockId.Air))
        {
            var coord = new BlockCoord(x, y, z);
            _fires.Remove(coord);
            QueueEvent(FireEventKind.Consumed, coord, flammability);
        }
    }

    private void Extinguish(WorldEditor editor, BlockCoord coord, float intensity)
    {
        editor.SetBlock(coord.X, coord.Y, coord.Z, BlockId.Air);
        QueueEvent(FireEventKind.Extinguished, coord, intensity);
    }

    private static bool HasFlammableNeighbor(World world, int x, int y, int z)
    {
        for (int i = 0; i < NeighborOffsets.Length; i++)
        {
            var offset = NeighborOffsets[i];
            if (IsFlammable(world.GetBlock(x + offset.X, y + offset.Y, z + offset.Z)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsFlammable(BlockId id)
    {
        return id switch
        {
            BlockId.Wood or BlockId.BirchWood or BlockId.SpruceWood => true,
            BlockId.Planks => true,
            BlockId.Leaves or BlockId.BirchLeaves or BlockId.SpruceLeaves => true,
            BlockId.TallGrass or BlockId.Flower or BlockId.DeadBush or BlockId.SugarCane => true,
            _ => false
        };
    }

    private static float GetFlammability(BlockId id)
    {
        return id switch
        {
            BlockId.Leaves or BlockId.BirchLeaves or BlockId.SpruceLeaves => 1f,
            BlockId.Wood or BlockId.BirchWood or BlockId.SpruceWood => 0.7f,
            BlockId.Planks => 0.85f,
            BlockId.TallGrass or BlockId.Flower or BlockId.DeadBush or BlockId.SugarCane => 0.9f,
            _ => 0f
        };
    }

    private static bool IsFireSpace(BlockId id)
    {
        if (id == BlockId.Air)
        {
            return true;
        }

        var def = BlockRegistry.Get(id);
        return !def.IsSolid && (def.RenderMode is RenderMode.Cross or RenderMode.Torch);
    }

    private int NextInt(int maxExclusive)
    {
        return _random.Next(maxExclusive);
    }

    private float NextFloat()
    {
        return (float)_random.NextDouble();
    }

    private void QueueEvent(FireEventKind kind, BlockCoord coord, float intensity)
    {
        int limit = Math.Max(1, _config.MaxEventQueue);
        if (_events.Count >= limit)
        {
            if (!_eventOverflowLogged)
            {
                Log.Warn($"Fire event queue reached the limit of {limit}; dropping oldest events.");
                _eventOverflowLogged = true;
            }

            _events.Dequeue();
        }

        _events.Enqueue(new FireEvent(kind, new Vector3(coord.X + 0.5f, coord.Y + 0.5f, coord.Z + 0.5f), Math.Clamp(intensity, 0.1f, 2f)));
    }

    private struct FireState
    {
        public float Age;
        public float SpreadAccumulator;

        public FireState(float age, float spreadAccumulator)
        {
            Age = age;
            SpreadAccumulator = spreadAccumulator;
        }
    }
}
