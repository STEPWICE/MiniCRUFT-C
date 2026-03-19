using System.Linq;
using MiniCRUFT.Core;
using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class WorldGenerationTests
{
    [Fact]
    public void GenerateChunk_IsDeterministic_ForSameSeed()
    {
        BlockRegistry.Initialize();
        BiomeRegistry.Initialize();

        var settings = new WorldGenSettings
        {
            ForcedBiome = BiomeId.Plains
        };

        var firstChunk = new Chunk(0, 0);
        var secondChunk = new Chunk(0, 0);

        new WorldGenerator(1337, settings).GenerateChunk(firstChunk);
        new WorldGenerator(1337, settings).GenerateChunk(secondChunk);

        Assert.Equal(BlockId.Bedrock, firstChunk.GetBlock(0, 0, 0));
        Assert.True(firstChunk.Blocks.SequenceEqual(secondChunk.Blocks));
        Assert.True(firstChunk.Biomes.SequenceEqual(secondChunk.Biomes));
        Assert.True(firstChunk.SkyLight.SequenceEqual(secondChunk.SkyLight));
        Assert.True(firstChunk.TorchLight.SequenceEqual(secondChunk.TorchLight));
    }

    [Fact]
    public void GenerateChunk_StrictBetaFillsWaterBelowSeaLevel()
    {
        BlockRegistry.Initialize();
        BiomeRegistry.Initialize();

        var settings = new WorldGenSettings
        {
            StrictBetaMode = true,
            ForcedBiome = BiomeId.Plains,
            BaseHeight = 4,
            SeaLevel = 10,
            ContinentalAmplitude = 0f,
            PeakAmplitude = 0f,
            ErosionAmplitude = 0f,
            RidgeAmplitude = 0f,
            DetailScale = 0f,
            BiomeScale = 0f,
            BiomeWarpStrength = 0f,
            DesertDuneAmplitude = 0f,
            TreeClusterStrength = 0f,
            RiverThreshold = 1f,
            RiverWidth = 1f,
            RiverDepth = 0,
            RiverWarpStrength = 0f,
            WaterSlopeThreshold = 100f,
            WaterMinDepth = 0,
            CliffSmoothStrength = 0f
        };

        var chunk = new Chunk(0, 0);

        new WorldGenerator(1337, settings).GenerateChunk(chunk);

        Assert.Contains(chunk.Blocks, block => WaterBlocks.IsWater(block));
        Assert.Contains(chunk.Blocks, block => block == BlockId.Sand);
    }

    [Fact]
    public void GenerateChunk_SeedsLavaAquifers_WhenEnabled()
    {
        BlockRegistry.Initialize();
        BiomeRegistry.Initialize();

        var settings = new WorldGenSettings
        {
            ForcedBiome = BiomeId.Plains,
            BaseHeight = 32,
            SeaLevel = 48,
            ContinentalAmplitude = 0f,
            PeakAmplitude = 0f,
            ErosionAmplitude = 0f,
            RidgeAmplitude = 0f,
            DetailScale = 0f,
            BiomeScale = 0f,
            BiomeWarpStrength = 0f,
            DesertDuneAmplitude = 0f,
            TreeClusterStrength = 0f,
            RiverThreshold = 1f,
            RiverWidth = 1f,
            RiverDepth = 0,
            RiverWarpStrength = 0f,
            WaterSlopeThreshold = 100f,
            WaterMinDepth = 0,
            CliffSmoothStrength = 0f,
            CaveThreshold = -1f,
            GenerateAquifers = true,
            AquiferWaterLevelOffset = 0,
            AquiferWaterScale = 0.1f,
            AquiferWaterThreshold = 0f,
            AquiferLavaLevelOffset = 0,
            AquiferLavaScale = 0.1f,
            AquiferLavaThreshold = 0f
        };

        var chunk = new Chunk(0, 0);

        new WorldGenerator(1337, settings).GenerateChunk(chunk);

        Assert.Contains(chunk.Blocks, block => LiquidBlocks.IsLava(block));
    }
}
