using System;
using MiniCRUFT.Core;

namespace MiniCRUFT.World;

public sealed class WorldGenerator
{
    private readonly NoiseService _noise;
    private readonly WorldGenSettings _settings;
    private readonly int _seed;
    private readonly WorldHeightSampler _heightSampler;
    private readonly SurfaceFeatureGenerator _features;
    private readonly StructureGenerator _structures;
    private readonly WorldLiquidSeeder _liquidSeeder;

    public WorldGenerator(int seed, WorldGenSettings settings)
    {
        _seed = seed;
        _settings = settings;
        _noise = new NoiseService(seed);
        _heightSampler = new WorldHeightSampler(seed, settings);
        _features = new SurfaceFeatureGenerator(settings);
        _structures = new StructureGenerator(seed, settings);
        _liquidSeeder = new WorldLiquidSeeder(seed, settings);
    }

    public void GenerateChunk(Chunk chunk)
    {
        var rand = new Random(HashSeed(_seed, chunk.ChunkX, chunk.ChunkZ));
        int[] surfaceHeights = new int[Chunk.SizeX * Chunk.SizeZ];
        float riverWidth = _settings.RiverThreshold * _settings.RiverWidth;
        float trailScale = _settings.TrailScale;
        float detailScale = _settings.DetailScale;
        float duneScale = _settings.DesertDuneScale;
        float clusterScale = _settings.TreeClusterScale;

        for (int x = 0; x < Chunk.SizeX; x++)
        {
            for (int z = 0; z < Chunk.SizeZ; z++)
            {
                int worldX = chunk.ChunkX * Chunk.SizeX + x;
                int worldZ = chunk.ChunkZ * Chunk.SizeZ + z;

                var sample = SampleHeight(worldX, worldZ, riverWidth, detailScale, duneScale);
                var sampleX = SampleHeight(worldX + 1, worldZ, riverWidth, detailScale, duneScale);
                var sampleZ = SampleHeight(worldX, worldZ + 1, riverWidth, detailScale, duneScale);
                var sampleXZ = SampleHeight(worldX + 1, worldZ + 1, riverWidth, detailScale, duneScale);

                int height = Math.Clamp(sample.Height, 1, Chunk.SizeY - 2);
                int heightX = Math.Clamp(sampleX.Height, 1, Chunk.SizeY - 2);
                int heightZ = Math.Clamp(sampleZ.Height, 1, Chunk.SizeY - 2);
                int heightXZ = Math.Clamp(sampleXZ.Height, 1, Chunk.SizeY - 2);
                var biome = sample.Biome;
                bool isRiver = sample.RiverValue < riverWidth;
                bool nearRiver = sample.RiverInfluence > _settings.RiverBankInfluenceMin;
                bool riverWater = sample.RiverInfluence > _settings.RiverWaterInfluenceMin;

                float slope = MathF.Max(MathF.Abs(height - heightX), MathF.Abs(height - heightZ));
                height = ApplyCliffSmoothing(height, heightX, heightZ, heightXZ, slope);
                height = Math.Clamp(height, 1, Chunk.SizeY - 2);
                slope = MathF.Max(MathF.Abs(height - heightX), MathF.Abs(height - heightZ));

                chunk.SetBiome(x, z, biome);
                surfaceHeights[Chunk.GetBiomeIndex(x, z)] = height;

                bool cliff = slope > _settings.CliffSlopeThreshold || sample.Ridge > _settings.RidgeClamp;
                BlockId surface = BiomeRegistry.Get(biome).SurfaceBlock;
                if (_settings.StrictBetaMode)
                {
                    if (height >= _settings.SnowLine)
                    {
                        surface = BlockId.Snow;
                    }
                    else if (biome == BiomeId.Tundra)
                    {
                        surface = BlockId.Snow;
                    }
                    else if (biome == BiomeId.Taiga && height >= _settings.SnowLine - 4)
                    {
                        surface = BlockId.Snow;
                    }
                    else if (cliff || slope >= _settings.MountainStoneSlope || height >= _settings.SeaLevel + _settings.MountainStoneHeightOffset)
                    {
                        surface = BlockId.Stone;
                    }
                }
                else if (biome == BiomeId.Mountains)
                {
                    if (height >= _settings.SnowLine)
                    {
                        surface = BlockId.Snow;
                    }
                    else
                    {
                        bool stoneCap = height >= _settings.SeaLevel + _settings.MountainStoneHeightOffset ||
                                        slope >= _settings.MountainStoneSlope;
                        surface = stoneCap ? BlockId.Stone : BlockId.Grass;
                    }
                }
                else if (biome == BiomeId.Tundra)
                {
                    surface = BlockId.Snow;
                }
                else if (biome == BiomeId.Taiga && height >= _settings.SnowLine - 4)
                {
                    surface = BlockId.Snow;
                }

                if (nearRiver && !cliff)
                {
                    float bankNoise = (_noise.Detail(worldX * 0.05f + 11f, worldZ * 0.05f - 7f) + 1f) * 0.5f;
                    if (height <= _settings.SeaLevel - 1)
                    {
                        surface = bankNoise < 0.35f ? BlockId.Gravel : bankNoise < 0.7f ? BlockId.Sand : BlockId.Clay;
                    }
                    else if (height <= _settings.SeaLevel + 1)
                    {
                        surface = BlockId.Sand;
                    }
                }

                int beachSize = _settings.StrictBetaMode ? Math.Min(_settings.BeachSize, 3) : _settings.BeachSize;
                if (height <= _settings.SeaLevel + beachSize && !cliff)
                {
                    surface = BlockId.Sand;
                }

                for (int y = 0; y <= height; y++)
                {
                    BlockId block;
                    if (y == 0)
                    {
                        block = BlockId.Bedrock;
                    }
                    else if (y == height)
                    {
                        block = cliff ? BlockId.Stone : surface;
                    }
                    else if (y > height - 4)
                    {
                        if (cliff || surface == BlockId.Stone)
                        {
                            block = BlockId.Stone;
                        }
                        else
                        {
                            block = surface == BlockId.Sand ? BlockId.Sand : BlockId.Dirt;
                        }
                    }
                    else
                    {
                        block = BlockId.Stone;
                    }

                    chunk.SetBlock(x, y, z, block);
                }

                if (ShouldFillWater(height, heightX, heightZ, heightXZ, slope, isRiver, riverWater))
                {
                    for (int y = height + 1; y <= _settings.SeaLevel; y++)
                    {
                        chunk.SetBlock(x, y, z, WaterBlocks.SourceId);
                    }
                }

                // Caves
                for (int y = 5; y < height - 2; y++)
                {
                    float cave = _noise.Cave(worldX, y, worldZ);
                    if (cave > _settings.CaveThreshold)
                    {
                        chunk.SetBlock(x, y, z, BlockId.Air);
                    }
                }

                // Ores
                if (height > 8)
                {
                    GenerateOre(chunk, rand, x, z, BlockId.CoalOre, 0, 128, 10);
                    GenerateOre(chunk, rand, x, z, BlockId.IronOre, 0, 64, 6);
                    GenerateOre(chunk, rand, x, z, BlockId.GoldOre, 0, 32, 4);
                    GenerateOre(chunk, rand, x, z, BlockId.DiamondOre, 0, 16, 2);
                }

                // Vegetation
                var groundBlock = chunk.GetBlock(x, height, z);
                bool groundSuitable = groundBlock == BlockId.Grass || groundBlock == BlockId.Dirt;
                bool snowyGround = groundBlock == BlockId.Snow;
                if (snowyGround && (biome == BiomeId.Taiga || biome == BiomeId.Tundra))
                {
                    groundSuitable = true;
                }
                bool heightAboveWater = height >= _settings.SeaLevel;
                bool slopeOk = slope <= _settings.TreeMaxSlope;
                bool isTrail = IsTrail(worldX, worldZ, trailScale);
                bool clearTrail = isTrail && rand.NextDouble() < _settings.TrailClearChance;

                if (isTrail && !cliff && groundBlock == BlockId.Grass && rand.NextDouble() < _settings.TrailDirtChance)
                {
                    chunk.SetBlock(x, height, z, BlockId.Dirt);
                    groundBlock = BlockId.Dirt;
                    groundSuitable = true;
                }

                float clusterNoise = (_noise.TreeCluster(worldX * clusterScale, worldZ * clusterScale) + 1f) * 0.5f;
                float clusterFactor = MathUtil.Lerp(1f - _settings.TreeClusterStrength, 1f + _settings.TreeClusterStrength, clusterNoise);

                float treeChance = GetTreeChance(biome) * clusterFactor;
                if (clearTrail)
                {
                    treeChance *= 0.15f;
                }

                if (treeChance > 0f && rand.NextDouble() < treeChance)
                {
                    int treeY = height + 1;
                    if (treeY < Chunk.SizeY - 6 && groundSuitable && heightAboveWater && slopeOk && chunk.GetBlock(x, treeY, z) == BlockId.Air)
                    {
                        if (biome == BiomeId.Taiga || biome == BiomeId.Mountains)
                        {
                            TreeGenerator.GenerateSpruce(chunk, x, treeY, z, rand);
                        }
                        else
                        {
                            bool largePlaced = false;
                            if (biome == BiomeId.Forest && rand.NextDouble() < _settings.LargeTreeChance)
                            {
                                largePlaced = TreeGenerator.GenerateLargeOak(chunk, x, treeY, z, rand, _settings.LargeTreeMinHeight, _settings.LargeTreeMaxHeight, _settings.LargeTreeLeafRadius, _settings.LargeTreeCanopyDepth);
                            }

                            if (!largePlaced)
                            {
                                double roll = rand.NextDouble();
                                if (roll < 0.45)
                                {
                                    TreeGenerator.GenerateBirch(chunk, x, treeY, z, rand);
                                }
                                else
                                {
                                    TreeGenerator.GenerateOak(chunk, x, treeY, z, rand);
                                }
                            }
                        }
                    }
                }

                if (!clearTrail && groundBlock == BlockId.Grass && heightAboveWater && chunk.GetBlock(x, height + 1, z) == BlockId.Air)
                {
                    float tallGrassChance = GetTallGrassChance(biome);
                    if (tallGrassChance > 0f && rand.NextDouble() < tallGrassChance)
                    {
                        chunk.SetBlock(x, height + 1, z, BlockId.TallGrass);
                    }

                    float flowerChance = GetFlowerChance(biome);
                    if (flowerChance > 0f && rand.NextDouble() < flowerChance)
                    {
                        chunk.SetBlock(x, height + 1, z, BlockId.Flower);
                    }
                }

                if (biome == BiomeId.Desert && heightAboveWater && groundBlock == BlockId.Sand)
                {
                    int plantY = height + 1;
                    if (plantY < Chunk.SizeY - 2 && chunk.GetBlock(x, plantY, z) == BlockId.Air)
                    {
                        if (rand.NextDouble() < _settings.DesertCactusChance && CanPlaceCactus(chunk, x, plantY, z))
                        {
                            PlaceColumn(chunk, x, plantY, z, BlockId.Cactus, rand.Next(2, 4));
                        }
                        else if (rand.NextDouble() < _settings.DesertDeadBushChance)
                        {
                            chunk.SetBlock(x, plantY, z, BlockId.DeadBush);
                        }
                    }
                }

                if (!clearTrail && (groundBlock == BlockId.Sand || groundBlock == BlockId.Dirt || groundBlock == BlockId.Grass))
                {
                    if (rand.NextDouble() < _settings.SugarCaneChance && IsAdjacentToWater(chunk, x, height, z) &&
                        chunk.GetBlock(x, height + 1, z) == BlockId.Air)
                    {
                        int caneHeight = rand.Next(2, 4);
                        PlaceColumn(chunk, x, height + 1, z, BlockId.SugarCane, caneHeight);
                    }
                }

                _liquidSeeder.SeedColumn(chunk, x, z, worldX, worldZ, height);
            }
        }

        _features.Apply(chunk, surfaceHeights, rand);
        _structures.Apply(chunk, surfaceHeights);
        WorldLighting.RecalculateChunkLighting(chunk);
        chunk.MarkDirty();
    }

