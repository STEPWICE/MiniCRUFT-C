using System;
using MiniCRUFT.Core;

namespace MiniCRUFT.World;

public sealed class WorldHeightSampler
{
    private readonly NoiseService _noise;
    private readonly WorldGenSettings _settings;
    private readonly BiomeId? _forcedBiome;

    public WorldHeightSampler(int seed, WorldGenSettings settings)
    {
        _noise = new NoiseService(seed);
        _settings = settings;
        _forcedBiome = settings.ForcedBiome;
    }

    public float GetRiverValue(int worldX, int worldZ)
    {
        return MathF.Abs(_noise.River(worldX, worldZ));
    }

    public BiomeId GetBiomeId(int worldX, int worldZ, float riverValue)
    {
        if (_forcedBiome.HasValue)
        {
            return _forcedBiome.Value;
        }

        float temp = (_noise.Biome(worldX, worldZ) + 1f) * 0.5f;
        float humid = (_noise.Erosion(worldX + 1000, worldZ - 1000) + 1f) * 0.5f;

        var biome = BiomeRegistry.Pick(temp, humid);
        if (riverValue < _settings.RiverThreshold)
        {
            biome = BiomeId.River;
        }

        return biome;
    }

    public int GetBaseHeight(int worldX, int worldZ, out float ridge)
    {
        float continental = (_noise.Continental(worldX, worldZ) + 1f) * 0.5f;
        float peaks = (_noise.Peaks(worldX, worldZ) + 1f) * 0.5f;
        float erosion = (_noise.Erosion(worldX, worldZ) + 1f) * 0.5f;
        ridge = 1f - MathF.Abs(peaks * 2f - 1f);
        ridge = MathF.Min(ridge, _settings.RidgeClamp);

        float height = _settings.BaseHeight +
                       continental * _settings.ContinentalAmplitude +
                       peaks * _settings.PeakAmplitude +
                       erosion * _settings.ErosionAmplitude +
                       ridge * _settings.RidgeAmplitude;

        return (int)height;
    }

    public int GetSmoothedHeight(int worldX, int worldZ)
    {
        float ridge;
        int height = GetBaseHeight(worldX, worldZ, out ridge);
        if (_settings.CliffSmoothStrength <= 0f)
        {
            return height;
        }

        int heightX = GetBaseHeight(worldX + 1, worldZ, out _);
        int heightZ = GetBaseHeight(worldX, worldZ + 1, out _);
        float slope = MathF.Max(MathF.Abs(height - heightX), MathF.Abs(height - heightZ));

        if (slope > _settings.CliffSlopeThreshold)
        {
            float avg = (height + heightX + heightZ) / 3f;
            height = (int)MathF.Round(MathUtil.Lerp(height, avg, _settings.CliffSmoothStrength));
        }

        return height;
    }
}
