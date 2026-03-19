using System;
using System.Collections.Generic;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.IO;
using MiniCRUFT.World;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Game;

public sealed class MobSystem
{
    private const float MinSpawnDistance = 12f;
    private const float HurtFlashSeconds = 0.2f;
    private const float ExplosionRadius = 3.5f;
    private const float ExplosionKnockback = 7f;
    private const float CreeperExplosionResistanceScale = 0.35f;
    private const int CreeperExplosionMaxAffectedBlocks = 64;
    private const float MobKnockback = 4.5f;
    private const float PassiveEscapeSeconds = 2.25f;
    private const float SunBurnThreshold = 0.35f;
    private const float SeparationRadius = 1.45f;
    private const float SeparationStrength = 0.9f;

    private readonly MobConfig _config;
    private readonly Random _random;
    private readonly List<MobState> _mobs = new();
    private readonly List<MobRenderInstance> _renderInstances = new();
    private readonly Queue<MobEvent> _eventQueue = new();

    private float _spawnTimer;
    private bool _renderDirty = true;
    private bool _eventOverflowLogged;

    public MobSystem(int seed, MobConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _random = new Random(seed);
        _spawnTimer = _config.SpawnIntervalSeconds;
        MobCatalog.Initialize();
    }

    public IReadOnlyList<MobRenderInstance> RenderInstances => _renderInstances;

    public bool HasMobOfType(MobType type)
    {
        for (int i = 0; i < _mobs.Count; i++)
        {
            MobState mob = _mobs[i];
            if (mob.Type == type && mob.Health > 0)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryGetMobSnapshot(MobType type, out MobRenderInstance instance)
    {
        for (int i = 0; i < _mobs.Count; i++)
        {
            MobState mob = _mobs[i];
            if (mob.Type != type || mob.Health <= 0)
            {
                continue;
            }

            instance = BuildRenderInstance(mob);
            return true;
        }

        instance = default;
        return false;
    }

    public bool TrySpawnScripted(MobType type, Vector3 position, float yaw, WorldType world)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!MobCatalog.TryGet(type, out MobDefinition? definition))
        {
            return false;
        }

        if (_mobs.Count >= _config.MaxAlive && type != MobType.Herobrine)
        {
            return false;
        }

        if (HasMobNearby(position, definition.Width))
        {
            return false;
        }

        var collider = new CharacterCollider(definition.Width, definition.Height);
        if (CharacterPhysics.IsColliding(world, position, collider))
        {
            return false;
        }

        _mobs.Add(CreateMobState(definition, position, yaw, yaw, homePosition: position));
        _renderDirty = true;
        return true;
    }

    public int DespawnAllOfType(MobType type, bool emitDeathEvent = false)
    {
        int removed = 0;
        for (int i = _mobs.Count - 1; i >= 0; i--)
        {
            MobState mob = _mobs[i];
            if (mob.Type != type)
            {
                continue;
            }

            if (emitDeathEvent)
            {
                Enqueue(MobEventKind.Death, mob.Type, mob.Position, 1f, mob.Elite, mob.EliteVariant);
            }

            _mobs.RemoveAt(i);
            removed++;
        }

        if (removed > 0)
        {
            _renderDirty = true;
        }

        return removed;
    }

    public void EmitScriptedEvent(MobEventKind kind, MobType type, Vector3 position, float intensity = 1f)
    {
        Enqueue(kind, type, position, intensity);
    }

    public void Update(float dt, WorldType world, WorldEditor editor, Player player, float sunIntensity, float rainIntensity = 0f)
        => Update(dt, world, editor, player, sunIntensity, rainIntensity, primeTnt: null);

    public void Update(float dt, WorldType world, WorldEditor editor, Player player, float sunIntensity, float rainIntensity, Action<BlockCoord>? primeTnt)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(player);

        if (dt <= 0f)
        {
            RefreshRenderInstances();
            return;
        }

        if (!_config.Enabled)
        {
            RefreshRenderInstances();
            return;
        }

        float stepDt = Math.Min(dt, 0.1f);
        Vector3 playerPosition = player.Position;

        for (int i = _mobs.Count - 1; i >= 0; i--)
        {
            if (!UpdateMob(_mobs[i], stepDt, world, editor, player, playerPosition, sunIntensity, primeTnt))
            {
                _mobs.RemoveAt(i);
                _renderDirty = true;
            }
        }