    public int EstimateSurfaceHeight(int worldX, int worldZ)
    {
        float riverWidth = _settings.RiverThreshold * _settings.RiverWidth;
        var sample = SampleHeight(worldX, worldZ, riverWidth, _settings.DetailScale, _settings.DesertDuneScale);
        return Math.Clamp(sample.Height, 1, Chunk.SizeY - 2);
    }

    private int ApplyBiomeHeight(int baseHeight, BiomeId biome)
    {
        float scale = 1f;
        float bias = 0f;
        switch (biome)
        {
            case BiomeId.Forest:
                scale = _settings.ForestHeightScale;
                bias = _settings.ForestHeightBias;
                break;
            case BiomeId.Plains:
                scale = _settings.PlainsHeightScale;
                bias = _settings.PlainsHeightBias;
                break;
            case BiomeId.Desert:
                scale = _settings.DesertHeightScale;
                bias = _settings.DesertHeightBias;
                break;
            case BiomeId.Mountains:
                scale = _settings.MountainsHeightScale;
                bias = _settings.MountainsHeightBias;
                break;
            case BiomeId.Taiga:
                scale = _settings.TaigaHeightScale;
                bias = _settings.TaigaHeightBias;
                break;
            case BiomeId.Tundra:
                scale = _settings.TundraHeightScale;
                bias = _settings.TundraHeightBias;
                break;
            case BiomeId.Swamp:
                scale = _settings.SwampHeightScale;
                bias = _settings.SwampHeightBias;
                break;
            case BiomeId.Savanna:
                scale = _settings.SavannaHeightScale;
                bias = _settings.SavannaHeightBias;
                break;
            case BiomeId.Shrubland:
                scale = _settings.ShrublandHeightScale;
                bias = _settings.ShrublandHeightBias;
                break;
        }

        return (int)MathF.Round(baseHeight * scale + bias);
    }

