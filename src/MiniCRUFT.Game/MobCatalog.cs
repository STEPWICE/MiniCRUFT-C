using System;
using System.Collections.Generic;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public static class MobCatalog
{
    private static readonly Dictionary<MobType, MobDefinition> Definitions = new();
    private static readonly object Sync = new();
    private static volatile bool _initialized;

    public static IReadOnlyDictionary<MobType, MobDefinition> All => Definitions;

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        lock (Sync)
        {
            if (_initialized)
            {
                return;
            }

            Register(new MobDefinition(
                MobType.Zombie,
                "zombie",
                new Vector4(0.80f, 1.00f, 0.80f, 1f),
                hostile: true,
                attackMode: MobAttackMode.Melee,
                maxHealth: 20,
                width: 0.6f,
                height: 1.95f,
                walkSpeed: 1.0f,
                chaseSpeed: 2.4f,
                fleeSpeed: 0f,
                wanderSpeed: 0.55f,
                aggroRadius: 18f,
                alertRadius: 0f,
                attackRange: 1.35f,
                fuseCancelRange: 0f,
                attackDamage: 4,
                attackCooldownSeconds: 1.2f,
                fuseSeconds: 0f,
                stepHeight: 0.6f,
                jumpVelocity: 5.8f,
                hopChance: 0.08f,
                hopVelocity: 4.4f,
                waterSpeedMultiplier: 0.45f,
                waterBuoyancy: 5.5f,
                wanderChangeSeconds: 3f,
                idleSoundIntervalSeconds: 8f,
                daySpawnMultiplier: 0.25f,
                nightSpawnMultiplier: 1.0f,
                spawnWeight: 1.0f,
                sunDamagePerSecond: 2.5f,
                burnsInSun: true,
                canSpawnInWater: false,
                preferredBiomes: new HashSet<BiomeId>
                {
                    BiomeId.Forest,
                    BiomeId.Plains,
                    BiomeId.Desert,
                    BiomeId.Mountains,
                    BiomeId.Taiga,
                    BiomeId.Tundra,
                    BiomeId.Swamp,
                    BiomeId.Savanna,
                    BiomeId.Shrubland
                }));

            Register(new MobDefinition(
                MobType.Creeper,
                "creeper",
                new Vector4(0.85f, 1.05f, 0.85f, 1f),
                hostile: true,
                attackMode: MobAttackMode.Fuse,
                maxHealth: 20,
                width: 0.6f,
                height: 1.7f,
                walkSpeed: 1.0f,
                chaseSpeed: 2.2f,
                fleeSpeed: 0f,
                wanderSpeed: 0.55f,
                aggroRadius: 18f,
                alertRadius: 0f,
                attackRange: 3.0f,
                fuseCancelRange: 7f,
                attackDamage: 30,
                attackCooldownSeconds: 0f,
                fuseSeconds: 1.5f,
                stepHeight: 0.6f,
                jumpVelocity: 5.6f,
                hopChance: 0.06f,
                hopVelocity: 4.2f,
                waterSpeedMultiplier: 0.4f,
                waterBuoyancy: 5.5f,
                wanderChangeSeconds: 3.2f,
                idleSoundIntervalSeconds: 10f,
                daySpawnMultiplier: 0.15f,
                nightSpawnMultiplier: 1.0f,
                spawnWeight: 0.6f,
                sunDamagePerSecond: 0f,
                burnsInSun: false,
                canSpawnInWater: false,
                preferredBiomes: new HashSet<BiomeId>
                {
                    BiomeId.Forest,
                    BiomeId.Plains,
                    BiomeId.Mountains,
                    BiomeId.Taiga,
                    BiomeId.Swamp,
                    BiomeId.Savanna,
                    BiomeId.Shrubland
                }));

            Register(new MobDefinition(
                MobType.Cow,
                "cow",
                new Vector4(1.0f, 0.92f, 0.82f, 1f),
                hostile: false,
                attackMode: MobAttackMode.None,
                maxHealth: 10,
                width: 0.9f,
                height: 1.4f,
                walkSpeed: 0.75f,
                chaseSpeed: 0f,
                fleeSpeed: 1.05f,
                wanderSpeed: 0.65f,
                aggroRadius: 0f,
                alertRadius: 6.5f,
                attackRange: 0f,
                fuseCancelRange: 0f,
                attackDamage: 0,
                attackCooldownSeconds: 0f,
                fuseSeconds: 0f,
                stepHeight: 0.45f,
                jumpVelocity: 4.8f,
                hopChance: 0.03f,
                hopVelocity: 3.8f,
                waterSpeedMultiplier: 0.55f,
                waterBuoyancy: 6f,
                wanderChangeSeconds: 4.5f,
                idleSoundIntervalSeconds: 8f,
                daySpawnMultiplier: 1.0f,
                nightSpawnMultiplier: 0.35f,
                spawnWeight: 1.2f,
                sunDamagePerSecond: 0f,
                burnsInSun: false,
                canSpawnInWater: false,
                preferredBiomes: new HashSet<BiomeId>
                {
                    BiomeId.Forest,
                    BiomeId.Plains,
                    BiomeId.Taiga,
                    BiomeId.Savanna,
                    BiomeId.Shrubland
                }));

            Register(new MobDefinition(
                MobType.Sheep,
                "sheep",
                new Vector4(1.0f, 1.0f, 1.0f, 1f),
                hostile: false,
                attackMode: MobAttackMode.None,
                maxHealth: 8,
                width: 0.9f,
                height: 1.3f,
                walkSpeed: 0.8f,
                chaseSpeed: 0f,
                fleeSpeed: 1.1f,
                wanderSpeed: 0.7f,
                aggroRadius: 0f,
                alertRadius: 7.5f,
                attackRange: 0f,
                fuseCancelRange: 0f,
                attackDamage: 0,
                attackCooldownSeconds: 0f,
                fuseSeconds: 0f,
                stepHeight: 0.4f,
                jumpVelocity: 4.8f,
                hopChance: 0.04f,
                hopVelocity: 3.9f,
                waterSpeedMultiplier: 0.55f,
                waterBuoyancy: 6f,
                wanderChangeSeconds: 4f,
                idleSoundIntervalSeconds: 7f,
                daySpawnMultiplier: 1.0f,
                nightSpawnMultiplier: 0.3f,
                spawnWeight: 1.1f,
                sunDamagePerSecond: 0f,
                burnsInSun: false,
                canSpawnInWater: false,
                preferredBiomes: new HashSet<BiomeId>
                {
                    BiomeId.Forest,
                    BiomeId.Plains,
                    BiomeId.Taiga,
                    BiomeId.Tundra,
                    BiomeId.Savanna,
                    BiomeId.Shrubland
                }));

            Register(new MobDefinition(
                MobType.Chicken,
                "chicken",
                new Vector4(1.0f, 1.0f, 1.0f, 1f),
                hostile: false,
                attackMode: MobAttackMode.None,
                maxHealth: 4,
                width: 0.4f,
                height: 0.9f,
                walkSpeed: 0.95f,
                chaseSpeed: 0f,
                fleeSpeed: 1.35f,
                wanderSpeed: 0.8f,
                aggroRadius: 0f,
                alertRadius: 9f,
                attackRange: 0f,
                fuseCancelRange: 0f,
                attackDamage: 0,
                attackCooldownSeconds: 0f,
                fuseSeconds: 0f,
                stepHeight: 0.35f,
                jumpVelocity: 4.5f,
                hopChance: 0.14f,
                hopVelocity: 5.2f,
                waterSpeedMultiplier: 0.6f,
                waterBuoyancy: 6f,
                wanderChangeSeconds: 2.5f,
                idleSoundIntervalSeconds: 6f,
                daySpawnMultiplier: 1.0f,
                nightSpawnMultiplier: 0.35f,
                spawnWeight: 0.9f,
                sunDamagePerSecond: 0f,
                burnsInSun: false,
                canSpawnInWater: false,
                preferredBiomes: new HashSet<BiomeId>
                {
                    BiomeId.Forest,
                    BiomeId.Plains,
                    BiomeId.Taiga,
                    BiomeId.Savanna,
                    BiomeId.Shrubland
                }));

            Register(new MobDefinition(
                MobType.Herobrine,
                "herobrine",
                new Vector4(1.0f, 1.0f, 1.0f, 1f),
                hostile: true,
                attackMode: MobAttackMode.None,
                maxHealth: 20,
                width: 0.6f,
                height: 1.95f,
                walkSpeed: 1.15f,
                chaseSpeed: 2.3f,
                fleeSpeed: 0f,
                wanderSpeed: 0.35f,
                aggroRadius: 28f,
                alertRadius: 0f,
                attackRange: 0f,
                fuseCancelRange: 0f,
                attackDamage: 0,
                attackCooldownSeconds: 0f,
                fuseSeconds: 0f,
                stepHeight: 0.6f,
                jumpVelocity: 5.8f,
                hopChance: 0.03f,
                hopVelocity: 4.8f,
                waterSpeedMultiplier: 0.4f,
                waterBuoyancy: 5.4f,
                wanderChangeSeconds: 5f,
                idleSoundIntervalSeconds: 0f,
                daySpawnMultiplier: 0f,
                nightSpawnMultiplier: 0f,
                spawnWeight: 0f,
                sunDamagePerSecond: 0f,
                burnsInSun: false,
                canSpawnInWater: false,
                preferredBiomes: new HashSet<BiomeId>
                {
                    BiomeId.Forest,
                    BiomeId.Plains,
                    BiomeId.Desert,
                    BiomeId.Mountains,
                    BiomeId.Taiga,
                    BiomeId.Tundra,
                    BiomeId.Swamp,
                    BiomeId.Savanna,
                    BiomeId.Shrubland
                }));

            _initialized = true;
        }
    }

    public static MobDefinition Get(MobType type)
    {
        if (!_initialized)
        {
            Initialize();
        }

        return Definitions[type];
    }

    public static bool TryGet(MobType type, out MobDefinition definition)
    {
        if (!_initialized)
        {
            Initialize();
        }

        return Definitions.TryGetValue(type, out definition!);
    }

    public static float GetConfiguredWeight(MobType type, MobConfig config)
    {
        return type switch
        {
            MobType.Zombie => config.ZombieWeight,
            MobType.Creeper => config.CreeperWeight,
            MobType.Cow => config.CowWeight,
            MobType.Sheep => config.SheepWeight,
            MobType.Chicken => config.ChickenWeight,
            MobType.Herobrine => 0f,
            _ => 0f
        };
    }

    public static float GetDayNightMultiplier(MobDefinition definition, MobConfig config, bool isNight)
    {
        if (definition.Hostile)
        {
            return (isNight ? config.HostileNightMultiplier : config.HostileDayMultiplier) *
                   (isNight ? definition.NightSpawnMultiplier : definition.DaySpawnMultiplier);
        }

        return (isNight ? config.PassiveNightMultiplier : config.PassiveDayMultiplier) *
               (isNight ? definition.NightSpawnMultiplier : definition.DaySpawnMultiplier);
    }

    private static void Register(MobDefinition definition)
    {
        Definitions[definition.Type] = definition;
    }
}
