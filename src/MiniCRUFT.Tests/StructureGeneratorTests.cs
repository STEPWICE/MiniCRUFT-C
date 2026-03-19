using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class StructureGeneratorTests
{
    [Fact]
    public void GenerateChunk_IsDeterministic_ForSameSeed()
    {
        var settings = CreateStructureFriendlySettings();
        var generatorA = new WorldGenerator(12345, settings);
        var generatorB = new WorldGenerator(12345, settings);

        var chunkA = new Chunk(0, 0);
        var chunkB = new Chunk(0, 0);

        generatorA.GenerateChunk(chunkA);
        generatorB.GenerateChunk(chunkB);

        Assert.Equal(chunkA.Blocks, chunkB.Blocks);
        Assert.Equal(chunkA.Biomes, chunkB.Biomes);
    }

    [Fact]
    public void GenerateChunk_PlacesCampPoi_WhenEnabled()
    {
        var settings = CreateStructureFriendlySettings();
        settings.StructureChance = 1f;
        settings.CampChance = 1f;
        settings.WatchtowerChance = 0f;
        settings.BuriedCacheChance = 0f;
        settings.CaveCacheChance = 0f;
        settings.RuinChance = 0f;
        settings.MineShaftChance = 0f;

        var generator = new WorldGenerator(4242, settings);
        var chunk = new Chunk(0, 0);

        generator.GenerateChunk(chunk);

        Assert.Contains(chunk.Blocks, block => block == BlockId.Chest);
        Assert.Contains(chunk.Blocks, block => block == BlockId.CraftingTable);
        Assert.Contains(chunk.Blocks, block => block == BlockId.Furnace);
    }

    [Fact]
    public void GenerateChunk_PlacesRuinPoi_WhenEnabled()
    {
        var settings = CreateStructureFriendlySettings();
        settings.StructureChance = 1f;
        settings.CampChance = 0f;
        settings.WatchtowerChance = 0f;
        settings.BuriedCacheChance = 0f;
        settings.CaveCacheChance = 0f;
        settings.RuinChance = 1f;
        settings.MineShaftChance = 0f;

        var generator = new StructureGenerator(9898, settings);
        var chunk = CreateRuinChunk();
        int[] surfaceHeights = new int[Chunk.SizeX * Chunk.SizeZ];
        for (int i = 0; i < surfaceHeights.Length; i++)
        {
            surfaceHeights[i] = 80;
        }

        generator.Apply(chunk, surfaceHeights);

        Assert.Contains(chunk.Blocks, block => block == BlockId.Chest);
        Assert.Contains(chunk.Blocks, block => block == BlockId.Cobblestone);
        Assert.Contains(chunk.Blocks, block => block == BlockId.Gravel);
    }

    [Fact]
    public void GenerateChunk_PlacesMineShaftPoi_WhenEnabled()
    {
        var settings = CreateStructureFriendlySettings();
        settings.StructureChance = 1f;
        settings.CampChance = 0f;
        settings.WatchtowerChance = 0f;
        settings.BuriedCacheChance = 0f;
        settings.CaveCacheChance = 0f;
        settings.RuinChance = 0f;
        settings.MineShaftChance = 1f;

        var generator = new StructureGenerator(7171, settings);
        var chunk = CreateMineShaftChunk();
        int[] surfaceHeights = new int[Chunk.SizeX * Chunk.SizeZ];
        for (int i = 0; i < surfaceHeights.Length; i++)
        {
            surfaceHeights[i] = 80;
        }

        generator.Apply(chunk, surfaceHeights);

        Assert.Contains(chunk.Blocks, block => block == BlockId.Chest);
        Assert.Contains(chunk.Blocks, block => block == BlockId.Wood);
        Assert.Contains(chunk.Blocks, block => block == BlockId.Planks);
    }

    private static WorldGenSettings CreateStructureFriendlySettings()
    {
        return new WorldGenSettings
        {
            StrictBetaMode = false,
            BaseHeight = 80,
            SeaLevel = 62,
            ContinentalAmplitude = 0f,
            PeakAmplitude = 0f,
            ErosionAmplitude = 0f,
            RidgeAmplitude = 0f,
            DetailScale = 0f,
            DesertDuneScale = 0f,
            RiverScale = 0f,
            RiverWarpStrength = 0f,
            BiomeScale = 0f,
            BiomeWarpStrength = 0f,
            BiomeContrast = 1f,
            MountainBiomeThreshold = 1f,
            CliffSlopeThreshold = 20f,
            CliffSmoothStrength = 0f,
            TreeChance = 0f,
            ForestTreeChance = 0f,
            PlainsTreeChance = 0f,
            TaigaTreeChance = 0f,
            TundraTreeChance = 0f,
            SwampTreeChance = 0f,
            SavannaTreeChance = 0f,
            ShrublandTreeChance = 0f,
            MountainsTreeChance = 0f,
            ForestTallGrassChance = 0f,
            PlainsTallGrassChance = 0f,
            TaigaTallGrassChance = 0f,
            TundraTallGrassChance = 0f,
            SavannaTallGrassChance = 0f,
            ShrublandTallGrassChance = 0f,
            ForestFlowerChance = 0f,
            SwampFlowerChance = 0f,
            DesertCactusChance = 0f,
            DesertDeadBushChance = 0f,
            SugarCaneChance = 0f,
            PondChance = 0f,
            BoulderChance = 0f,
            FallenLogChance = 0f,
            FlowerPatchChance = 0f,
            GravelPatchChance = 0f,
            GenerateAquifers = false,
            StructureChance = 1f,
            CampChance = 1f,
            WatchtowerChance = 1f,
            BuriedCacheChance = 1f,
            CaveCacheChance = 1f,
            RuinChance = 1f,
            MineShaftChance = 1f,
            StructureMargin = 4,
            ForcedBiome = BiomeId.Plains,
            TreeMaxSlope = 20f,
            WaterMinDepth = 0
        };
    }

    private static Chunk CreateMineShaftChunk()
    {
        var chunk = new Chunk(0, 0);
        for (int x = 0; x < Chunk.SizeX; x++)
        {
            for (int z = 0; z < Chunk.SizeZ; z++)
            {
                chunk.SetBiome(x, z, BiomeId.Forest);
                for (int y = 0; y <= 19; y++)
                {
                    chunk.SetBlock(x, y, z, BlockId.Stone);
                }
            }
        }

        return chunk;
    }

    private static Chunk CreateRuinChunk()
    {
        var chunk = new Chunk(0, 0);
        for (int x = 0; x < Chunk.SizeX; x++)
        {
            for (int z = 0; z < Chunk.SizeZ; z++)
            {
                chunk.SetBiome(x, z, BiomeId.Plains);
            }
        }

        return chunk;
    }
}