    private bool ShouldFillWater(int height, int heightX, int heightZ, int heightXZ, float slope, bool isRiver, bool riverWater)
    {
        if (_settings.StrictBetaMode)
        {
            return height < _settings.SeaLevel;
        }

        int sea = _settings.SeaLevel;
        if (height >= sea)
        {
            return false;
        }

        int basinDepth = sea - height;
        int minimumFillDepth = Math.Max(3, _settings.WaterMinDepth + 2);
        if (!isRiver && !riverWater && basinDepth < minimumFillDepth)
        {
            return false;
        }

        if (slope > _settings.WaterSlopeThreshold)
        {
            return false;
        }

        int minDepth = Math.Max(0, _settings.WaterMinDepth);
        int below = 0;
        if (height <= sea - minDepth) below++;
        if (heightX <= sea - minDepth) below++;
        if (heightZ <= sea - minDepth) below++;
        if (heightXZ <= sea - minDepth) below++;

        if (below < 2)
        {
            return false;
        }

        int avg = (height + heightX + heightZ + heightXZ) / 4;
        if (avg > sea - minDepth)
        {
            return false;
        }

        int minNeighbor = Math.Min(Math.Min(heightX, heightZ), heightXZ);
        if (height > minNeighbor + 1)
        {
            return false;
        }

        return true;
    }

