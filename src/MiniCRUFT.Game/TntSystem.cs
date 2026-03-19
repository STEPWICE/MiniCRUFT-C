using System.Collections.Generic;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.IO;
using MiniCRUFT.World;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Game;

public sealed class TntSystem
{
    private const float FuseRenderYOffset = 1.18f;

    private readonly TntConfig _config;
    private readonly List<TntState> _states = new();
    private readonly Queue<TntEvent> _events = new();
    private bool _eventOverflowLogged;
    private bool _stateOverflowLogged;

    public TntSystem(TntConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public void Update(float dt, WorldType world, WorldEditor editor, Player player, MobSystem? mobs = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(player);

        if (dt <= 0f)
        {
            return;
        }

        if (!_config.Enabled)
        {
            Clear();
            return;
        }

        for (int i = _states.Count - 1; i >= 0; i--)
        {
            var state = _states[i];
            if (!world.HasChunkAt(state.Position.X, state.Position.Z))
            {
                continue;
            }

            if (world.GetBlock(state.Position.X, state.Position.Y, state.Position.Z) != BlockId.Tnt)
            {
                _states.RemoveAt(i);
                continue;
            }

            state = state.WithFuse(state.FuseRemaining - dt);
            if (state.FuseRemaining > 0f)
            {
                _states[i] = state;
                continue;
            }

            _states.RemoveAt(i);
            Detonate(world, editor, player, mobs, state.Position);
        }
    }

    public bool Prime(WorldType world, BlockCoord position, float fuseSeconds, bool emitEvent = true)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!_config.Enabled)
        {
            return false;
        }

        if (!world.HasChunkAt(position.X, position.Z))
        {
            return false;
        }

        if (world.GetBlock(position.X, position.Y, position.Z) != BlockId.Tnt)
        {
            return false;
        }

        float fuse = fuseSeconds > 0f ? fuseSeconds : _config.FuseSeconds;
        if (fuse < 0.1f)
        {
            fuse = 0.1f;
        }

        int index = FindStateIndex(position);
        if (index >= 0)
        {
            if (fuse < _states[index].FuseRemaining)
            {
                _states[index] = new TntState(position, fuse, fuse);
                if (emitEvent)
                {
                    EnqueueEvent(new TntEvent(TntEventKind.Primed, ToRenderPosition(position), 1f, 0));
                }
            }
            return true;
        }

        if (_states.Count >= _config.MaxPrimedTnt)
        {
            if (!_stateOverflowLogged)
            {
                Log.Warn($"TNT state limit reached at {_config.MaxPrimedTnt}; additional primed TNT will be ignored.");
                _stateOverflowLogged = true;
            }

            return false;
        }

