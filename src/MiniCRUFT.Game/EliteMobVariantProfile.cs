using System.Numerics;
using MiniCRUFT.Core;

namespace MiniCRUFT.Game;

internal readonly record struct EliteMobVariantProfile(
    EliteMobVariant Variant,
    string Name,
    string Description,
    float HealthMultiplier,
    float DamageMultiplier,
    float SpeedMultiplier,
    float PursuitMultiplier,
    float KnockbackScale,
    float RageSeconds,
    float RageSpeedMultiplier,
    float RagePursuitMultiplier,
    float RageDamageMultiplier,
    float LootMultiplier,
    int TrophyCount,
    float SpawnWeight,
    Vector4 Tint);
