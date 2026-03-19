using System;
using System.Collections.Generic;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.IO;
using MiniCRUFT.Renderer;
using MiniCRUFT.World;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Game;

public enum HerobrineCueKind
{
    Manifest,
    Vanish,
    Footstep,
    Whisper,
    Flicker
}

public readonly struct HerobrineCue
{
    public HerobrineCueKind Kind { get; }
    public Vector3 Position { get; }
    public float Intensity { get; }

    public HerobrineCue(HerobrineCueKind kind, Vector3 position, float intensity)
    {
        Kind = kind;
        Position = position;
        Intensity = intensity;
    }
}

public sealed class HerobrineSystem
{
    private const int MaxCueCount = 16;
    private const int ManifestSpawnAttempts = 16;
    private const int EffectAttempts = 10;
    private const float ManifestDistancePadding = 2f;
    private const float HiddenDistanceScale = 1.8f;
    private const float DirectLookDotThreshold = 0.82f;
    private const float StepCueDistance = 5.5f;
    private const float StepCueSpread = 2.5f;
    private const float TorchFlickerSeconds = 2.4f;
    private const float FoliageFlickerSeconds = 4.5f;

    private readonly HerobrineConfig _config;
    private readonly Random _random;
    private readonly Queue<HerobrineCue> _cues = new();
    private readonly int _seed;
    private HerobrineModeProfile _modeProfile;

    private Vector3 _lastManifestPosition;
    private Vector3 _lastObservedPlayerPosition;
    private float _hauntPressure;
    private float _manifestCooldown;
    private float _eventCooldown;
    private float _worldEffectCooldown;
    private float _activeTimer;
    private float _directLookTimer;
    private float _hiddenTimer;
    private int _encounterCount;
    private bool _isManifested;
    private BlockAnomaly _anomaly;

    public HerobrineSystem(int seed, HerobrineConfig config)
    {
        _seed = seed;
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _random = new Random(seed ^ 0x5F3759DF);
        _modeProfile = HerobrineModeCatalog.Get(_config.Mode);
        _manifestCooldown = RollManifestCooldown();
    }

    public bool IsManifested => _isManifested;

    public float HauntPressure => _hauntPressure;

    public string Mode => HerobrineModeCatalog.Get(_config.Mode).Name;

    public string ModeDescription => HerobrineModeCatalog.Get(_config.Mode).Description;

    public void Load(HerobrineSaveData data)
    {
        if (data.Seed != _seed)
        {
            Log.Warn($"Herobrine save seed {data.Seed} does not match world seed {_seed}, ignoring saved Herobrine state.");
            ResetState();
            return;
        }

        _lastManifestPosition = data.LastManifestPosition;
        _lastObservedPlayerPosition = data.LastObservedPlayerPosition;
        _hauntPressure = Math.Max(0f, data.HauntPressure);
        _manifestCooldown = Math.Max(0f, data.ManifestCooldown);
        _eventCooldown = Math.Max(0f, data.EventCooldown);
        _worldEffectCooldown = Math.Max(0f, data.WorldEffectCooldown);
        _activeTimer = Math.Max(0f, data.ActiveTimer);
        _encounterCount = Math.Max(0, data.EncounterCount);
        _isManifested = data.IsManifested;
        _directLookTimer = 0f;
        _hiddenTimer = 0f;
        _anomaly = default;
        _cues.Clear();
        _modeProfile = HerobrineModeCatalog.Get(_config.Mode);
    }

    public HerobrineSaveData BuildSaveData(Vector3 playerPosition)
    {
        return new HerobrineSaveData(
            seed: _seed,
            lastManifestPosition: _lastManifestPosition,
            lastObservedPlayerPosition: playerPosition,
            hauntPressure: _hauntPressure,
            manifestCooldown: _manifestCooldown,
            eventCooldown: _eventCooldown,
            worldEffectCooldown: _worldEffectCooldown,
            activeTimer: _activeTimer,
            encounterCount: _encounterCount,
            isManifested: _isManifested);
    }