        _states.Add(new TntState(position, fuse, fuse));
        if (emitEvent)
        {
            EnqueueEvent(new TntEvent(TntEventKind.Primed, ToRenderPosition(position), 1f, 0));
        }
        return true;
    }

    public void Load(IEnumerable<TntSaveData> tnts, WorldType world)
    {
        ArgumentNullException.ThrowIfNull(tnts);
        ArgumentNullException.ThrowIfNull(world);

        Clear();

        if (!_config.Enabled)
        {
            return;
        }

        foreach (var data in tnts)
        {
            if (data.FuseRemaining <= 0f)
            {
                continue;
            }

            var position = data.Position;
            if (world.HasChunkAt(position.X, position.Z) &&
                world.GetBlock(position.X, position.Y, position.Z) != BlockId.Tnt)
            {
                continue;
            }

            float duration = data.FuseDuration > 0f ? data.FuseDuration : Math.Max(data.FuseRemaining, _config.FuseSeconds);
            if (!AddOrUpdate(position, data.FuseRemaining, duration, emitEvent: false))
            {
                break;
            }
        }
    }

    public void FillRenderInstances(List<TntRenderInstance> target)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.Clear();
        if (target.Capacity < _states.Count)
        {
            target.Capacity = _states.Count;
        }

        if (!_config.Enabled)
        {
            return;
        }

        for (int i = 0; i < _states.Count; i++)
        {
            var state = _states[i];
            target.Add(new TntRenderInstance(
                ToRenderPosition(state.Position),
                Math.Max(0f, state.FuseRemaining),
                Math.Max(state.FuseDuration, 0.1f)));
        }
    }

    public List<TntSaveData> BuildSaveData()
    {
        var result = new List<TntSaveData>(_states.Count);
        for (int i = 0; i < _states.Count; i++)
        {
            var state = _states[i];
            result.Add(new TntSaveData(state.Position, Math.Max(0f, state.FuseRemaining), Math.Max(state.FuseDuration, state.FuseRemaining)));
        }

        return result;
    }

    public bool TryDequeueEvent(out TntEvent tntEvent)
    {
        if (_events.Count == 0)
        {
            tntEvent = default;
            return false;
        }

        tntEvent = _events.Dequeue();
        if (_events.Count < _config.MaxEventQueue)
        {
            _eventOverflowLogged = false;
        }
        return true;
    }

    public void NotifyBlockChanged(BlockChange change)
    {
        if (change.NewId == BlockId.Tnt)
        {
            return;
        }

        Remove(change.X, change.Y, change.Z);
    }

    public void Clear()
    {
        _states.Clear();
        _events.Clear();
        _eventOverflowLogged = false;
        _stateOverflowLogged = false;
    }

    private void Detonate(WorldType world, WorldEditor editor, Player player, MobSystem? mobs, BlockCoord source)
    {
        Vector3 center = new(source.X + 0.5f, source.Y + 0.5f, source.Z + 0.5f);

        ApplyPlayerExplosion(player, center);
        mobs?.ApplyExplosion(center, _config.ExplosionRadius, _config.ExplosionDamage, _config.KnockbackStrength, player);
        int affectedBlocks = ExplosionSystem.DestroyBlocks(
            world,
            editor,
            center,
            _config.ExplosionRadius,
            _config.ResistanceScale,
            _config.MaxAffectedBlocks,
            source,
            block => Prime(world, block, _config.ChainReactionFuseSeconds));
        float intensity = Math.Clamp(1f + affectedBlocks / 64f, 0.85f, 1.35f);
        EnqueueEvent(new TntEvent(TntEventKind.Explosion, center, intensity, affectedBlocks));
    }

    private void ApplyPlayerExplosion(Player player, Vector3 center)
    {
        float radius = _config.ExplosionRadius;
        if (radius <= 0f)
        {
            return;
        }

        float distance = Vector3.Distance(center, player.Position);
        if (distance > radius)
        {
            return;
        }

        float falloff = 1f - Math.Clamp(distance / radius, 0f, 1f);
        int damage = Math.Max(1, (int)MathF.Round(_config.ExplosionDamage * falloff));
        Vector3 knockback = MobSystem.BuildKnockback(center, player.Position, _config.KnockbackStrength * falloff);
        player.TryApplyDamage(damage, knockback);
    }

    private bool AddOrUpdate(BlockCoord position, float fuseSeconds, float fuseDuration, bool emitEvent = true)
    {
        int index = FindStateIndex(position);
        if (index >= 0)
        {
            if (fuseSeconds < _states[index].FuseRemaining)
            {
                _states[index] = new TntState(position, fuseSeconds, Math.Max(fuseDuration, 0.1f));
                if (emitEvent)
                {
                    EnqueueEvent(new TntEvent(TntEventKind.Primed, ToRenderPosition(position), 1f, 0));
                }
            }
            return true;
        }

        if (_states.Count >= _config.MaxPrimedTnt)
        {
            if (!_stateOverflowLogged)
            {
                Log.Warn($"TNT state limit reached at {_config.MaxPrimedTnt}; additional primed TNT will be ignored.");
                _stateOverflowLogged = true;
            }

            return false;
        }

        _states.Add(new TntState(position, fuseSeconds, Math.Max(fuseDuration, 0.1f)));
        if (emitEvent)
        {
            EnqueueEvent(new TntEvent(TntEventKind.Primed, ToRenderPosition(position), 1f, 0));
        }

        return true;
    }

    private void Remove(int x, int y, int z)
    {
        for (int i = _states.Count - 1; i >= 0; i--)
        {
            var state = _states[i];
            if (state.Position.X == x && state.Position.Y == y && state.Position.Z == z)
            {
                _states.RemoveAt(i);
                return;
            }
        }
    }

    private int FindStateIndex(BlockCoord position)
    {
        for (int i = 0; i < _states.Count; i++)
        {
            if (_states[i].Position.Equals(position))
            {
                return i;
            }
        }

        return -1;
    }

    private static Vector3 ToRenderPosition(BlockCoord position)
    {
        return new Vector3(position.X + 0.5f, position.Y + FuseRenderYOffset, position.Z + 0.5f);
    }

    private void EnqueueEvent(TntEvent tntEvent)
    {
        int limit = Math.Max(1, _config.MaxEventQueue);
        if (_events.Count >= limit)
        {
            if (!_eventOverflowLogged)
            {
                Log.Warn($"TNT event queue reached the limit of {limit}; dropping oldest events.");
                _eventOverflowLogged = true;
            }

            _events.Dequeue();
        }

        _events.Enqueue(tntEvent);
    }

}
