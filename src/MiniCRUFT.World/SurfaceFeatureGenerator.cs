using System;

namespace MiniCRUFT.World;

public sealed class SurfaceFeatureGenerator
{
    private readonly WorldGenSettings _settings;

    public SurfaceFeatureGenerator(WorldGenSettings settings)
    {
        _settings = settings;
    }

    public void Apply(Chunk chunk, int[] surfaceHeights, Random rand)
    {
        if (_settings.StrictBetaMode)
        {
            return;
        }

        if (surfaceHeights.Length < Chunk.SizeX * Chunk.SizeZ)
        {
            return;
        }

        TryPlacePonds(chunk, surfaceHeights, rand);
        TryPlaceBoulders(chunk, surfaceHeights, rand);
        TryPlaceFallenLogs(chunk, surfaceHeights, rand);
        TryPlaceFlowerPatches(chunk, surfaceHeights, rand);
        TryPlaceGravelPatches(chunk, surfaceHeights, rand);
    }

    private void TryPlacePonds(Chunk chunk, int[] heights, Random rand)
    {
        int attempts = ClampAttempts(_settings.PondChance, 2);
        for (int i = 0; i < attempts; i++)
        {
            if (rand.NextDouble() >= _settings.PondChance)
            {
                continue;
            }

            int cx = rand.Next(2, Chunk.SizeX - 2);
            int cz = rand.Next(2, Chunk.SizeZ - 2);
            var biome = chunk.GetBiome(cx, cz);
            if (!IsPondBiome(biome))
            {
                continue;
            }

            int centerHeight = heights[Chunk.GetBiomeIndex(cx, cz)];
            if (centerHeight <= 4 || centerHeight >= Chunk.SizeY - 6)
            {
                continue;
            }

            if (GetMaxNeighborDelta(heights, cx, cz) > 2)
            {
                continue;
            }

            int radius = rand.Next(_settings.PondRadiusMin, _settings.PondRadiusMax + 1);
            int waterLevel = Math.Min(_settings.SeaLevel - 1, centerHeight - 1);
            if (waterLevel <= 2)
            {
                continue;
            }

            int bottom = Math.Max(1, waterLevel - _settings.PondDepth);
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (x < 0 || x >= Chunk.SizeX) continue;
                for (int z = cz - radius; z <= cz + radius; z++)
                {
                    if (z < 0 || z >= Chunk.SizeZ) continue;
                    int dx = x - cx;
                    int dz = z - cz;
                    if (dx * dx + dz * dz > radius * radius)
                    {
                        continue;
                    }

                    int baseHeight = heights[Chunk.GetBiomeIndex(x, z)];
                    if (Math.Abs(baseHeight - centerHeight) > 4)
                    {
                        continue;
                    }

                    BlockId bottomBlock = chunk.GetBlock(x, baseHeight, z) == BlockId.Sand ? BlockId.Sand : BlockId.Dirt;

                    for (int y = baseHeight; y > waterLevel; y--)
                    {
                        chunk.SetBlock(x, y, z, BlockId.Air);
                    }

                    for (int y = bottom; y <= waterLevel; y++)
                    {
                        if (y == bottom)
                        {
                            chunk.SetBlock(x, y, z, bottomBlock);
                        }
                        else
                        {
                            chunk.SetBlock(x, y, z, WaterBlocks.SourceId);
                        }
                    }

                    if (waterLevel + 1 < Chunk.SizeY)
                    {
                        chunk.SetBlock(x, waterLevel + 1, z, BlockId.Air);
                    }
                }
            }
        }
    }

    private void TryPlaceBoulders(Chunk chunk, int[] heights, Random rand)
    {
        int attempts = ClampAttempts(_settings.BoulderChance, 2);
        for (int i = 0; i < attempts; i++)
        {
            if (rand.NextDouble() >= _settings.BoulderChance)
            {
                continue;
            }

            int cx = rand.Next(1, Chunk.SizeX - 1);
            int cz = rand.Next(1, Chunk.SizeZ - 1);
            var biome = chunk.GetBiome(cx, cz);
            if (!IsBoulderBiome(biome))
            {
                continue;
            }

            int groundY = heights[Chunk.GetBiomeIndex(cx, cz)];
            if (groundY <= _settings.SeaLevel - 1 || groundY >= Chunk.SizeY - 4)
            {
                continue;
            }

            if (GetMaxNeighborDelta(heights, cx, cz) > 3)
            {
                continue;
            }

            int radius = rand.Next(_settings.BoulderRadiusMin, _settings.BoulderRadiusMax + 1);
            BlockId rock = rand.NextDouble() < _settings.BoulderStoneChance ? BlockId.Stone : BlockId.Gravel;
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (x < 0 || x >= Chunk.SizeX) continue;
                for (int z = cz - radius; z <= cz + radius; z++)
                {
                    if (z < 0 || z >= Chunk.SizeZ) continue;
                    int dx = x - cx;
                    int dz = z - cz;
                    int distSq = dx * dx + dz * dz;
                    if (distSq > radius * radius)
                    {
                        continue;
                    }

                    int height = (int)MathF.Round(MathF.Sqrt(radius * radius - distSq));
                    for (int y = 0; y <= height; y++)
                    {
                        int placeY = groundY + y;
                        if (placeY >= Chunk.SizeY - 1)
                        {
                            break;
                        }

                        if (y == 0 && WaterBlocks.IsWater(chunk.GetBlock(x, placeY, z)))
                        {
                            break;
                        }

                        if (y > 0 || chunk.GetBlock(x, placeY, z) == BlockId.Air)
                        {
                            chunk.SetBlock(x, placeY, z, rock);
                        }
                    }
                }
            }
        }
    }

    private void TryPlaceFallenLogs(Chunk chunk, int[] heights, Random rand)
    {
        int attempts = ClampAttempts(_settings.FallenLogChance, 2);
        for (int i = 0; i < attempts; i++)
        {
            if (rand.NextDouble() >= _settings.FallenLogChance)
            {
                continue;
            }

            int cx = rand.Next(1, Chunk.SizeX - 1);
            int cz = rand.Next(1, Chunk.SizeZ - 1);
            int length = rand.Next(_settings.FallenLogMinLength, _settings.FallenLogMaxLength + 1);
            bool alongX = rand.Next(2) == 0;

            var biome = chunk.GetBiome(cx, cz);
            if (!IsLogBiome(biome))
            {
                continue;
            }

            BlockId logBlock = PickLogBlock(biome, rand);
            int placed = 0;
            for (int iLen = 0; iLen < length; iLen++)
            {
                int x = cx + (alongX ? iLen : 0);
                int z = cz + (alongX ? 0 : iLen);
                if (x < 0 || x >= Chunk.SizeX || z < 0 || z >= Chunk.SizeZ)
                {
                    break;
                }

                int groundY = heights[Chunk.GetBiomeIndex(x, z)];
                int placeY = groundY + 1;
                if (placeY >= Chunk.SizeY - 1)
                {
                    break;
                }

                if (chunk.GetBlock(x, placeY, z) != BlockId.Air)
                {
                    break;
                }

                if (WaterBlocks.IsWater(chunk.GetBlock(x, groundY, z)))
                {
                    break;
                }

                chunk.SetBlock(x, placeY, z, logBlock);
                placed++;
            }

            if (placed > 0)
            {
                return;
            }
        }
    }

    private void TryPlaceFlowerPatches(Chunk chunk, int[] heights, Random rand)
    {
        int attempts = ClampAttempts(_settings.FlowerPatchChance, 3);
        for (int i = 0; i < attempts; i++)
        {
            if (rand.NextDouble() >= _settings.FlowerPatchChance)
            {
                continue;
            }

            int cx = rand.Next(1, Chunk.SizeX - 1);
            int cz = rand.Next(1, Chunk.SizeZ - 1);
            var biome = chunk.GetBiome(cx, cz);
            if (!IsFlowerBiome(biome))
            {
                continue;
            }

            int radius = rand.Next(_settings.FlowerPatchRadiusMin, _settings.FlowerPatchRadiusMax + 1);
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (x < 0 || x >= Chunk.SizeX) continue;
                for (int z = cz - radius; z <= cz + radius; z++)
                {
                    if (z < 0 || z >= Chunk.SizeZ) continue;
                    int dx = x - cx;
                    int dz = z - cz;
                    if (dx * dx + dz * dz > radius * radius)
                    {
                        continue;
                    }

                    if (rand.NextDouble() > 0.7)
                    {
                        continue;
                    }

                    int groundY = heights[Chunk.GetBiomeIndex(x, z)];
                    if (groundY <= _settings.SeaLevel - 1)
                    {
                        continue;
                    }

                    if (chunk.GetBlock(x, groundY, z) != BlockId.Grass)
                    {
                        continue;
                    }

                    if (chunk.GetBlock(x, groundY + 1, z) != BlockId.Air)
                    {
                        continue;
                    }

                    chunk.SetBlock(x, groundY + 1, z, BlockId.Flower);
                }
            }
        }
    }

    private void TryPlaceGravelPatches(Chunk chunk, int[] heights, Random rand)
    {
        int attempts = ClampAttempts(_settings.GravelPatchChance, 2);
        for (int i = 0; i < attempts; i++)
        {
            if (rand.NextDouble() >= _settings.GravelPatchChance)
            {
                continue;
            }

            int cx = rand.Next(1, Chunk.SizeX - 1);
            int cz = rand.Next(1, Chunk.SizeZ - 1);
            var biome = chunk.GetBiome(cx, cz);
            if (!IsGravelBiome(biome))
            {
                continue;
            }

            int radius = rand.Next(_settings.GravelPatchRadiusMin, _settings.GravelPatchRadiusMax + 1);
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (x < 0 || x >= Chunk.SizeX) continue;
                for (int z = cz - radius; z <= cz + radius; z++)
                {
                    if (z < 0 || z >= Chunk.SizeZ) continue;
                    int dx = x - cx;
                    int dz = z - cz;
                    if (dx * dx + dz * dz > radius * radius)
                    {
                        continue;
                    }

                    int groundY = heights[Chunk.GetBiomeIndex(x, z)];
                    BlockId ground = chunk.GetBlock(x, groundY, z);
                    if (ground is BlockId.Dirt or BlockId.Grass or BlockId.Sand or BlockId.Stone)
                    {
                        chunk.SetBlock(x, groundY, z, BlockId.Gravel);
                    }
                }
            }
        }
    }

    private static int ClampAttempts(float chance, int maxAttempts)
    {
        if (chance <= 0f)
        {
            return 0;
        }

        int attempts = (int)MathF.Round(chance * 8f);
        if (attempts < 1) attempts = 1;
        if (attempts > maxAttempts) attempts = maxAttempts;
        return attempts;
    }

    private static int GetMaxNeighborDelta(int[] heights, int x, int z)
    {
        int index = Chunk.GetBiomeIndex(x, z);
        int center = heights[index];
        int maxDelta = 0;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dz == 0) continue;
                int nx = x + dx;
                int nz = z + dz;
                if (nx < 0 || nx >= Chunk.SizeX || nz < 0 || nz >= Chunk.SizeZ)
                {
                    continue;
                }

                int neighbor = heights[Chunk.GetBiomeIndex(nx, nz)];
                int delta = Math.Abs(center - neighbor);
                if (delta > maxDelta)
                {
                    maxDelta = delta;
                }
            }
        }

        return maxDelta;
    }

    private static bool IsPondBiome(BiomeId biome)
    {
        return biome is BiomeId.Forest or BiomeId.Plains or BiomeId.Savanna or BiomeId.Shrubland or BiomeId.Taiga or BiomeId.Swamp;
    }

    private static bool IsBoulderBiome(BiomeId biome)
    {
        return biome is BiomeId.Mountains or BiomeId.Taiga or BiomeId.Tundra or BiomeId.Shrubland;
    }

    private static bool IsLogBiome(BiomeId biome)
    {
        return biome is BiomeId.Forest or BiomeId.Plains or BiomeId.Taiga or BiomeId.Swamp or BiomeId.Savanna or BiomeId.Shrubland;
    }

    private static bool IsFlowerBiome(BiomeId biome)
    {
        return biome is BiomeId.Plains or BiomeId.Forest or BiomeId.Savanna or BiomeId.Shrubland;
    }

    private static bool IsGravelBiome(BiomeId biome)
    {
        return biome is BiomeId.Mountains or BiomeId.River or BiomeId.Taiga;
    }

    private static BlockId PickLogBlock(BiomeId biome, Random rand)
    {
        return biome switch
        {
            BiomeId.Taiga => BlockId.SpruceWood,
            BiomeId.Swamp => BlockId.Wood,
            BiomeId.Forest => rand.NextDouble() < 0.3 ? BlockId.BirchWood : BlockId.Wood,
            _ => BlockId.Wood
        };
    }
}