    private int ApplyCliffSmoothing(int height, int heightX, int heightZ, int heightXZ, float slope)
    {
        if (_settings.CliffSmoothStrength <= 0f)
        {
            return height;
        }

        if (slope <= _settings.CliffSlopeThreshold)
        {
            return height;
        }

        int avg = (height + heightX + heightZ + heightXZ) / 4;
        float t = MathUtil.Clamp((slope - _settings.CliffSlopeThreshold) / _settings.CliffSlopeThreshold, 0f, 1f);
        return (int)MathF.Round(MathUtil.Lerp(height, avg, _settings.CliffSmoothStrength * t));
    }

    private bool IsTrail(int worldX, int worldZ, float trailScale)
    {
        float primary = MathF.Abs(_noise.Trail(worldX * trailScale, worldZ * trailScale));
        if (primary >= _settings.TrailThreshold)
        {
            return false;
        }

        float secondaryScale = _settings.TrailSecondaryScale;
        float secondary = MathF.Abs(_noise.Trail(worldX * secondaryScale + 1000f, worldZ * secondaryScale - 1000f));
        return secondary < _settings.TrailSecondaryThreshold;
    }

    private static int HashSeed(int seed, int x, int z)
    {
        unchecked
        {
            int hash = seed;
            hash = (hash * 397) ^ x;
            hash = (hash * 397) ^ z;
            return hash;
        }
    }

