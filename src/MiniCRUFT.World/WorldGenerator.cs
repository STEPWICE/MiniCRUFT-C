using System;
using MiniCRUFT.Core;

namespace MiniCRUFT.World;

public sealed class WorldGenerator
{
    private readonly NoiseService _noise;
    private readonly WorldGenSettings _settings;
    private readonly int _seed;
    private readonly WorldHeightSampler _heightSampler;

    public WorldGenerator(int seed, WorldGenSettings settings)
    {
        _seed = seed;
        _settings = settings;
        _noise = new NoiseService(seed);
        _heightSampler = new WorldHeightSampler(seed, settings);
    }

    public void GenerateChunk(Chunk chunk)
    {
        var rand = new Random(HashSeed(_seed, chunk.ChunkX, chunk.ChunkZ));

        for (int x = 0; x < Chunk.SizeX; x++)
        {
            for (int z = 0; z < Chunk.SizeZ; z++)
            {
                int worldX = chunk.ChunkX * Chunk.SizeX + x;
                int worldZ = chunk.ChunkZ * Chunk.SizeZ + z;

                int height = _heightSampler.GetBaseHeight(worldX, worldZ, out float ridge);
                int heightX = _heightSampler.GetBaseHeight(worldX + 1, worldZ, out _);
                int heightZ = _heightSampler.GetBaseHeight(worldX, worldZ + 1, out _);
                float slope = MathF.Max(MathF.Abs(height - heightX), MathF.Abs(height - heightZ));

                float riverVal = _heightSampler.GetRiverValue(worldX, worldZ);
                var biome = _heightSampler.GetBiomeId(worldX, worldZ, riverVal);
                bool isRiver = biome == BiomeId.River;
                if (isRiver)
                {
                    height = Math.Min(height, _settings.SeaLevel - 3);
                }
                if (_settings.CliffSmoothStrength > 0f && slope > _settings.CliffSlopeThreshold)
                {
                    float avg = (height + heightX + heightZ) / 3f;
                    height = (int)MathF.Round(MathUtil.Lerp(height, avg, _settings.CliffSmoothStrength));
                }

                height = Math.Clamp(height, 1, Chunk.SizeY - 2);
                chunk.SetBiome(x, z, biome);

                BlockId surface = BiomeRegistry.Get(biome).SurfaceBlock;

                bool cliff = slope > _settings.CliffSlopeThreshold || ridge > 0.85f;
                if (height <= _settings.SeaLevel + _settings.BeachSize && !cliff)
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

                for (int y = height + 1; y <= _settings.SeaLevel; y++)
                {
                    chunk.SetBlock(x, y, z, BlockId.Water);
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
                bool heightAboveWater = height >= _settings.SeaLevel;
                bool slopeOk = slope <= _settings.TreeMaxSlope;

                if ((biome == BiomeId.Forest || biome == BiomeId.Plains) && rand.NextDouble() < _settings.TreeChance)
                {
                    int treeY = height + 1;
                    if (treeY < Chunk.SizeY - 6 && groundSuitable && heightAboveWater && slopeOk)
                    {
                        bool largePlaced = false;
                        if (biome == BiomeId.Forest && rand.NextDouble() < _settings.LargeTreeChance)
                        {
                            largePlaced = TreeGenerator.GenerateLargeOak(chunk, x, treeY, z, rand, _settings.LargeTreeMinHeight, _settings.LargeTreeMaxHeight, _settings.LargeTreeLeafRadius, _settings.LargeTreeCanopyDepth);
                        }

                        if (!largePlaced)
                        {
                            double roll = rand.NextDouble();
                            if (roll < 0.5)
                            {
                                TreeGenerator.GenerateOak(chunk, x, treeY, z, rand);
                            }
                            else
                            {
                                TreeGenerator.GenerateBirch(chunk, x, treeY, z, rand);
                            }
                        }
                    }
                }
                else if (biome == BiomeId.Mountains && rand.NextDouble() < _settings.TreeChance * 0.6)
                {
                    int treeY = height + 1;
                    if (treeY < Chunk.SizeY - 8 && groundSuitable && heightAboveWater && slopeOk)
                    {
                        TreeGenerator.GenerateSpruce(chunk, x, treeY, z, rand);
                    }
                }

                if (biome == BiomeId.Forest && rand.NextDouble() < 0.05)
                {
                    int flowerY = height + 1;
                    if (flowerY < Chunk.SizeY - 1 && groundBlock == BlockId.Grass && heightAboveWater &&
                        chunk.GetBlock(x, flowerY, z) == BlockId.Air)
                    {
                        chunk.SetBlock(x, flowerY, z, BlockId.Flower);
                    }
                }

                if (biome == BiomeId.Plains && rand.NextDouble() < 0.08)
                {
                    int grassY = height + 1;
                    if (grassY < Chunk.SizeY - 1 && groundBlock == BlockId.Grass && heightAboveWater &&
                        chunk.GetBlock(x, grassY, z) == BlockId.Air)
                    {
                        chunk.SetBlock(x, grassY, z, BlockId.TallGrass);
                    }
                }
            }
        }

        WorldLighting.RecalculateChunkLighting(chunk);
        chunk.MarkDirty();
    }

    public int EstimateSurfaceHeight(int worldX, int worldZ)
    {
        int height = _heightSampler.GetBaseHeight(worldX, worldZ, out float ridge);
        int heightX = _heightSampler.GetBaseHeight(worldX + 1, worldZ, out _);
        int heightZ = _heightSampler.GetBaseHeight(worldX, worldZ + 1, out _);
        float slope = MathF.Max(MathF.Abs(height - heightX), MathF.Abs(height - heightZ));

        float riverVal = _heightSampler.GetRiverValue(worldX, worldZ);
        if (riverVal < _settings.RiverThreshold)
        {
            height = Math.Min(height, _settings.SeaLevel - 3);
        }
        if (_settings.CliffSmoothStrength > 0f && slope > _settings.CliffSlopeThreshold)
        {
            float avg = (height + heightX + heightZ) / 3f;
            height = (int)MathF.Round(MathUtil.Lerp(height, avg, _settings.CliffSmoothStrength));
        }

        height = Math.Clamp(height, 1, Chunk.SizeY - 2);
        return height;
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
}
