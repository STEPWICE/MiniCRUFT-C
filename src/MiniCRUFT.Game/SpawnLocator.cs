using System;
using System.Collections.Generic;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public static class SpawnLocator
{
    public static bool TryFindSpawn(int seed, WorldGenSettings settings, SpawnConfig config, out Vector3 spawn, out BiomeId biome)
    {
        spawn = Vector3.Zero;
        biome = BiomeId.Plains;

        int radius = Math.Max(64, config.SearchRadius);
        int attempts = Math.Max(1, config.MaxAttempts);
        float maxSlope = config.MaxSlope > 0f ? config.MaxSlope : settings.CliffSlopeThreshold * 0.7f;
        int minHeight = Math.Max(1, config.MinHeightAboveSea);

        var excluded = BuildExcludedBiomes(config.ExcludedBiomes, out var invalidBiomeNames);
        if (invalidBiomeNames.Count > 0)
        {
            Log.Warn($"Ignored unsupported spawn excluded biomes: {string.Join(", ", invalidBiomeNames)}");
        }

        if (!string.IsNullOrWhiteSpace(config.Mode) &&
            !config.Mode.Equals("AnyNonRiver", StringComparison.OrdinalIgnoreCase))
        {
            Log.Warn($"Unsupported spawn mode '{config.Mode}' in config; current code only honors AnyNonRiver.");
        }

        var random = config.Randomize
            ? new Random(unchecked(Environment.TickCount ^ seed))
            : new Random(seed ^ 0x5F3759DF);
        var sampler = new WorldHeightSampler(seed, settings);
        var generator = new WorldGenerator(seed, settings);

        for (int i = 0; i < attempts; i++)
        {
            int x = random.Next(-radius, radius + 1);
            int z = random.Next(-radius, radius + 1);

            float riverVal = sampler.GetRiverValue(x, z);
            var candidateBiome = sampler.GetBiomeId(x, z, riverVal);
            if (string.IsNullOrWhiteSpace(config.Mode) ||
                config.Mode.Equals("AnyNonRiver", StringComparison.OrdinalIgnoreCase))
            {
                if (candidateBiome == BiomeId.River)
                {
                    continue;
                }
            }

            if (excluded.Contains(candidateBiome))
            {
                continue;
            }

            int height = generator.EstimateSurfaceHeight(x, z);
            int heightX = generator.EstimateSurfaceHeight(x + 1, z);
            int heightZ = generator.EstimateSurfaceHeight(x, z + 1);
            float slope = MathF.Max(MathF.Abs(height - heightX), MathF.Abs(height - heightZ));

            if (height < settings.SeaLevel + minHeight)
            {
                continue;
            }

            if (slope > maxSlope)
            {
                continue;
            }

            float safeY = Math.Max(height + 3f, settings.SeaLevel + minHeight + 2f);
            spawn = new Vector3(x + 0.5f, safeY, z + 0.5f);
            biome = candidateBiome;
            return true;
        }

        return false;
    }

    public static void LogBiomeSample(int seed, WorldGenSettings settings, Vector3 center, int radius)
    {
        var sampler = new WorldHeightSampler(seed, settings);
        var counts = new Dictionary<BiomeId, int>();
        int startX = MathUtil.FloorToInt(center.X) - radius;
        int startZ = MathUtil.FloorToInt(center.Z) - radius;
        int size = radius * 2 + 1;

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                int wx = startX + x;
                int wz = startZ + z;
                float riverVal = sampler.GetRiverValue(wx, wz);
                var biome = sampler.GetBiomeId(wx, wz, riverVal);
                counts.TryGetValue(biome, out var current);
                counts[biome] = current + 1;
            }
        }

        var parts = new List<string>();
        foreach (var pair in counts)
        {
            parts.Add($"{pair.Key}:{pair.Value}");
        }

        Log.Info($"Biome sample ({size}x{size}) around spawn: {string.Join(", ", parts)}");
    }

    private static HashSet<BiomeId> BuildExcludedBiomes(List<string>? names, out List<string> invalidNames)
    {
        var result = new HashSet<BiomeId>();
        invalidNames = new List<string>();
        if (names == null)
        {
            return result;
        }
        foreach (var name in names)
        {
            if (Enum.TryParse<BiomeId>(name, true, out var biome))
            {
                result.Add(biome);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                invalidNames.Add(name);
            }
        }
        return result;
    }
}