    private static void GenerateOre(Chunk chunk, Random rand, int x, int z, BlockId ore, int minY, int maxY, int attempts)
    {
        for (int i = 0; i < attempts; i++)
        {
            int y = rand.Next(minY, Math.Min(maxY, Chunk.SizeY - 1));
            if (chunk.GetBlock(x, y, z) == BlockId.Stone)
            {
                chunk.SetBlock(x, y, z, ore);
            }
        }
    }

    private HeightSample SampleHeight(int worldX, int worldZ, float riverWidth, float detailScale, float duneScale)
    {
        int baseHeight = _heightSampler.GetSmoothedHeight(worldX, worldZ);
        _heightSampler.GetBaseHeight(worldX, worldZ, out float ridge);
        float riverVal = _heightSampler.GetRiverValue(worldX, worldZ);
        var biome = _heightSampler.GetBiomeId(worldX, worldZ, riverVal);

        float height = baseHeight;
        if (!_settings.StrictBetaMode)
        {
            height = ApplyBiomeHeight(baseHeight, biome);
        }

        float detail = _noise.Detail(worldX * detailScale, worldZ * detailScale) * GetDetailAmplitude(biome);
        height += detail;

        if (biome == BiomeId.Desert)
        {
            float dune = MathF.Abs(_noise.Dune(worldX * duneScale, worldZ * duneScale));
            height += dune * _settings.DesertDuneAmplitude;
        }

        float riverInfluence = 0f;
        if (riverWidth > 0f)
        {
            float influenceWidth = MathF.Max(0.5f, _settings.RiverInfluenceWidth);
            riverInfluence = MathUtil.Clamp(1f - riverVal / (riverWidth * influenceWidth), 0f, 1f);
            height -= riverInfluence * _settings.RiverDepth;
        }

        return new HeightSample((int)MathF.Round(height), biome, riverVal, riverInfluence, ridge);
    }

    private float GetDetailAmplitude(BiomeId biome)
    {
        return biome switch
        {
            BiomeId.Forest => _settings.ForestDetailAmplitude,
            BiomeId.Plains => _settings.PlainsDetailAmplitude,
            BiomeId.Desert => _settings.DesertDetailAmplitude,
            BiomeId.Mountains => _settings.MountainsDetailAmplitude,
            BiomeId.Taiga => _settings.TaigaDetailAmplitude,
            BiomeId.Tundra => _settings.TundraDetailAmplitude,
            BiomeId.Swamp => _settings.SwampDetailAmplitude,
            BiomeId.Savanna => _settings.SavannaDetailAmplitude,
            BiomeId.Shrubland => _settings.ShrublandDetailAmplitude,
            _ => _settings.PlainsDetailAmplitude
        };
    }

    private float GetTreeChance(BiomeId biome)
    {
        return biome switch
        {
            BiomeId.Forest => _settings.ForestTreeChance,
            BiomeId.Plains => _settings.PlainsTreeChance,
            BiomeId.Desert => 0f,
            BiomeId.Taiga => _settings.TaigaTreeChance,
            BiomeId.Tundra => _settings.TundraTreeChance,
            BiomeId.Swamp => _settings.SwampTreeChance,
            BiomeId.Savanna => _settings.SavannaTreeChance,
            BiomeId.Shrubland => _settings.ShrublandTreeChance,
            BiomeId.River => 0f,
            BiomeId.Mountains => _settings.MountainsTreeChance,
            _ => _settings.TreeChance
        };
    }