        TrySpawn(stepDt, world, playerPosition, sunIntensity, rainIntensity);
        if (ResolveMobCrowding())
        {
            _renderDirty = true;
        }
        RefreshRenderInstances();
    }

    public bool TryDequeueEvent(out MobEvent mobEvent)
    {
        if (_eventQueue.Count == 0)
        {
            mobEvent = default;
            return false;
        }

        mobEvent = _eventQueue.Dequeue();
        if (_eventQueue.Count < _config.MaxEventQueue)
        {
            _eventOverflowLogged = false;
        }
        return true;
    }

    public void Load(IEnumerable<MobSaveData> mobs)
    {
        Clear();
        MobSaveData? herobrineData = null;

        foreach (MobSaveData data in mobs)
        {
            if (data.Type == MobType.Herobrine)
            {
                herobrineData ??= data;
                continue;
            }

            if (_mobs.Count >= _config.MaxAlive)
            {
                break;
            }

            if (!MobCatalog.TryGet(data.Type, out MobDefinition? definition))
            {
                continue;
            }

            bool elite = data.Elite && definition.Hostile;
            EliteMobVariant eliteVariant = elite ? EliteMobVariantCatalog.Normalize(data.EliteVariant) : EliteMobVariant.None;
            int health = Math.Clamp(data.Health, 1, GetMaxHealth(definition, elite, eliteVariant));
            var mob = CreateMobState(
                definition,
                data.Position,
                data.Yaw,
                data.WanderAngle,
                data.HomePosition,
                elite: elite,
                eliteVariant: eliteVariant,
                health: health);
            mob.Velocity = data.Velocity;
            mob.AttackCooldown = Math.Max(0f, data.AttackCooldown);
            mob.WanderTimer = Math.Max(0f, data.WanderTimer);
            mob.IdleTimer = Math.Max(0f, data.IdleTimer);
            mob.HurtTimer = Math.Max(0f, data.HurtTimer);
            mob.SpecialTimer = Math.Max(0f, data.SpecialTimer);
            mob.SpecialActive = data.SpecialActive;
            mob.OnGround = data.OnGround;
            mob.StepDistance = Math.Max(0f, data.StepDistance);
            mob.Age = Math.Max(0f, data.Age);

            _mobs.Add(mob);
        }

        if (herobrineData.HasValue && MobCatalog.TryGet(MobType.Herobrine, out MobDefinition? herobrineDefinition))
        {
            MobSaveData data = herobrineData.Value;
            bool elite = data.Elite && herobrineDefinition.Hostile;
            EliteMobVariant eliteVariant = elite ? EliteMobVariantCatalog.Normalize(data.EliteVariant) : EliteMobVariant.None;
            int health = Math.Clamp(data.Health, 1, GetMaxHealth(herobrineDefinition, elite, eliteVariant));
            var herobrine = CreateMobState(
                herobrineDefinition,
                data.Position,
                data.Yaw,
                data.WanderAngle,
                data.HomePosition,
                elite: elite,
                eliteVariant: eliteVariant,
                health: health);
            herobrine.Velocity = data.Velocity;
            herobrine.AttackCooldown = Math.Max(0f, data.AttackCooldown);
            herobrine.WanderTimer = Math.Max(0f, data.WanderTimer);
            herobrine.IdleTimer = Math.Max(0f, data.IdleTimer);
            herobrine.HurtTimer = Math.Max(0f, data.HurtTimer);
            herobrine.SpecialTimer = Math.Max(0f, data.SpecialTimer);
            herobrine.SpecialActive = data.SpecialActive;
            herobrine.OnGround = data.OnGround;
            herobrine.StepDistance = Math.Max(0f, data.StepDistance);
            herobrine.Age = Math.Max(0f, data.Age);

            _mobs.Add(herobrine);
        }

        _renderDirty = true;
        RefreshRenderInstances();
    }

    public List<MobSaveData> BuildSaveData()
    {
        var result = new List<MobSaveData>(_mobs.Count);
        for (int i = 0; i < _mobs.Count; i++)
        {
            MobState mob = _mobs[i];
            result.Add(new MobSaveData(
                mob.Type,
                mob.Position,
                mob.Velocity,
                mob.HomePosition,
                mob.Yaw,
                mob.WanderAngle,
                mob.Health,
                mob.AttackCooldown,
                mob.WanderTimer,
                mob.IdleTimer,
                mob.HurtTimer,
                mob.SpecialTimer,
                mob.SpecialActive,
                mob.OnGround,
                mob.StepDistance,
                mob.Age,
                mob.Elite,
                mob.EliteVariant));
        }

        return result;
    }

    public MobHitResult Raycast(Vector3 origin, Vector3 direction, float maxDistance)
    {
        if (_mobs.Count == 0 || maxDistance <= 0f || direction.LengthSquared() <= float.Epsilon)
        {
            return default;
        }

        Vector3 rayDirection = Vector3.Normalize(direction);
        float bestDistance = maxDistance;
        int bestIndex = -1;

        for (int i = 0; i < _mobs.Count; i++)
        {
            MobState mob = _mobs[i];
            if (mob.Health <= 0)
            {
                continue;
            }

            if (!TryIntersectAabb(origin, rayDirection, bestDistance, mob, out float distance))
            {
                continue;
            }

            bestDistance = distance;
            bestIndex = i;
        }

        if (bestIndex < 0)
        {
            return default;
        }

        MobState hitMob = _mobs[bestIndex];
        return new MobHitResult(true, bestIndex, hitMob.Type, hitMob.Position, bestDistance);
    }

    public bool TryDamageMob(MobHitResult hit, int damage, Vector3 sourcePosition)
    {
        if (!hit.Hit || damage <= 0 || _mobs.Count == 0)
        {
            return false;
        }

        int index = ResolveHitIndex(hit);
        if (index < 0)
        {
            return false;
        }

        MobState mob = _mobs[index];
        if (mob.Health <= 0)
        {
            return false;
        }

        mob.Health = Math.Max(0, mob.Health - damage);
        ApplyDamageResponse(mob, sourcePosition);
        mob.HurtTimer = HurtFlashSeconds;

        Vector3 away = mob.Position - sourcePosition;
        away.Y = 0f;
        if (away.LengthSquared() > float.Epsilon)
        {
            away = Vector3.Normalize(away);
            float knockback = MobKnockback * GetKnockbackScale(mob);
            mob.Velocity += new Vector3(away.X * knockback, 2.5f, away.Z * knockback);
        }
        else
        {
            mob.Velocity += new Vector3(0f, 2.5f, 0f);
        }

        Enqueue(MobEventKind.Hurt, mob.Type, mob.Position, 1f, mob.Elite, mob.EliteVariant);

        if (mob.Health <= 0)
        {
            Enqueue(MobEventKind.Death, mob.Type, mob.Position, 1f, mob.Elite, mob.EliteVariant);
            _mobs.RemoveAt(index);
        }

        _renderDirty = true;
        return true;
    }

    internal void ApplyExplosion(Vector3 position, float radius, int baseDamage, float knockbackStrength, Player player, MobState? sourceMob = null)
    {
        if (radius <= 0f || baseDamage <= 0)
        {
            return;
        }

        bool anyHit = false;

        float playerDistance = Vector3.Distance(position, player.Position);
        if (playerDistance <= radius)
        {
            float falloff = 1f - Math.Clamp(playerDistance / radius, 0f, 1f);
            int damage = Math.Max(1, (int)MathF.Round(baseDamage * falloff));
            Vector3 knockback = BuildKnockback(position, player.Position, knockbackStrength * falloff);
            player.TryApplyDamage(damage, knockback, _config.PlayerDamageCooldownSeconds);
            anyHit = true;
        }

        float radiusSquared = radius * radius;
        for (int i = _mobs.Count - 1; i >= 0; i--)
        {
            MobState mob = _mobs[i];
            if (ReferenceEquals(mob, sourceMob))
            {
                continue;
            }

            float distanceSquared = Vector3.DistanceSquared(position, mob.Position);
            if (distanceSquared > radiusSquared)
            {
                continue;
            }

            float distance = MathF.Sqrt(distanceSquared);
            float falloff = 1f - Math.Clamp(distance / radius, 0f, 1f);
            int damage = Math.Max(1, (int)MathF.Round(baseDamage * falloff));
            mob.Health = Math.Max(0, mob.Health - damage);
            ApplyDamageResponse(mob, position);
            mob.HurtTimer = HurtFlashSeconds;

            Vector3 knockback = BuildKnockback(position, mob.Position, knockbackStrength * falloff * GetKnockbackScale(mob));
            mob.Velocity += knockback;

            if (mob.Health <= 0)
            {
                Enqueue(MobEventKind.Death, mob.Type, mob.Position, 1f, mob.Elite, mob.EliteVariant);
            }
            else
            {
                Enqueue(MobEventKind.Hurt, mob.Type, mob.Position, falloff, mob.Elite, mob.EliteVariant);
            }

            anyHit = true;
        }

        if (anyHit)
        {
            _renderDirty = true;
        }
    }

    public void Clear()
    {
        _mobs.Clear();
        _renderInstances.Clear();
        _eventQueue.Clear();
        _spawnTimer = _config.SpawnIntervalSeconds;
        _renderDirty = false;
        _eventOverflowLogged = false;
    }

    private bool UpdateMob(MobState mob, float dt, WorldType world, WorldEditor editor, Player player, Vector3 playerPosition, float sunIntensity, Action<BlockCoord>? primeTnt)
    {
        mob.Age += dt;
        mob.AttackCooldown = Math.Max(0f, mob.AttackCooldown - dt);
        mob.WanderTimer = Math.Max(0f, mob.WanderTimer - dt);
        mob.IdleTimer = Math.Max(0f, mob.IdleTimer - dt);
        mob.HurtTimer = Math.Max(0f, mob.HurtTimer - dt);
        mob.StaggerTimer = Math.Max(0f, mob.StaggerTimer - dt);
        mob.PursuitTimer = Math.Max(0f, mob.PursuitTimer - dt);
        mob.RageTimer = Math.Max(0f, mob.RageTimer - dt);

        if (mob.Health <= 0)
        {
            return false;
        }

        if (!world.HasChunkAt(MathUtil.FloorToInt(mob.Position.X), MathUtil.FloorToInt(mob.Position.Z)))
        {
            return false;
        }

        if (Vector3.DistanceSquared(mob.Position, playerPosition) > _config.DespawnRadius * _config.DespawnRadius)
        {
            return false;
        }

        ReadEnvironment(world, mob.Position, out bool inWater, out bool inLava, out BlockId groundBlock);
        if (inLava && ApplyContinuousDamage(mob, _config.LavaDamagePerSecond, dt, MobEventKind.Hurt))
        {
            return false;
        }

        if (mob.Definition.BurnsInSun &&
            sunIntensity >= SunBurnThreshold &&
            !inWater &&
            IsSkyExposed(world, mob.Position, mob.Definition.Height) &&
            ApplyContinuousDamage(mob, mob.Definition.SunDamagePerSecond * sunIntensity, dt, MobEventKind.Hurt))
        {
            return false;
        }

        HandleCombat(mob, player, playerPosition, world, editor, dt, primeTnt);
        if (mob.Health <= 0)
        {
            return false;
        }

        Vector2 desiredDirection = ComputeDesiredDirection(mob, world, playerPosition, inWater || inLava, out float moveSpeed);
        ApplyMovement(mob, desiredDirection, moveSpeed, dt, world, inWater, inLava);
        EmitStepEventIfNeeded(mob, groundBlock, dt);
        EmitAmbientEventIfNeeded(mob);

        _renderDirty = true;
        return mob.Health > 0;
    }

    private void HandleCombat(MobState mob, Player player, Vector3 playerPosition, WorldType world, WorldEditor editor, float dt, Action<BlockCoord>? primeTnt)
    {
        if (mob.StaggerTimer > 0f)
        {
            mob.SpecialActive = false;
            mob.SpecialTimer = 0f;
            return;
        }

        Vector3 toPlayer = playerPosition - mob.Position;
        Vector2 toPlayerFlat = new(toPlayer.X, toPlayer.Z);
        float distanceToPlayer = toPlayerFlat.Length();
        bool verticalOverlap = MathF.Abs(toPlayer.Y) <= MathF.Max(1.6f, mob.Definition.Height);
        bool inAggroRadius = distanceToPlayer <= mob.Definition.AggroRadius;

        if (!mob.Definition.Hostile || !verticalOverlap)
        {
            mob.SpecialActive = false;
            mob.SpecialTimer = 0f;
            return;
        }

        if (inAggroRadius)
        {
            mob.PursuitTimer = Math.Max(mob.PursuitTimer, GetPursuitSeconds(mob));
            mob.LastKnownTargetPosition = playerPosition;
        }
        else if (mob.PursuitTimer <= 0f)
        {
            mob.SpecialActive = false;
            mob.SpecialTimer = 0f;
            return;
        }

        if (mob.Definition.AttackMode == MobAttackMode.Melee)
        {
            mob.SpecialActive = false;
            mob.SpecialTimer = 0f;

            if (inAggroRadius && distanceToPlayer <= mob.Definition.AttackRange && mob.AttackCooldown <= 0f)
            {
                Vector3 knockback = BuildKnockback(mob.Position, playerPosition, ExplosionKnockback * 0.55f);
                bool hitPlayer = player.TryApplyDamage(GetAttackDamage(mob), knockback, _config.PlayerDamageCooldownSeconds);
                mob.AttackCooldown = mob.Definition.AttackCooldownSeconds;
                if (hitPlayer)
                {
                    Enqueue(MobEventKind.Attack, mob.Type, mob.Position, 1f, mob.Elite, mob.EliteVariant);
                }
            }

            return;
        }

        if (mob.Definition.AttackMode != MobAttackMode.Fuse)
        {
            return;
        }

        Vector3 mobEyePosition = mob.Position + new Vector3(0f, mob.Definition.Height * 0.85f, 0f);
        bool hasLineOfSight = HasLineOfSight(world, mobEyePosition, player.EyePosition);
        float fuseCancelRange = mob.Definition.FuseCancelRange > 0f ? mob.Definition.FuseCancelRange : mob.Definition.AttackRange;
        bool canStartFuse = inAggroRadius && distanceToPlayer <= mob.Definition.AttackRange && hasLineOfSight;
        bool canMaintainFuse = distanceToPlayer <= fuseCancelRange && hasLineOfSight;

        if (!canMaintainFuse)
        {
            mob.SpecialActive = false;
            mob.SpecialTimer = 0f;
            return;
        }

        if (!mob.SpecialActive)
        {
            if (!canStartFuse)
            {
                return;
            }

            mob.SpecialActive = true;
            mob.SpecialTimer = 0f;
            Enqueue(MobEventKind.Attack, mob.Type, mob.Position, 1f, mob.Elite, mob.EliteVariant);
        }

        mob.SpecialTimer += dt;
        if (mob.SpecialTimer >= mob.Definition.FuseSeconds)
        {
            Explode(mob, player, world, editor, primeTnt);
        }
    }

    private void Explode(MobState mob, Player player, WorldType world, WorldEditor editor, Action<BlockCoord>? primeTnt)
    {
        Vector3 explosionCenter = mob.Position + new Vector3(0f, mob.Definition.Height * 0.5f, 0f);
        BlockCoord source = new(MathUtil.FloorToInt(explosionCenter.X), MathUtil.FloorToInt(explosionCenter.Y), MathUtil.FloorToInt(explosionCenter.Z));
        ApplyExplosion(explosionCenter, ExplosionRadius, GetAttackDamage(mob), ExplosionKnockback, player, mob);
        int affectedBlocks = ExplosionSystem.DestroyBlocks(
            world,
            editor,
            explosionCenter,
            ExplosionRadius,
            CreeperExplosionResistanceScale,
            CreeperExplosionMaxAffectedBlocks,
            source,
            primeTnt);
        mob.Health = 0;
        float intensity = Math.Clamp(1f + affectedBlocks / 64f, 0.85f, 1.35f);
        Enqueue(MobEventKind.Explosion, mob.Type, explosionCenter, intensity, mob.Elite, mob.EliteVariant);
        Enqueue(MobEventKind.Death, mob.Type, explosionCenter, 1f, mob.Elite, mob.EliteVariant);
    }

    private Vector2 ComputeDesiredDirection(MobState mob, WorldType world, Vector3 playerPosition, bool swimming, out float speed)
    {
        speed = 0f;
        Vector3 toPlayer = playerPosition - mob.Position;
        Vector2 toPlayerFlat = new(toPlayer.X, toPlayer.Z);
        float distanceToPlayer = toPlayerFlat.Length();
        float speedScale = GetSpeedScale(mob);

        if (mob.Definition.AttackMode == MobAttackMode.Fuse && mob.SpecialActive)
        {
            speed = 0f;
            return Vector2.Zero;
        }

        if (mob.StaggerTimer > 0f)
        {
            Vector3 awayFromDamage = mob.Position - mob.LastDamageSourcePosition;
            Vector2 retreat = new(awayFromDamage.X, awayFromDamage.Z);
            if (retreat.LengthSquared() > float.Epsilon)
            {
                speed = mob.Definition.Hostile
                    ? mob.Definition.WanderSpeed * speedScale
                    : mob.Definition.FleeSpeed * speedScale;

                if (swimming)
                {
                    speed *= mob.Definition.WaterSpeedMultiplier;
                }

                Vector2 separation = ComputeSeparation(mob);
                if (separation.LengthSquared() > float.Epsilon)
                {
                    retreat = MobNavigation.NormalizeOrZero(retreat + separation * (SeparationStrength * 0.65f));
                }

                return SteerDirection(mob, world, retreat, speed, swimming, out speed);
            }
        }

        Vector3 pursuitTarget = mob.PursuitTimer > 0f ? mob.LastKnownTargetPosition : playerPosition;
        Vector3 toPursuitTarget = pursuitTarget - mob.Position;
        Vector2 toPursuitFlat = new(toPursuitTarget.X, toPursuitTarget.Z);
        float distanceToPursuitTarget = toPursuitFlat.Length();

        if (mob.Definition.Hostile && distanceToPursuitTarget > 0.001f && (distanceToPlayer <= mob.Definition.AggroRadius || mob.PursuitTimer > 0f))
        {
            Vector2 chase = MobNavigation.NormalizeOrZero(toPursuitFlat);
            speed = mob.Definition.ChaseSpeed * speedScale;

            if (mob.PursuitTimer > 0f)
            {
                speed *= Math.Max(0f, _config.HostilePursuitSpeedMultiplier);
            }

            if (mob.Definition.AttackMode == MobAttackMode.Fuse && mob.SpecialActive)
            {
                speed *= 0.4f;
            }

            if (swimming)
            {
                speed *= mob.Definition.WaterSpeedMultiplier;
            }

            Vector2 separation = ComputeSeparation(mob);
            if (separation.LengthSquared() > float.Epsilon)
            {
                chase = MobNavigation.NormalizeOrZero(chase + separation * SeparationStrength);
            }

            return SteerDirection(mob, world, chase, speed, swimming, out speed);
        }

        if (!mob.Definition.Hostile && mob.Definition.AlertRadius > 0f)
        {
            bool playerThreat = distanceToPlayer <= mob.Definition.AlertRadius && distanceToPlayer > 0.001f;
            bool hostileThreat = TryGetNearestHostilePosition(mob.Position, mob.Definition.AlertRadius, out Vector3 hostilePosition, out float hostileDistance);
            if (playerThreat || hostileThreat)
            {
                Vector3 threatPosition = playerPosition;
                if (hostileThreat && (!playerThreat || hostileDistance < distanceToPlayer))
                {
                    threatPosition = hostilePosition;
                }

                Vector3 awayVector = mob.Position - threatPosition;
                Vector2 flee = MobNavigation.NormalizeOrZero(new Vector2(awayVector.X, awayVector.Z));
                speed = mob.Definition.FleeSpeed * speedScale;

                if (swimming)
                {
                    speed *= mob.Definition.WaterSpeedMultiplier;
                }

                Vector2 separation = ComputeSeparation(mob);
                if (separation.LengthSquared() > float.Epsilon)
                {
                    flee = MobNavigation.NormalizeOrZero(flee + separation * SeparationStrength);
                }

                return SteerDirection(mob, world, flee, speed, swimming, out speed);
            }
        }

        if (mob.WanderTimer <= 0f || !HasGroundAhead(world, mob.Position, mob.WanderAngle, Math.Max(mob.Definition.Width, _config.EdgeAvoidDistance)))
        {
            mob.WanderAngle = (float)(_random.NextDouble() * Math.PI * 2d);
            mob.WanderTimer = Math.Max(0.4f, mob.Definition.WanderChangeSeconds * (0.7f + (float)_random.NextDouble() * 0.6f));
        }

        speed = _random.NextDouble() < 0.12d * Math.Min(1f, mob.Age / 4f)
            ? mob.Definition.WalkSpeed * 0.6f
            : mob.Definition.WanderSpeed;
        speed *= speedScale;

        if (swimming)
        {
            speed *= mob.Definition.WaterSpeedMultiplier;
        }

        Vector2 wander = MobNavigation.NormalizeOrZero(new Vector2(MathF.Cos(mob.WanderAngle), MathF.Sin(mob.WanderAngle)));
        Vector2 separationWander = ComputeSeparation(mob);
        if (separationWander.LengthSquared() > float.Epsilon)
        {
            wander = MobNavigation.NormalizeOrZero(wander + separationWander * SeparationStrength);
        }

        return SteerDirection(mob, world, wander, speed, swimming, out speed);
    }

    private bool TryGetNearestHostilePosition(Vector3 position, float radius, out Vector3 hostilePosition, out float hostileDistance)
    {
        hostilePosition = default;
        hostileDistance = float.MaxValue;
        if (radius <= 0f)
        {
            return false;
        }

        float radiusSquared = radius * radius;
        bool found = false;
        for (int i = 0; i < _mobs.Count; i++)
        {
            MobState mob = _mobs[i];
            if (!mob.Definition.Hostile || mob.Health <= 0)
            {
                continue;
            }

            float distanceSquared = Vector3.DistanceSquared(mob.Position, position);
            if (distanceSquared > radiusSquared || distanceSquared >= hostileDistance * hostileDistance)
            {
                continue;
            }

            hostileDistance = MathF.Sqrt(distanceSquared);
            hostilePosition = mob.Position;
            found = true;
        }

        return found;
    }

    private Vector2 ComputeSeparation(MobState mob)
    {
        float radius = Math.Max(SeparationRadius, mob.Definition.Width * 2.2f);
        float radiusSquared = radius * radius;
        Vector2 separation = Vector2.Zero;
        int count = 0;

        for (int i = 0; i < _mobs.Count; i++)
        {
            MobState other = _mobs[i];
            if (ReferenceEquals(other, mob) || other.Health <= 0)
            {
                continue;
            }

            Vector2 delta = new(other.Position.X - mob.Position.X, other.Position.Z - mob.Position.Z);
            float distanceSquared = delta.LengthSquared();
            if (distanceSquared <= float.Epsilon || distanceSquared > radiusSquared)
            {
                continue;
            }

            float distance = MathF.Sqrt(distanceSquared);
            float falloff = 1f - Math.Clamp(distance / radius, 0f, 1f);
            separation -= delta / distance * falloff;
            count++;
        }

        if (count == 0)
        {
            return Vector2.Zero;
        }

        return separation / count;
    }

    private Vector2 SteerDirection(MobState mob, WorldType world, Vector2 preferredDirection, float baseSpeed, bool swimming, out float adjustedSpeed)
    {
        preferredDirection = MobNavigation.NormalizeOrZero(preferredDirection);
        if (preferredDirection.LengthSquared() <= float.Epsilon)
        {
            adjustedSpeed = 0f;
            return Vector2.Zero;
        }

        float turnDistance = Math.Max(mob.Definition.Width, _config.EdgeAvoidDistance);
        ReadOnlySpan<float> turnAngles = stackalloc float[]
        {
            0f,
            0.35f,
            -0.35f,
            0.7f,
            -0.7f,
            1.05f,
            -1.05f
        };

        Span<MobNavigationSample> samples = stackalloc MobNavigationSample[turnAngles.Length];
        for (int i = 0; i < turnAngles.Length; i++)
        {
            float turn = turnAngles[i];
            Vector2 candidate = MobNavigation.NormalizeOrZero(MobNavigation.Rotate(preferredDirection, turn));
            if (candidate.LengthSquared() <= float.Epsilon)
            {
                samples[i] = new MobNavigationSample(Vector2.Zero, 0f, false, false);
                continue;
            }

            float yaw = MathF.Atan2(candidate.Y, candidate.X);
            bool hasGroundAhead = swimming || HasGroundAhead(world, mob.Position, yaw, turnDistance);
            bool hasHeadClear = !HasBlockedHead(world, mob.Position, yaw, mob.Definition.Width);
            float candidateSpeed = baseSpeed;
            float turnMagnitude = MathF.Abs(turn);
            if (turnMagnitude > 1f)
            {
                candidateSpeed *= 0.82f;
            }
            else if (turnMagnitude > 0.6f)
            {
                candidateSpeed *= 0.9f;
            }
            else if (turnMagnitude > 0.2f)
            {
                candidateSpeed *= 0.96f;
            }

            if (!hasGroundAhead)
            {
                candidateSpeed *= 0.9f;
            }

            if (!hasHeadClear)
            {
                candidateSpeed *= 0.7f;
            }

            samples[i] = new MobNavigationSample(candidate, candidateSpeed, hasGroundAhead, hasHeadClear);
        }

        Vector2 selected = MobNavigation.SelectBestDirection(preferredDirection, samples, out adjustedSpeed);
        if (selected.LengthSquared() > float.Epsilon)
        {
            mob.Yaw = MathF.Atan2(selected.Y, selected.X);
        }

        return selected;
    }

    private void ApplyMovement(MobState mob, Vector2 direction, float speed, float dt, WorldType world, bool inWater, bool inLava)
    {
        Vector3 position = mob.Position;
        Vector3 velocity = mob.Velocity;
        float environmentSpeed = speed;
        if (inWater || inLava)
        {
            environmentSpeed *= _config.WaterSlowMultiplier;
        }

        velocity.X = direction.X * environmentSpeed;
        velocity.Z = direction.Y * environmentSpeed;

        if (inWater || inLava)
        {
            velocity.Y += _config.Gravity * 0.15f * dt;
            float buoyancyScale = inLava ? 0.55f : 1f;
            velocity.Y += (_config.WaterBuoyancy + mob.Definition.WaterBuoyancy) * 0.35f * buoyancyScale * dt;
            velocity.Y = Math.Clamp(velocity.Y, -2.2f, 2.8f);

            if (inWater)
            {
                Vector3 waterCurrent = LiquidCurrentSampler.SampleWaterCurrent(world, mob.Position, mob.Definition.Width, mob.Definition.Height);
                if (waterCurrent.LengthSquared() > float.Epsilon)
                {
                    velocity += waterCurrent * _config.WaterCurrentMultiplier;
                }
            }

            if ((direction.LengthSquared() > 0.001f && HasBlockedHead(world, mob)) || position.Y < mob.HomePosition.Y - 1f)
            {
                velocity.Y = Math.Max(velocity.Y, mob.Definition.HopVelocity * 0.45f * GetSpeedScale(mob));
            }
        }
        else
        {
            velocity.Y += _config.Gravity * dt;
        }

        if (mob.OnGround && direction.LengthSquared() > 0.001f && HasBlockedHead(world, mob))
        {
            float jumpVelocity = Math.Max(mob.Definition.HopVelocity, mob.Definition.JumpVelocity) * GetSpeedScale(mob);
            velocity.Y = jumpVelocity;
            mob.OnGround = false;
        }

        var collider = new CharacterCollider(mob.Definition.Width, mob.Definition.Height);
        CharacterPhysics.MoveWithStep(
            world,
            ref position,
            ref velocity,
            collider,
            dt,
            Math.Min(_config.StepHeight, mob.Definition.StepHeight),
            mob.OnGround,
            out bool onGround);

        mob.OnGround = onGround;
        CharacterPhysics.TryResolveInsideSolid(world, ref position, ref velocity, collider);
        mob.Position = position;
        mob.Velocity = velocity;
    }

    private void EmitStepEventIfNeeded(MobState mob, BlockId groundBlock, float dt)
    {
        if (!mob.OnGround)
        {
            return;
        }

        float horizontalDistance = new Vector2(mob.Velocity.X, mob.Velocity.Z).Length() * dt;
        if (horizontalDistance <= 0.001f)
        {
            return;
        }

        mob.StepDistance += horizontalDistance;
        float threshold = Math.Max(0.35f, mob.Definition.Width * 0.9f);
        if (mob.StepDistance < threshold)
        {
            return;
        }

        mob.StepDistance -= threshold;
        float intensity = LiquidBlocks.IsLiquid(groundBlock) ? 0.35f : 1f;
        Enqueue(MobEventKind.Step, mob.Type, mob.Position, intensity, mob.Elite, mob.EliteVariant);
    }

    private void EmitAmbientEventIfNeeded(MobState mob)
    {
        if (mob.Definition.IdleSoundIntervalSeconds <= 0f || mob.IdleTimer > 0f)
        {
            return;
        }

        mob.IdleTimer = mob.Definition.IdleSoundIntervalSeconds * (0.75f + (float)_random.NextDouble() * 0.7f);
        Enqueue(MobEventKind.Ambient, mob.Type, mob.Position, 1f, mob.Elite, mob.EliteVariant);
    }

    private void TrySpawn(float dt, WorldType world, Vector3 playerPosition, float sunIntensity, float rainIntensity)
    {
        if (_mobs.Count >= _config.MaxAlive || _config.SpawnIntervalSeconds <= 0f)
        {
            return;
        }

        _spawnTimer -= dt;
        if (_spawnTimer > 0f)
        {
            return;
        }

        _spawnTimer = _config.SpawnIntervalSeconds;
        for (int attempt = 0; attempt < _config.SpawnAttemptsPerTick && _mobs.Count < _config.MaxAlive; attempt++)
        {
            if (TrySpawnOne(world, playerPosition, sunIntensity, rainIntensity))
            {
                _renderDirty = true;
            }
        }
    }

    private bool TrySpawnOne(WorldType world, Vector3 playerPosition, float sunIntensity, float rainIntensity)
    {
        float angle = (float)(_random.NextDouble() * Math.PI * 2d);
        float distance = MinSpawnDistance + (float)_random.NextDouble() * Math.Max(0f, _config.SpawnRadius - MinSpawnDistance);
        int worldX = MathUtil.FloorToInt(playerPosition.X + MathF.Cos(angle) * distance);
        int worldZ = MathUtil.FloorToInt(playerPosition.Z + MathF.Sin(angle) * distance);

        if (!world.HasChunkAt(worldX, worldZ))
        {
            return false;
        }

        if (!TryFindSpawnPosition(world, worldX, worldZ, out Vector3 spawnPosition, out BiomeId biome, out bool submerged))
        {
            return false;
        }

        if (Vector3.DistanceSquared(spawnPosition, playerPosition) < MinSpawnDistance * MinSpawnDistance)
        {
            return false;
        }

        bool skyExposed = IsSkyExposed(world, spawnPosition, 1.9f);
        if (!TrySelectSpawnDefinition(biome, sunIntensity, rainIntensity, submerged, skyExposed, out MobDefinition definition))
        {
            return false;
        }

        if (HasMobNearby(spawnPosition, definition.Width))
        {
            return false;
        }

        var collider = new CharacterCollider(definition.Width, definition.Height);
        if (CharacterPhysics.IsColliding(world, spawnPosition, collider))
        {
            return false;
        }

        bool elite = ShouldSpawnElite(definition);
        EliteMobVariant eliteVariant = elite && _random.NextDouble() < _config.EliteVariantChance
            ? EliteMobVariantCatalog.SelectSpecialVariant(_random)
            : EliteMobVariant.None;
        var mob = CreateMobState(
            definition,
            spawnPosition,
            (float)(_random.NextDouble() * Math.PI * 2d),
            (float)(_random.NextDouble() * Math.PI * 2d),
            spawnPosition,
            elite: elite,
            eliteVariant: eliteVariant);
        mob.LastKnownTargetPosition = playerPosition;

        _mobs.Add(mob);
        return true;
    }

    private bool TryFindSpawnPosition(WorldType world, int worldX, int worldZ, out Vector3 spawnPosition, out BiomeId biome, out bool submerged)
    {
        spawnPosition = default;
        submerged = false;
        biome = BiomeId.Plains;

        ChunkCoord coord = WorldType.ToChunkCoord(worldX, worldZ);
        Chunk? chunk = world.GetChunk(coord);
        if (chunk is null)
        {
            return false;
        }

        (int localX, int localZ) = WorldType.ToLocalCoord(worldX, worldZ);
        biome = chunk.GetBiome(localX, localZ);

        for (int y = Chunk.SizeY - 3; y >= 1; y--)
        {
            BlockId ground = world.GetBlock(worldX, y, worldZ);
            if (ground == BlockId.Air)
            {
                continue;
            }

            BlockDefinition groundDef = BlockRegistry.Get(ground);
            bool liquidGround = LiquidBlocks.IsLiquid(ground);
            if ((!groundDef.IsSolid || groundDef.IsFoliage) && !liquidGround)
            {
                continue;
            }

            BlockId body = world.GetBlock(worldX, y + 1, worldZ);
            BlockId head = world.GetBlock(worldX, y + 2, worldZ);
            if (BlockRegistry.Get(body).IsSolid || BlockRegistry.Get(head).IsSolid)
            {
                continue;
            }

            submerged = LiquidBlocks.IsLiquid(body) || LiquidBlocks.IsLiquid(head);
            spawnPosition = new Vector3(worldX + 0.5f, y + 1f, worldZ + 0.5f);
            return true;
        }

        return false;
    }

    private bool TrySelectSpawnDefinition(BiomeId biome, float sunIntensity, float rainIntensity, bool submerged, bool skyExposed, out MobDefinition definition)
    {
        definition = null!;
        float totalWeight = 0f;

        foreach (KeyValuePair<MobType, MobDefinition> pair in MobCatalog.All)
        {
            totalWeight += GetSpawnWeight(pair.Value, biome, sunIntensity, rainIntensity, submerged, skyExposed);
        }

        if (totalWeight <= 0f)
        {
            return false;
        }

        float roll = (float)_random.NextDouble() * totalWeight;
        foreach (KeyValuePair<MobType, MobDefinition> pair in MobCatalog.All)
        {
            MobDefinition candidate = pair.Value;
            roll -= GetSpawnWeight(candidate, biome, sunIntensity, rainIntensity, submerged, skyExposed);
            if (roll <= 0f)
            {
                definition = candidate;
                return true;
            }
        }

        return false;
    }

    private float GetSpawnWeight(MobDefinition definition, BiomeId biome, float sunIntensity, float rainIntensity, bool submerged, bool skyExposed)
    {
        return MobSpawnRules.GetWeight(definition, _config, biome, sunIntensity, rainIntensity, submerged, skyExposed);
    }

    private bool HasMobNearby(Vector3 position, float width)
    {
        float radius = MathF.Max(3.25f, width * 4f);
        float radiusSquared = radius * radius;

        for (int i = 0; i < _mobs.Count; i++)
        {
            if (Vector3.DistanceSquared(_mobs[i].Position, position) < radiusSquared)
            {
                return true;
            }
        }

        return false;
    }

    public bool HasHostileNearby(Vector3 position, float radius)
    {
        if (radius <= 0f)
        {
            return false;
        }

        float radiusSquared = radius * radius;
        for (int i = 0; i < _mobs.Count; i++)
        {
            MobState mob = _mobs[i];
            if (!mob.Definition.Hostile || mob.Health <= 0)
            {
                continue;
            }

            if (Vector3.DistanceSquared(mob.Position, position) <= radiusSquared)
            {
                return true;
            }
        }

        return false;
    }

    private bool ResolveMobCrowding()
    {
        bool changed = false;
        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < _mobs.Count; i++)
            {
                MobState first = _mobs[i];
                if (first.Health <= 0)
                {
                    continue;
                }

                for (int j = i + 1; j < _mobs.Count; j++)
                {
                    MobState second = _mobs[j];
                    if (second.Health <= 0)
                    {
                        continue;
                    }

                    Vector2 delta = new(second.Position.X - first.Position.X, second.Position.Z - first.Position.Z);
                    float distanceSquared = delta.LengthSquared();
                    float minDistance = (first.Definition.Width + second.Definition.Width) * 0.5f + 0.08f;
                    float minDistanceSquared = minDistance * minDistance;
                    if (distanceSquared <= float.Epsilon || distanceSquared >= minDistanceSquared)
                    {
                        continue;
                    }

                    float distance = MathF.Sqrt(distanceSquared);
                    Vector2 normal = delta / distance;
                    float penetration = minDistance - distance;
                    float push = penetration * 0.5f;
                    Vector3 offset = new(normal.X * push, 0f, normal.Y * push);

                    first.Position -= offset;
                    second.Position += offset;
                    first.Velocity -= offset * 1.35f;
                    second.Velocity += offset * 1.35f;
                    changed = true;
                }
            }
        }

        return changed;
    }

    private bool ApplyContinuousDamage(MobState mob, float damagePerSecond, float dt, MobEventKind eventKind)
    {
        if (damagePerSecond <= 0f || dt <= 0f)
        {
            return false;
        }

        float expectedDamage = damagePerSecond * dt;
        int damage = (int)MathF.Floor(expectedDamage);
        float fractional = expectedDamage - damage;
        if (_random.NextDouble() < fractional)
        {
            damage++;
        }

        if (damage <= 0)
        {
            return false;
        }

        mob.Health = Math.Max(0, mob.Health - damage);
        mob.HurtTimer = HurtFlashSeconds;
        Enqueue(eventKind, mob.Type, mob.Position, Math.Min(1f, expectedDamage), mob.Elite, mob.EliteVariant);

        if (mob.Health <= 0)
        {
            Enqueue(MobEventKind.Death, mob.Type, mob.Position, 1f, mob.Elite, mob.EliteVariant);
            return true;
        }

        return false;
    }

    private static void ReadEnvironment(WorldType world, Vector3 position, out bool inWater, out bool inLava, out BlockId groundBlock)
    {
        int x = MathUtil.FloorToInt(position.X);
        int y = MathUtil.FloorToInt(position.Y);
        int z = MathUtil.FloorToInt(position.Z);

        BlockId feet = world.GetBlock(x, y, z);
        BlockId chest = world.GetBlock(x, y + 1, z);
        groundBlock = world.GetBlock(x, y - 1, z);

        inWater = LiquidBlocks.IsWater(feet) || LiquidBlocks.IsWater(chest);
        inLava = LiquidBlocks.IsLava(feet) || LiquidBlocks.IsLava(chest);
    }

    private static bool HasLineOfSight(WorldType world, Vector3 origin, Vector3 target)
    {
        Vector3 toTarget = target - origin;
        float distance = toTarget.Length();
        if (distance <= float.Epsilon)
        {
            return false;
        }

        RaycastResult hit = WorldRaycaster.Raycast(world, origin, Vector3.Normalize(toTarget), Math.Max(0f, distance - 0.2f));
        return !hit.Hit;
    }

    private static bool IsSkyExposed(WorldType world, Vector3 position, float height)
    {
        int x = MathUtil.FloorToInt(position.X);
        int z = MathUtil.FloorToInt(position.Z);
        int startY = Math.Min(Chunk.SizeY - 1, MathUtil.FloorToInt(position.Y + height));

        for (int y = startY; y < Chunk.SizeY; y++)
        {
            BlockId block = world.GetBlock(x, y, z);
            if (block == BlockId.Air || LiquidBlocks.IsLiquid(block))
            {
                continue;
            }

            if (BlockRegistry.Get(block).BlocksSkyLight)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasGroundAhead(WorldType world, Vector3 position, float yaw, float distance)
    {
        int x = MathUtil.FloorToInt(position.X + MathF.Cos(yaw) * distance);
        int y = MathUtil.FloorToInt(position.Y - 0.2f);
        int z = MathUtil.FloorToInt(position.Z + MathF.Sin(yaw) * distance);
        BlockId block = world.GetBlock(x, y, z);
        return BlockRegistry.Get(block).IsSolid || LiquidBlocks.IsLiquid(block);
    }

    private static bool HasBlockedHead(WorldType world, MobState mob)
    {
        return HasBlockedHead(world, mob.Position, mob.Yaw, mob.Definition.Width);
    }

    private static bool HasBlockedHead(WorldType world, Vector3 position, float yaw, float width)
    {
        float checkDistance = MathF.Max(0.45f, width * 0.75f);
        int x = MathUtil.FloorToInt(position.X + MathF.Cos(yaw) * checkDistance);
        int y = MathUtil.FloorToInt(position.Y + 0.95f);
        int z = MathUtil.FloorToInt(position.Z + MathF.Sin(yaw) * checkDistance);
        return BlockRegistry.Get(world.GetBlock(x, y, z)).IsSolid;
    }

    internal static Vector3 BuildKnockback(Vector3 source, Vector3 target, float force)
    {
        Vector3 direction = target - source;
        direction.Y = 0f;
        if (direction.LengthSquared() <= float.Epsilon)
        {
            return new Vector3(0f, force * 0.35f, 0f);
        }

        direction = Vector3.Normalize(direction);
        return new Vector3(direction.X * force, force * 0.35f, direction.Z * force);
    }

    private int ResolveHitIndex(MobHitResult hit)
    {
        if (hit.Index >= 0 && hit.Index < _mobs.Count)
        {
            MobState indexed = _mobs[hit.Index];
            if (indexed.Type == hit.Type && Vector3.DistanceSquared(indexed.Position, hit.Position) <= 1f)
            {
                return hit.Index;
            }
        }

        float bestDistance = float.MaxValue;
        int bestIndex = -1;
        for (int i = 0; i < _mobs.Count; i++)
        {
            MobState mob = _mobs[i];
            if (mob.Type != hit.Type)
            {
                continue;
            }

            float distance = Vector3.DistanceSquared(mob.Position, hit.Position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestDistance <= 4f ? bestIndex : -1;
    }

    private void Enqueue(MobEventKind kind, MobType type, Vector3 position, float intensity, bool elite = false, EliteMobVariant eliteVariant = EliteMobVariant.None)
    {
        int limit = Math.Max(1, _config.MaxEventQueue);
        if (_eventQueue.Count >= limit)
        {
            if (!_eventOverflowLogged)
            {
                Log.Warn($"Mob event queue reached the limit of {limit}; dropping oldest events.");
                _eventOverflowLogged = true;
            }

            _eventQueue.Dequeue();
        }

        _eventQueue.Enqueue(new MobEvent(kind, type, position, intensity, elite, eliteVariant));
    }

    private void RefreshRenderInstances()
    {
        if (!_renderDirty)
        {
            return;
        }

        _renderInstances.Clear();
        if (_renderInstances.Capacity < _mobs.Count)
        {
            _renderInstances.Capacity = _mobs.Count;
        }

        for (int i = 0; i < _mobs.Count; i++)
        {
            MobState mob = _mobs[i];
            _renderInstances.Add(BuildRenderInstance(mob));
        }

        _renderDirty = false;
    }

    private MobState CreateMobState(MobDefinition definition, Vector3 position, float yaw, float wanderAngle, Vector3 homePosition, bool elite = false, EliteMobVariant eliteVariant = EliteMobVariant.None, int? health = null)
    {
        bool useElite = elite && definition.Hostile;
        EliteMobVariant normalizedVariant = useElite ? EliteMobVariantCatalog.Normalize(eliteVariant) : EliteMobVariant.None;
        int maxHealth = GetMaxHealth(definition, useElite, normalizedVariant);
        return new MobState(
            definition,
            position,
            Vector3.Zero,
            yaw,
            wanderAngle,
            homePosition,
            Math.Clamp(health ?? maxHealth, 1, maxHealth),
            maxHealth,
            useElite,
            normalizedVariant)
        {
            WanderTimer = Math.Max(0.5f, definition.WanderChangeSeconds * (0.5f + (float)_random.NextDouble())),
            IdleTimer = definition.IdleSoundIntervalSeconds * (0.35f + (float)_random.NextDouble() * 0.65f),
            OnGround = true
        };
    }

    private MobRenderInstance BuildRenderInstance(MobState mob)
    {
        float specialProgress = mob.Definition.FuseSeconds > 0f
            ? Math.Clamp(mob.SpecialTimer / mob.Definition.FuseSeconds, 0f, 1f)
            : 0f;
        float staggerProgress = mob.StaggerTimer > 0f
            ? Math.Clamp(mob.StaggerTimer / Math.Max(0.01f, _config.StaggerSeconds), 0f, 1f)
            : 0f;

        return new MobRenderInstance(
            mob.Type,
            mob.Position,
            mob.Velocity,
            mob.Definition.Width,
            mob.Definition.Height,
            mob.Health,
            mob.MaxHealth,
            mob.Yaw,
            mob.OnGround,
            Math.Clamp(mob.HurtTimer / HurtFlashSeconds, 0f, 1f),
            mob.SpecialActive ? specialProgress : 0f,
            mob.Age,
            mob.Definition.Tint,
            mob.Elite,
            mob.EliteVariant,
            staggerProgress);
    }

    private int GetMaxHealth(MobDefinition definition, bool elite, EliteMobVariant eliteVariant = EliteMobVariant.None)
    {
        if (!elite)
        {
            return definition.MaxHealth;
        }

        EliteMobVariantProfile profile = EliteMobVariantCatalog.Get(eliteVariant);
        int maxHealth = (int)MathF.Ceiling(definition.MaxHealth * _config.EliteHealthMultiplier * profile.HealthMultiplier);
        return Math.Max(definition.MaxHealth, maxHealth);
    }

    private float GetSpeedScale(MobState mob)
    {
        EliteMobVariantProfile profile = EliteMobVariantCatalog.Get(mob.EliteVariant);
        float scale = mob.Elite ? _config.EliteSpeedMultiplier * profile.SpeedMultiplier : 1f;
        if (mob.RageTimer > 0f)
        {
            scale *= profile.RageSpeedMultiplier;
        }

        if (mob.StaggerTimer > 0f)
        {
            scale *= _config.StaggerSpeedMultiplier;
        }

        return scale;
    }

    private float GetPursuitSeconds(MobState mob)
    {
        if (!mob.Definition.Hostile)
        {
            return 0f;
        }

        EliteMobVariantProfile profile = EliteMobVariantCatalog.Get(mob.EliteVariant);
        float pursuitSeconds = _config.HostilePursuitSeconds;
        if (mob.Elite)
        {
            pursuitSeconds *= _config.ElitePursuitMultiplier * profile.PursuitMultiplier;
        }

        if (mob.RageTimer > 0f)
        {
            pursuitSeconds *= profile.RagePursuitMultiplier;
        }

        return pursuitSeconds;
    }

    private int GetAttackDamage(MobState mob)
    {
        EliteMobVariantProfile profile = EliteMobVariantCatalog.Get(mob.EliteVariant);
        if (!mob.Elite)
        {
            int baseDamage = mob.Definition.AttackDamage;
            if (mob.RageTimer > 0f)
            {
                baseDamage = (int)MathF.Ceiling(baseDamage * profile.RageDamageMultiplier);
            }

            return Math.Max(1, baseDamage);
        }

        int damage = (int)MathF.Ceiling(mob.Definition.AttackDamage * _config.EliteDamageMultiplier * profile.DamageMultiplier);
        if (mob.RageTimer > 0f)
        {
            damage = (int)MathF.Ceiling(damage * profile.RageDamageMultiplier);
        }

        return Math.Max(1, damage);
    }

    private float GetKnockbackScale(MobState mob)
    {
        return EliteMobVariantCatalog.Get(mob.EliteVariant).KnockbackScale;
    }

    private bool ShouldSpawnElite(MobDefinition definition)
    {
        return definition.Hostile && _random.NextDouble() < _config.EliteSpawnChance;
    }

    private void ApplyDamageResponse(MobState mob, Vector3 sourcePosition)
    {
        mob.SpecialActive = false;
        mob.SpecialTimer = 0f;
        mob.StaggerTimer = Math.Max(mob.StaggerTimer, _config.StaggerSeconds);
        mob.LastDamageSourcePosition = sourcePosition;

        if (mob.Definition.Hostile)
        {
            EliteMobVariantProfile profile = EliteMobVariantCatalog.Get(mob.EliteVariant);
            mob.RageTimer = Math.Max(mob.RageTimer, profile.RageSeconds);
            mob.PursuitTimer = Math.Max(mob.PursuitTimer, GetPursuitSeconds(mob));
            mob.LastKnownTargetPosition = sourcePosition;
            return;
        }

        Vector3 away = mob.Position - sourcePosition;
        away.Y = 0f;
        if (away.LengthSquared() <= float.Epsilon)
        {
            return;
        }

        away = Vector3.Normalize(away);
        mob.WanderAngle = MathF.Atan2(away.Z, away.X);
        mob.WanderTimer = PassiveEscapeSeconds;
    }

    private static bool TryIntersectAabb(Vector3 origin, Vector3 direction, float maxDistance, MobState mob, out float distance)
    {
        float halfWidth = mob.Definition.Width * 0.5f;
        Vector3 min = new(mob.Position.X - halfWidth, mob.Position.Y, mob.Position.Z - halfWidth);
        Vector3 max = new(mob.Position.X + halfWidth, mob.Position.Y + mob.Definition.Height, mob.Position.Z + halfWidth);

        float tMin = 0f;
        float tMax = maxDistance;

        if (!ClipAxis(origin.X, direction.X, min.X, max.X, ref tMin, ref tMax) ||
            !ClipAxis(origin.Y, direction.Y, min.Y, max.Y, ref tMin, ref tMax) ||
            !ClipAxis(origin.Z, direction.Z, min.Z, max.Z, ref tMin, ref tMax))
        {
            distance = 0f;
            return false;
        }

        distance = tMin >= 0f ? tMin : tMax;
        return distance >= 0f && distance <= maxDistance;
    }

    private static bool ClipAxis(float origin, float direction, float min, float max, ref float tMin, ref float tMax)
    {
        if (MathF.Abs(direction) < 0.0001f)
        {
            return origin >= min && origin <= max;
        }

        float inverse = 1f / direction;
        float t1 = (min - origin) * inverse;
        float t2 = (max - origin) * inverse;
        if (t1 > t2)
        {
            (t1, t2) = (t2, t1);
        }

        tMin = Math.Max(tMin, t1);
        tMax = Math.Min(tMax, t2);
        return tMin <= tMax;
    }
}