    public void Update(float dt, WorldType world, WorldEditor editor, MobSystem mobs, Player player, Camera camera, float sunIntensity)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(mobs);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(camera);

        if (dt <= 0f)
        {
            return;
        }

        _modeProfile = HerobrineModeCatalog.Get(_config.Mode);

        if (!_config.Enabled)
        {
            Disable(world, editor, mobs);
            return;
        }

        _manifestCooldown = Math.Max(0f, _manifestCooldown - dt);
        _eventCooldown = Math.Max(0f, _eventCooldown - dt);
        _worldEffectCooldown = Math.Max(0f, _worldEffectCooldown - dt);
        _lastObservedPlayerPosition = player.Position;

        UpdateHauntPressure(world, player, sunIntensity, dt);
        UpdateAnomaly(editor, dt);
        UpdateManifestation(world, mobs, player, camera, dt);

        if (!_isManifested && _manifestCooldown <= 0f)
        {
            TryManifest(world, mobs, player);
        }

        if (_eventCooldown <= 0f)
        {
            TryEmitPresenceCue(world, player);
        }

        if (_worldEffectCooldown <= 0f && _config.WorldEffectIntensity > 0f)
        {
            TryApplyWorldEffect(world, editor, player);
        }
    }

    public void Disable(WorldType world, WorldEditor editor, MobSystem mobs)
    {
        RestoreAnomaly(editor);
        _isManifested = false;
        _activeTimer = 0f;
        _directLookTimer = 0f;
        _hiddenTimer = 0f;
        _hauntPressure = 0f;
        _eventCooldown = 0f;
        _worldEffectCooldown = 0f;
        _manifestCooldown = RollManifestCooldown();
        _cues.Clear();
        mobs.DespawnAllOfType(MobType.Herobrine);
    }

    public bool TryDequeueCue(out HerobrineCue cue)
    {
        if (_cues.Count == 0)
        {
            cue = default;
            return false;
        }

        cue = _cues.Dequeue();
        return true;
    }

    public string BuildDebugStatus()
    {
        string manifested = _isManifested ? "active" : "idle";
        return $"Herobrine: {manifested}, mode={Mode}, pressure={_hauntPressure:F2}, manifestCd={_manifestCooldown:F1}, eventCd={_eventCooldown:F1}";
    }

    private void UpdateHauntPressure(WorldType world, Player player, float sunIntensity, float dt)
    {
        float night = 1f - Math.Clamp(sunIntensity, 0f, 1f);
        float cave = IsPlayerUnderground(world, player.Position) ? 1f : 0f;
        float encounterPressure = Math.Min(0.35f, _encounterCount * 0.03f * _modeProfile.PressureRiseMultiplier);
        float target = _modeProfile.PressureFloor +
                       (night * _config.NightBias * 0.22f * _modeProfile.PressureRiseMultiplier) +
                       (cave * _config.CaveBias * 0.18f * _modeProfile.PressureRiseMultiplier) +
                       encounterPressure;
        float response = target >= _hauntPressure ? _modeProfile.PressureRiseMultiplier : _modeProfile.PressureFallMultiplier;
        float blend = Math.Clamp(dt * 0.2f * response, 0f, 1f);
        _hauntPressure += (target - _hauntPressure) * blend;
        _hauntPressure = Math.Clamp(_hauntPressure, 0f, 1f);
    }

    private void UpdateManifestation(WorldType world, MobSystem mobs, Player player, Camera camera, float dt)
    {
        if (!mobs.TryGetMobSnapshot(MobType.Herobrine, out MobRenderInstance herobrine))
        {
            _isManifested = false;
            _activeTimer = 0f;
            _directLookTimer = 0f;
            _hiddenTimer = 0f;
            return;
        }

        _isManifested = true;
        _activeTimer += dt;
        _lastManifestPosition = herobrine.Position;

        Vector3 eyePosition = camera.Position;
        Vector3 toHerobrine = herobrine.Position + new Vector3(0f, herobrine.Height * 0.7f, 0f) - eyePosition;
        float distance = toHerobrine.Length();
        bool visible = distance > 0.001f && HasLineOfSight(world, eyePosition, toHerobrine, distance) && IsFacingTarget(camera, toHerobrine, distance);
        float directLookDespawnSeconds = _config.DirectLookDespawnSeconds * _modeProfile.DirectLookDespawnMultiplier;
        float hiddenTimeoutSeconds = _config.HiddenTimeoutSeconds * _modeProfile.HiddenTimeoutMultiplier;
        float manifestDurationSeconds = _config.ManifestDurationSeconds * _modeProfile.ManifestDurationMultiplier;

        if (visible)
        {
            _directLookTimer += dt;
            _hiddenTimer = 0f;
            if (_directLookTimer >= directLookDespawnSeconds)
            {
                DespawnManifestation(mobs, herobrine.Position, HerobrineCueKind.Vanish, 0.9f);
                return;
            }
        }
        else
        {
            _directLookTimer = 0f;
            if (distance >= _config.MaxManifestDistance * HiddenDistanceScale)
            {
                _hiddenTimer += dt;
            }
            else
            {
                _hiddenTimer = 0f;
            }

            if (_hiddenTimer >= hiddenTimeoutSeconds)
            {
                DespawnManifestation(mobs, herobrine.Position, HerobrineCueKind.Vanish, 0.55f);
                return;
            }
        }

        if (_activeTimer >= manifestDurationSeconds)
        {
            DespawnManifestation(mobs, herobrine.Position, HerobrineCueKind.Vanish, 0.7f);
        }
    }

    private void TryManifest(WorldType world, MobSystem mobs, Player player)
    {
        if (mobs.HasMobOfType(MobType.Herobrine))
        {
            _isManifested = true;
            return;
        }

        if (!TryFindManifestPosition(world, player, out Vector3 spawnPosition, out float yaw))
        {
            _manifestCooldown = Math.Max(4f, _config.EventCooldownSeconds * 0.5f);
            return;
        }

        if (!mobs.TrySpawnScripted(MobType.Herobrine, spawnPosition, yaw, world))
        {
            _manifestCooldown = Math.Max(4f, _config.EventCooldownSeconds * 0.5f);
            return;
        }

        _isManifested = true;
        _activeTimer = 0f;
        _directLookTimer = 0f;
        _hiddenTimer = 0f;
        _encounterCount++;
        _lastManifestPosition = spawnPosition;
        float eventCooldown = Math.Max(1.5f, _config.EventCooldownSeconds * _modeProfile.EventCooldownMultiplier);
        float manifestCooldown = Math.Max(_config.ManifestDurationSeconds * _modeProfile.ManifestDurationMultiplier, eventCooldown * 0.75f);
        _eventCooldown = eventCooldown * 0.5f;
        _manifestCooldown = Math.Max(RollManifestCooldown(), manifestCooldown);
        EnqueueCue(HerobrineCueKind.Manifest, spawnPosition, 0.85f);
        mobs.EmitScriptedEvent(MobEventKind.Ambient, MobType.Herobrine, spawnPosition, 0.9f);
    }

    private void DespawnManifestation(MobSystem mobs, Vector3 position, HerobrineCueKind cueKind, float intensity)
    {
        mobs.DespawnAllOfType(MobType.Herobrine);
        _isManifested = false;
        _activeTimer = 0f;
        _directLookTimer = 0f;
        _hiddenTimer = 0f;
        _manifestCooldown = RollManifestCooldown();
        _eventCooldown = Math.Max(_eventCooldown, _config.EventCooldownSeconds * _modeProfile.EventCooldownMultiplier * 0.6f);
        EnqueueCue(cueKind, position, intensity);
    }

    private void TryEmitPresenceCue(WorldType world, Player player)
    {
        float cueMultiplier = Math.Clamp(_modeProfile.WorldEffectIntensityMultiplier, 0.8f, 1.5f);
        if (TryFindStepCuePosition(world, player, out Vector3 position))
        {
            EnqueueCue(HerobrineCueKind.Footstep, position, (0.55f + Math.Min(0.3f, _hauntPressure)) * cueMultiplier);
        }
        else
        {
            EnqueueCue(HerobrineCueKind.Whisper, player.Position, (0.45f + Math.Min(0.35f, _hauntPressure)) * cueMultiplier);
        }

        _eventCooldown = Math.Max(2f, _config.EventCooldownSeconds * _modeProfile.EventCooldownMultiplier * (0.85f - Math.Min(0.35f, _hauntPressure * 0.2f)));
    }

    private void TryApplyWorldEffect(WorldType world, WorldEditor editor, Player player)
    {
        if (_anomaly.Active)
        {
            _worldEffectCooldown = Math.Max(2f, _config.WorldEffectCooldownSeconds * _modeProfile.WorldEffectCooldownMultiplier * 0.5f);
            return;
        }

        if (_random.NextDouble() > Math.Clamp(_config.WorldEffectIntensity * _modeProfile.WorldEffectIntensityMultiplier, 0f, 1f))
        {
            _worldEffectCooldown = Math.Max(6f, _config.WorldEffectCooldownSeconds * _modeProfile.WorldEffectCooldownMultiplier * 0.5f);
            return;
        }

        for (int attempt = 0; attempt < EffectAttempts; attempt++)
        {
            int x = MathUtil.FloorToInt(player.Position.X) + _random.Next(-6, 7);
            int z = MathUtil.FloorToInt(player.Position.Z) + _random.Next(-6, 7);
            int top = Math.Clamp(MathUtil.FloorToInt(player.Position.Y) + 3, 2, Chunk.SizeY - 3);

            for (int y = top; y >= 2; y--)
            {
                BlockId block = world.GetBlock(x, y, z);
                if (block == BlockId.Torch ||
                    block == BlockId.TorchWallNorth ||
                    block == BlockId.TorchWallSouth ||
                    block == BlockId.TorchWallWest ||
                    block == BlockId.TorchWallEast)
                {
                    if (editor.SetBlock(x, y, z, BlockId.Air))
                    {
                        _anomaly = new BlockAnomaly(new BlockCoord(x, y, z), block, BlockId.Air, TorchFlickerSeconds);
                        EnqueueCue(HerobrineCueKind.Flicker, new Vector3(x + 0.5f, y + 0.5f, z + 0.5f), 0.8f);
                        _worldEffectCooldown = Math.Max(4f, _config.WorldEffectCooldownSeconds * _modeProfile.WorldEffectCooldownMultiplier);
                        return;
                    }
                }

                if (block == BlockId.TallGrass || block == BlockId.Flower)
                {
                    if (editor.SetBlock(x, y, z, BlockId.DeadBush))
                    {
                        _anomaly = new BlockAnomaly(new BlockCoord(x, y, z), block, BlockId.DeadBush, FoliageFlickerSeconds);
                        EnqueueCue(HerobrineCueKind.Flicker, new Vector3(x + 0.5f, y + 0.5f, z + 0.5f), 0.55f);
                        _worldEffectCooldown = Math.Max(4f, _config.WorldEffectCooldownSeconds * _modeProfile.WorldEffectCooldownMultiplier);
                        return;
                    }
                }
            }
        }

        _worldEffectCooldown = Math.Max(6f, _config.WorldEffectCooldownSeconds * _modeProfile.WorldEffectCooldownMultiplier * 0.5f);
    }

    private void UpdateAnomaly(WorldEditor editor, float dt)
    {
        if (!_anomaly.Active)
        {
            return;
        }

        _anomaly.Timer -= dt;
        if (_anomaly.Timer > 0f)
        {
            return;
        }

        RestoreAnomaly(editor);
    }

    private void RestoreAnomaly(WorldEditor editor)
    {
        if (!_anomaly.Active)
        {
            return;
        }

        editor.SetBlock(_anomaly.Position.X, _anomaly.Position.Y, _anomaly.Position.Z, _anomaly.OriginalBlock);
        _anomaly = default;
    }

    private bool TryFindManifestPosition(WorldType world, Player player, out Vector3 spawnPosition, out float yaw)
    {
        spawnPosition = default;
        yaw = 0f;

        float playerYaw = DegreesToRadians(player.Yaw);
        bool preferBehind = _random.NextDouble() < Math.Clamp(_config.BehindPlayerChance + _modeProfile.BehindPlayerChanceBonus, 0f, 1f);
        for (int i = 0; i < ManifestSpawnAttempts; i++)
        {
            float angleOffset = (float)(_random.NextDouble() - 0.5d) * MathF.PI * (preferBehind ? 0.45f : 1.1f);
            float baseAngle = preferBehind
                ? playerYaw + MathF.PI
                : playerYaw + (i % 2 == 0 ? MathF.PI * 0.55f : -MathF.PI * 0.55f);
            float angle = baseAngle + angleOffset;
            float distance = _config.MinManifestDistance + (float)_random.NextDouble() * Math.Max(1f, _config.MaxManifestDistance - _config.MinManifestDistance);
            int worldX = MathUtil.FloorToInt(player.Position.X + MathF.Cos(angle) * distance);
            int worldZ = MathUtil.FloorToInt(player.Position.Z + MathF.Sin(angle) * distance);

            if (!TryFindGroundedPosition(world, worldX, worldZ, out Vector3 candidate))
            {
                continue;
            }

            float candidateDistance = Vector3.Distance(candidate, player.Position);
            if (candidateDistance < _config.MinManifestDistance - ManifestDistancePadding ||
                candidateDistance > _config.MaxManifestDistance + ManifestDistancePadding)
            {
                continue;
            }

            Vector3 toPlayer = player.Position - candidate;
            yaw = MathF.Atan2(toPlayer.Z, toPlayer.X);
            spawnPosition = candidate;
            return true;
        }

        return false;
    }

    private static bool TryFindGroundedPosition(WorldType world, int worldX, int worldZ, out Vector3 position)
    {
        position = default;
        if (!world.HasChunkAt(worldX, worldZ))
        {
            return false;
        }

        for (int y = Chunk.SizeY - 3; y >= 2; y--)
        {
            BlockId ground = world.GetBlock(worldX, y, worldZ);
            BlockId body = world.GetBlock(worldX, y + 1, worldZ);
            BlockId head = world.GetBlock(worldX, y + 2, worldZ);

            if (!BlockRegistry.Get(ground).IsSolid || LiquidBlocks.IsLiquid(ground))
            {
                continue;
            }

            if (BlockRegistry.Get(body).IsSolid || LiquidBlocks.IsLiquid(body))
            {
                continue;
            }

            if (BlockRegistry.Get(head).IsSolid || LiquidBlocks.IsLiquid(head))
            {
                continue;
            }

            position = new Vector3(worldX + 0.5f, y + 1f, worldZ + 0.5f);
            return true;
        }

        return false;
    }

    private bool TryFindStepCuePosition(WorldType world, Player player, out Vector3 position)
    {
        position = default;
        float playerYaw = DegreesToRadians(player.Yaw);
        for (int attempt = 0; attempt < 8; attempt++)
        {
            float angle = playerYaw + MathF.PI + ((float)_random.NextDouble() - 0.5f) * 1.2f;
            float distance = StepCueDistance + ((float)_random.NextDouble() - 0.5f) * StepCueSpread;
            int x = MathUtil.FloorToInt(player.Position.X + MathF.Cos(angle) * distance);
            int z = MathUtil.FloorToInt(player.Position.Z + MathF.Sin(angle) * distance);
            if (!TryFindGroundedPosition(world, x, z, out position))
            {
                continue;
            }

            Vector3 toCandidate = position - player.Position;
            Vector3 forward = new(MathF.Cos(playerYaw), 0f, MathF.Sin(playerYaw));
            toCandidate.Y = 0f;
            if (toCandidate.LengthSquared() <= float.Epsilon)
            {
                continue;
            }

            toCandidate = Vector3.Normalize(toCandidate);
            if (Vector3.Dot(forward, toCandidate) > 0.2f)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool HasLineOfSight(WorldType world, Vector3 eyePosition, Vector3 toTarget, float distance)
    {
        Vector3 direction = Vector3.Normalize(toTarget);
        RaycastResult hit = WorldRaycaster.Raycast(world, eyePosition, direction, Math.Max(0f, distance - 0.2f));
        return !hit.Hit;
    }

    private static bool IsFacingTarget(Camera camera, Vector3 toTarget, float distance)
    {
        if (distance <= float.Epsilon)
        {
            return false;
        }

        Vector3 direction = Vector3.Normalize(toTarget);
        return Vector3.Dot(camera.GetForward(), direction) >= DirectLookDotThreshold;
    }

    private static bool IsPlayerUnderground(WorldType world, Vector3 playerPosition)
    {
        int x = MathUtil.FloorToInt(playerPosition.X);
        int y = MathUtil.FloorToInt(playerPosition.Y + 2f);
        int z = MathUtil.FloorToInt(playerPosition.Z);

        for (int checkY = y; checkY < Chunk.SizeY; checkY++)
        {
            BlockId block = world.GetBlock(x, checkY, z);
            if (block == BlockId.Air || LiquidBlocks.IsLiquid(block))
            {
                continue;
            }

            return BlockRegistry.Get(block).BlocksSkyLight;
        }

        return false;
    }

    private float RollManifestCooldown()
    {
        float minInterval = Math.Max(5f, _config.MinManifestIntervalSeconds * _modeProfile.ManifestIntervalMultiplier);
        float maxInterval = Math.Max(minInterval + 1f, _config.MaxManifestIntervalSeconds * _modeProfile.ManifestIntervalMultiplier);
        float span = Math.Max(1f, maxInterval - minInterval);
        float pressure = Math.Clamp(_hauntPressure, 0f, 1f);
        float randomized = minInterval + (float)_random.NextDouble() * span;
        return Math.Max(5f, randomized * (1f - pressure * 0.35f));
    }

    private static float DegreesToRadians(float degrees)
    {
        return degrees * (MathF.PI / 180f);
    }

    private void EnqueueCue(HerobrineCueKind kind, Vector3 position, float intensity)
    {
        while (_cues.Count >= MaxCueCount)
        {
            _cues.Dequeue();
        }

        _cues.Enqueue(new HerobrineCue(kind, position, intensity));
    }

    private void ResetState()
    {
        _modeProfile = HerobrineModeCatalog.Get(_config.Mode);
        _lastManifestPosition = default;
        _lastObservedPlayerPosition = default;
        _hauntPressure = 0f;
        _manifestCooldown = RollManifestCooldown();
        _eventCooldown = 0f;
        _worldEffectCooldown = 0f;
        _activeTimer = 0f;
        _directLookTimer = 0f;
        _hiddenTimer = 0f;
        _encounterCount = 0;
        _isManifested = false;
        _anomaly = default;
        _cues.Clear();
    }

    private struct BlockAnomaly
    {
        public BlockCoord Position;
        public BlockId OriginalBlock;
        public BlockId CurrentBlock;
        public float Timer;

        public BlockAnomaly(BlockCoord position, BlockId originalBlock, BlockId currentBlock, float timer)
        {
            Position = position;
            OriginalBlock = originalBlock;
            CurrentBlock = currentBlock;
            Timer = timer;
        }

        public bool Active => Timer > 0f;
    }
}