    private float GetTallGrassChance(BiomeId biome)
    {
        return biome switch
        {
            BiomeId.Forest => _settings.ForestTallGrassChance,
            BiomeId.Plains => _settings.PlainsTallGrassChance,
            BiomeId.Taiga => _settings.TaigaTallGrassChance,
            BiomeId.Tundra => _settings.TundraTallGrassChance,
            BiomeId.Savanna => _settings.SavannaTallGrassChance,
            BiomeId.Shrubland => _settings.ShrublandTallGrassChance,
            BiomeId.Swamp => _settings.ForestTallGrassChance * 0.6f,
            _ => 0f
        };
    }

    private float GetFlowerChance(BiomeId biome)
    {
        return biome switch
        {
            BiomeId.Forest => _settings.ForestFlowerChance,
            BiomeId.Swamp => _settings.SwampFlowerChance,
            BiomeId.Plains => _settings.ForestFlowerChance * 0.5f,
            _ => 0f
        };
    }

    private static bool CanPlaceCactus(Chunk chunk, int x, int y, int z)
    {
        if (y <= 0 || y >= Chunk.SizeY - 1)
        {
            return false;
        }

        if (chunk.GetBlock(x, y, z) != BlockId.Air)
        {
            return false;
        }

        if (x > 0 && chunk.GetBlock(x - 1, y, z) != BlockId.Air) return false;
        if (x < Chunk.SizeX - 1 && chunk.GetBlock(x + 1, y, z) != BlockId.Air) return false;
        if (z > 0 && chunk.GetBlock(x, y, z - 1) != BlockId.Air) return false;
        if (z < Chunk.SizeZ - 1 && chunk.GetBlock(x, y, z + 1) != BlockId.Air) return false;

        return true;
    }

    private static void PlaceColumn(Chunk chunk, int x, int y, int z, BlockId block, int height)
    {
        int maxY = Math.Min(Chunk.SizeY - 1, y + height - 1);
        for (int ty = y; ty <= maxY; ty++)
        {
            if (chunk.GetBlock(x, ty, z) != BlockId.Air)
            {
                break;
            }
            chunk.SetBlock(x, ty, z, block);
        }
    }

    private static bool IsAdjacentToWater(Chunk chunk, int x, int y, int z)
    {
        if (IsWaterAt(chunk, x - 1, y, z) || IsWaterAt(chunk, x + 1, y, z) ||
            IsWaterAt(chunk, x, y, z - 1) || IsWaterAt(chunk, x, y, z + 1) ||
            IsWaterAt(chunk, x - 1, y + 1, z) || IsWaterAt(chunk, x + 1, y + 1, z) ||
            IsWaterAt(chunk, x, y + 1, z - 1) || IsWaterAt(chunk, x, y + 1, z + 1))
        {
            return true;
        }
        return false;
    }

    private static bool IsWaterAt(Chunk chunk, int x, int y, int z)
    {
        if (x < 0 || x >= Chunk.SizeX || z < 0 || z >= Chunk.SizeZ || y < 0 || y >= Chunk.SizeY)
        {
            return false;
        }

        return WaterBlocks.IsWater(chunk.GetBlock(x, y, z));
    }

    private readonly struct HeightSample
    {
        public int Height { get; }
        public BiomeId Biome { get; }
        public float RiverValue { get; }
        public float RiverInfluence { get; }
        public float Ridge { get; }

        public HeightSample(int height, BiomeId biome, float riverValue, float riverInfluence, float ridge)
        {
            Height = height;
            Biome = biome;
            RiverValue = riverValue;
            RiverInfluence = riverInfluence;
            Ridge = ridge;
        }
    }
}
