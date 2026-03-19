using System;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public static class MobSpawnRules
{
    public static float GetWeight(
        MobDefinition definition,
        MobConfig config,
        BiomeId biome,
        float sunIntensity,
        float rainIntensity,
        bool submerged,
        bool skyExposed)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(config);

        if (!definition.CanSpawnInBiome(biome))
        {
            return 0f;
        }

        if (submerged && !definition.CanSpawnInWater)
        {
            return 0f;
        }

        float weight = definition.SpawnWeight * MobCatalog.GetConfiguredWeight(definition.Type, config);
        if (weight <= 0f)
        {
            return 0f;
        }

        float nightFactor = 1f - Math.Clamp(sunIntensity, 0f, 1f);
        float dayFactor = 1f - nightFactor;
        float populationFactor = definition.Hostile
            ? (config.HostileDayMultiplier * dayFactor) + (config.HostileNightMultiplier * nightFactor)
            : (config.PassiveDayMultiplier * dayFactor) + (config.PassiveNightMultiplier * nightFactor);

        populationFactor *= (definition.DaySpawnMultiplier * dayFactor) + (definition.NightSpawnMultiplier * nightFactor);

        float rainFactor = Math.Clamp(rainIntensity, 0f, 1f);
        float weatherMultiplier = definition.Hostile
            ? 1f + (Math.Max(0f, config.RainHostileMultiplier) - 1f) * rainFactor
            : 1f + (Math.Max(0f, config.RainPassiveMultiplier) - 1f) * rainFactor;

        float environmentMultiplier = definition.Hostile
            ? (skyExposed ? Math.Max(0f, config.HostileSkyExposureMultiplier) : Math.Max(0f, config.HostileShelterMultiplier))
            : (skyExposed ? Math.Max(0f, config.PassiveSkyExposureMultiplier) : Math.Max(0f, config.PassiveShelterMultiplier));

        return weight * populationFactor * Math.Max(0f, weatherMultiplier) * Math.Max(0f, environmentMultiplier);
    }
}
