using System.Collections.Generic;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public sealed class MobDefinition
{
    public MobType Type { get; }
    public string TextureName { get; }
    public Vector4 Tint { get; }
    public bool Hostile { get; }
    public MobAttackMode AttackMode { get; }
    public int MaxHealth { get; }
    public float Width { get; }
    public float Height { get; }
    public float WalkSpeed { get; }
    public float ChaseSpeed { get; }
    public float FleeSpeed { get; }
    public float WanderSpeed { get; }
    public float AggroRadius { get; }
    public float AlertRadius { get; }
    public float AttackRange { get; }
    public float FuseCancelRange { get; }
    public int AttackDamage { get; }
    public float AttackCooldownSeconds { get; }
    public float FuseSeconds { get; }
    public float StepHeight { get; }
    public float JumpVelocity { get; }
    public float HopChance { get; }
    public float HopVelocity { get; }
    public float WaterSpeedMultiplier { get; }
    public float WaterBuoyancy { get; }
    public float WanderChangeSeconds { get; }
    public float IdleSoundIntervalSeconds { get; }
    public float DaySpawnMultiplier { get; }
    public float NightSpawnMultiplier { get; }
    public float SpawnWeight { get; }
    public float SunDamagePerSecond { get; }
    public bool BurnsInSun { get; }
    public bool CanSpawnInWater { get; }
    public IReadOnlySet<BiomeId> PreferredBiomes { get; }

    public MobDefinition(
        MobType type,
        string textureName,
        Vector4 tint,
        bool hostile,
        MobAttackMode attackMode,
        int maxHealth,
        float width,
        float height,
        float walkSpeed,
        float chaseSpeed,
        float fleeSpeed,
        float wanderSpeed,
        float aggroRadius,
        float alertRadius,
        float attackRange,
        float fuseCancelRange,
        int attackDamage,
        float attackCooldownSeconds,
        float fuseSeconds,
        float stepHeight,
        float jumpVelocity,
        float hopChance,
        float hopVelocity,
        float waterSpeedMultiplier,
        float waterBuoyancy,
        float wanderChangeSeconds,
        float idleSoundIntervalSeconds,
        float daySpawnMultiplier,
        float nightSpawnMultiplier,
        float spawnWeight,
        float sunDamagePerSecond,
        bool burnsInSun,
        bool canSpawnInWater,
        IReadOnlySet<BiomeId> preferredBiomes)
    {
        Type = type;
        TextureName = textureName;
        Tint = tint;
        Hostile = hostile;
        AttackMode = attackMode;
        MaxHealth = maxHealth;
        Width = width;
        Height = height;
        WalkSpeed = walkSpeed;
        ChaseSpeed = chaseSpeed;
        FleeSpeed = fleeSpeed;
        WanderSpeed = wanderSpeed;
        AggroRadius = aggroRadius;
        AlertRadius = alertRadius;
        AttackRange = attackRange;
        FuseCancelRange = fuseCancelRange;
        AttackDamage = attackDamage;
        AttackCooldownSeconds = attackCooldownSeconds;
        FuseSeconds = fuseSeconds;
        StepHeight = stepHeight;
        JumpVelocity = jumpVelocity;
        HopChance = hopChance;
        HopVelocity = hopVelocity;
        WaterSpeedMultiplier = waterSpeedMultiplier;
        WaterBuoyancy = waterBuoyancy;
        WanderChangeSeconds = wanderChangeSeconds;
        IdleSoundIntervalSeconds = idleSoundIntervalSeconds;
        DaySpawnMultiplier = daySpawnMultiplier;
        NightSpawnMultiplier = nightSpawnMultiplier;
        SpawnWeight = spawnWeight;
        SunDamagePerSecond = sunDamagePerSecond;
        BurnsInSun = burnsInSun;
        CanSpawnInWater = canSpawnInWater;
        PreferredBiomes = preferredBiomes;
    }

    public bool CanSpawnInBiome(BiomeId biome)
    {
        return PreferredBiomes.Contains(biome);
    }
}
