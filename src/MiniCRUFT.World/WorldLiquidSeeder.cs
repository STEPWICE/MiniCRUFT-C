using MiniCRUFT.Core;

namespace MiniCRUFT.World;

public sealed class WorldLiquidSeeder
{
    private readonly NoiseService _noise;
    private readonly WorldGenSettings _settings;

    public WorldLiquidSeeder(int seed, WorldGenSettings settings)
    {
        _noise = new NoiseService(SeedNoise(seed));
        _settings = settings;
    }

    public void SeedColumn(Chunk chunk, int localX, int localZ, int worldX, int worldZ, int surfaceHeight)
    {
        if (!_settings.GenerateAquifers || _settings.StrictBetaMode)
        {
            return;
        }

        int topY = Math.Min(surfaceHeight - 2, Chunk.SizeY - 2);
        if (topY < 5)
        {
            return;
        }

        int waterCeiling = Math.Min(_settings.SeaLevel - _settings.AquiferWaterLevelOffset, topY);
        int lavaCeiling = Math.Min(_settings.SeaLevel - _settings.AquiferLavaLevelOffset, waterCeiling - 1);
        if (waterCeiling < 5)
        {
            return;
        }

        for (int y = 5; y <= topY; y++)
        {
            if (chunk.GetBlock(localX, y, localZ) != BlockId.Air)
            {
                continue;
            }

            if (chunk.GetBlock(localX, y - 1, localZ) == BlockId.Air)
            {
                continue;
            }

            if (y <= lavaCeiling && SamplePocket(worldX, y, worldZ, _settings.AquiferLavaScale) >= _settings.AquiferLavaThreshold)
            {
                chunk.SetBlock(localX, y, localZ, LiquidBlocks.GetSourceId(LiquidKind.Lava));
                continue;
            }

            if (y <= waterCeiling && SamplePocket(worldX, y, worldZ, _settings.AquiferWaterScale) >= _settings.AquiferWaterThreshold)
            {
                chunk.SetBlock(localX, y, localZ, LiquidBlocks.GetSourceId(LiquidKind.Water));
            }
        }
    }

    private float SamplePocket(int worldX, int worldY, int worldZ, float scale)
    {
        float s = scale <= 0f ? 1f : scale;
        float sample = _noise.Cave(worldX * s, worldY * s, worldZ * s);
        return (sample + 1f) * 0.5f;
    }

    private static int SeedNoise(int seed)
    {
        unchecked
        {
            return seed ^ 0x6C1F3A57;
        }
    }
}
