using System;
using System.Collections.Generic;
using System.Numerics;
using MiniCRUFT.Core;

namespace MiniCRUFT.Game;

internal static class EliteMobVariantCatalog
{
    private static readonly EliteMobVariantProfile[] Profiles =
    [
        new(
            EliteMobVariant.None,
            "Elite",
            "Baseline elite threat.",
            HealthMultiplier: 1f,
            DamageMultiplier: 1f,
            SpeedMultiplier: 1f,
            PursuitMultiplier: 1f,
            KnockbackScale: 1f,
            RageSeconds: 2.2f,
            RageSpeedMultiplier: 1.12f,
            RagePursuitMultiplier: 1.14f,
            RageDamageMultiplier: 1.06f,
            LootMultiplier: 1f,
            TrophyCount: 1,
            SpawnWeight: 0f,
            Tint: new Vector4(1.18f, 1.08f, 0.86f, 1f)),
        new(
            EliteMobVariant.Brute,
            "Brute",
            "Slow, heavy, and difficult to stagger.",
            HealthMultiplier: 1.42f,
            DamageMultiplier: 1.12f,
            SpeedMultiplier: 0.88f,
            PursuitMultiplier: 1.08f,
            KnockbackScale: 0.55f,
            RageSeconds: 2.8f,
            RageSpeedMultiplier: 1.08f,
            RagePursuitMultiplier: 1.12f,
            RageDamageMultiplier: 1.04f,
            LootMultiplier: 1.2f,
            TrophyCount: 2,
            SpawnWeight: 0.45f,
            Tint: new Vector4(1.26f, 0.95f, 0.74f, 1f)),
        new(
            EliteMobVariant.Hunter,
            "Hunter",
            "Fast pursuit and punishing retaliation.",
            HealthMultiplier: 1.08f,
            DamageMultiplier: 1.22f,
            SpeedMultiplier: 1.24f,
            PursuitMultiplier: 1.45f,
            KnockbackScale: 0.9f,
            RageSeconds: 1.8f,
            RageSpeedMultiplier: 1.24f,
            RagePursuitMultiplier: 1.34f,
            RageDamageMultiplier: 1.08f,
            LootMultiplier: 1.35f,
            TrophyCount: 2,
            SpawnWeight: 0.35f,
            Tint: new Vector4(0.82f, 1.08f, 1.18f, 1f)),
        new(
            EliteMobVariant.Warden,
            "Warden",
            "Rare apex threat with the best rewards.",
            HealthMultiplier: 1.68f,
            DamageMultiplier: 1.34f,
            SpeedMultiplier: 1f,
            PursuitMultiplier: 1.82f,
            KnockbackScale: 0.4f,
            RageSeconds: 3.2f,
            RageSpeedMultiplier: 1.14f,
            RagePursuitMultiplier: 1.5f,
            RageDamageMultiplier: 1.12f,
            LootMultiplier: 1.8f,
            TrophyCount: 3,
            SpawnWeight: 0.2f,
            Tint: new Vector4(0.92f, 1.12f, 0.88f, 1f))
    ];

    public static EliteMobVariantProfile Get(EliteMobVariant variant)
    {
        int index = NormalizeIndex(variant);
        return Profiles[index];
    }

    public static EliteMobVariant Normalize(EliteMobVariant variant)
    {
        return NormalizeIndex(variant) switch
        {
            0 => EliteMobVariant.None,
            1 => EliteMobVariant.Brute,
            2 => EliteMobVariant.Hunter,
            3 => EliteMobVariant.Warden,
            _ => EliteMobVariant.None
        };
    }

    public static EliteMobVariant SelectSpecialVariant(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        float totalWeight = 0f;
        for (int i = 1; i < Profiles.Length; i++)
        {
            totalWeight += Profiles[i].SpawnWeight;
        }

        if (totalWeight <= 0f)
        {
            return EliteMobVariant.None;
        }

        float roll = (float)random.NextDouble() * totalWeight;
        for (int i = 1; i < Profiles.Length; i++)
        {
            roll -= Profiles[i].SpawnWeight;
            if (roll <= 0f)
            {
                return Profiles[i].Variant;
            }
        }

        return Profiles[Profiles.Length - 1].Variant;
    }

    private static int NormalizeIndex(EliteMobVariant variant)
    {
        return variant switch
        {
            EliteMobVariant.None => 0,
            EliteMobVariant.Brute => 1,
            EliteMobVariant.Hunter => 2,
            EliteMobVariant.Warden => 3,
            _ => 0
        };
    }
}
