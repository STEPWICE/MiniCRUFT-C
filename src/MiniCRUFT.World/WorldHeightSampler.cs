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
        float scale = _settings.RiverScale <= 0f ? 1f : _settings.RiverScale;
        float warp = _settings.RiverWarpStrength;
        float warpX = 0f;
        float warpZ = 0f;
        if (warp > 0f)
        {
            warpX = _noise.Erosion(worldX + 12000f, worldZ - 8000f) * warp;
            warpZ = _noise.Continental(worldX - 6000f, worldZ + 14000f) * warp;
        }

        return MathF.Abs(_noise.River(worldX + warpX, worldZ + warpZ, scale));
    }

    public BiomeId GetBiomeId(int worldX, int worldZ, float riverValue)
    {
        if (_settings.StrictBetaMode)
        {
            return GetStrictBetaBiomeId(worldX, worldZ);
        }

        if (_forcedBiome.HasValue)
        {
            return _forcedBiome.Value;
        }

        float riverWidth = _settings.RiverThreshold * _settings.RiverWidth;
        if (riverValue < riverWidth)
        {
            return BiomeId.River;
        }

        float mountainMask = (_noise.Peaks(worldX, worldZ) + 1f) * 0.5f;
        if (mountainMask > _settings.MountainBiomeThreshold)
        {
            return BiomeId.Mountains;
        }

        float scale = _settings.BiomeScale <= 0f ? 1f : _settings.BiomeScale;
        float warp = _settings.BiomeWarpStrength;
        float warpX = 0f;
        float warpZ = 0f;
        if (warp > 0f)
        {
            warpX = _noise.Continental(worldX, worldZ) * warp;
            warpZ = _noise.Erosion(worldX - 5000, worldZ + 5000) * warp;
        }

        float sampleX = (worldX + warpX) * scale;
        float sampleZ = (worldZ + warpZ) * scale;

        float temp = (_noise.Biome(sampleX, sampleZ) + 1f) * 0.5f;
        float humid = (_noise.Biome(sampleX + 1000f, sampleZ - 1000f) + 1f) * 0.5f;

        float contrast = _settings.BiomeContrast;
        if (MathF.Abs(contrast - 1f) > 0.001f)
        {
            temp = ApplyContrast(temp, contrast);
            humid = ApplyContrast(humid, contrast);
        }

        var biome = BiomeRegistry.Pick(temp, humid);
        return biome;
    }

    private BiomeId GetStrictBetaBiomeId(int worldX, int worldZ)
    {
        float scale = _settings.BiomeScale <= 0f ? 1f : _settings.BiomeScale;
        float warp = _settings.BiomeWarpStrength;
        float warpX = 0f;
        float warpZ = 0f;
        if (warp > 0f)
        {
            warpX = _noise.Continental(worldX, worldZ) * warp;
            warpZ = _noise.Erosion(worldX - 5000, worldZ + 5000) * warp;
        }

        float sampleX = (worldX + warpX) * scale;
        float sampleZ = (worldZ + warpZ) * scale;

        float temp = (_noise.Biome(sampleX, sampleZ) + 1f) * 0.5f;
        float humid = (_noise.Biome(sampleX + 1000f, sampleZ - 1000f) + 1f) * 0.5f;

        float contrast = _settings.BiomeContrast;
        if (MathF.Abs(contrast - 1f) > 0.001f)
        {
            temp = ApplyContrast(temp, contrast);
            humid = ApplyContrast(humid, contrast);
        }

        return BiomeRegistry.PickStrictBeta(temp, humid);
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
        int height = GetBaseHeight(worldX, worldZ, out _);
        if (_settings.CliffSmoothStrength <= 0f)
        {
            return height;
        }

        int heightX1 = GetBaseHeight(worldX + 1, worldZ, out _);
        int heightX0 = GetBaseHeight(worldX - 1, worldZ, out _);
        int heightZ1 = GetBaseHeight(worldX, worldZ + 1, out _);
        int heightZ0 = GetBaseHeight(worldX, worldZ - 1, out _);
        float slope = MathF.Max(
            MathF.Max(MathF.Abs(height - heightX1), MathF.Abs(height - heightX0)),
            MathF.Max(MathF.Abs(height - heightZ1), MathF.Abs(height - heightZ0)));

        if (slope > _settings.CliffSlopeThreshold)
        {
            float avg = (height + heightX1 + heightX0 + heightZ1 + heightZ0) / 5f;
            float t = MathUtil.Clamp((slope - _settings.CliffSlopeThreshold) / _settings.CliffSlopeThreshold, 0f, 1f);
            height = (int)MathF.Round(MathUtil.Lerp(height, avg, _settings.CliffSmoothStrength * t));
        }

        return height;
    }

    private static float ApplyContrast(float value, float contrast)
    {
        float centered = value - 0.5f;
        float adjusted = 0.5f + centered * contrast;
        return MathUtil.Clamp(adjusted, 0f, 1f);
    }
}
