using System;
using MiniCRUFT.Core;

namespace MiniCRUFT.World;

public sealed class StructureGenerator
{
    private const int CampSalt = 0x31;
    private const int WatchtowerSalt = 0x43;
    private const int BuriedCacheSalt = 0x59;
    private const int CaveCacheSalt = 0x6B;
    private const int RuinSalt = 0x7D;
    private const int MineShaftSalt = 0x8F;

    private enum StructureKind
    {
        Camp,
        Watchtower,
        BuriedCache,
        CaveCache,
        Ruin,
        MineShaft
    }

    private struct StructureAttempt
    {
        public StructureKind Kind;
        public float Weight;

        public StructureAttempt(StructureKind kind, float weight)
        {
            Kind = kind;
            Weight = weight;
        }
    }

    private readonly int _seed;
    private readonly WorldGenSettings _settings;

    public StructureGenerator(int seed, WorldGenSettings settings)
    {
        _seed = seed;
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public void Apply(Chunk chunk, int[] surfaceHeights)
    {
        if (_settings.StrictBetaMode || surfaceHeights.Length < Chunk.SizeX * Chunk.SizeZ)
        {
            return;
        }

        if (!RollChance(chunk, 0x21, _settings.StructureChance))
        {
            return;
        }

        float campWeight = Math.Max(0f, _settings.CampChance);
        float watchtowerWeight = Math.Max(0f, _settings.WatchtowerChance);
        float buriedCacheWeight = Math.Max(0f, _settings.BuriedCacheChance);
        float caveCacheWeight = Math.Max(0f, _settings.CaveCacheChance);
        float ruinWeight = Math.Max(0f, _settings.RuinChance);
        float mineShaftWeight = Math.Max(0f, _settings.MineShaftChance);
        float totalWeight = campWeight + watchtowerWeight + buriedCacheWeight + caveCacheWeight + ruinWeight + mineShaftWeight;
        if (totalWeight <= 0f)
        {
            return;
        }

        var rand = CreateRandom(chunk, 0x31);
        float roll = (float)rand.NextDouble() * totalWeight;

        StructureKind chosenKind;
        float chosenWeight;
        if (campWeight > 0f && (roll -= campWeight) <= 0f)
        {
            chosenKind = StructureKind.Camp;
            chosenWeight = campWeight;
        }
        else if (watchtowerWeight > 0f && (roll -= watchtowerWeight) <= 0f)
        {
            chosenKind = StructureKind.Watchtower;
            chosenWeight = watchtowerWeight;
        }
        else if (buriedCacheWeight > 0f && (roll -= buriedCacheWeight) <= 0f)
        {
            chosenKind = StructureKind.BuriedCache;
            chosenWeight = buriedCacheWeight;
        }
        else if (caveCacheWeight > 0f && (roll -= caveCacheWeight) <= 0f)
        {
            chosenKind = StructureKind.CaveCache;
            chosenWeight = caveCacheWeight;
        }
        else if (ruinWeight > 0f && (roll -= ruinWeight) <= 0f)
        {
            chosenKind = StructureKind.Ruin;
            chosenWeight = ruinWeight;
        }
        else
        {
            chosenKind = StructureKind.MineShaft;
            chosenWeight = mineShaftWeight;
        }

        Span<StructureAttempt> attempts = stackalloc StructureAttempt[6];
        int attemptCount = 0;
        attemptCount = AddAttempt(attempts, attemptCount, chosenKind, chosenWeight);
        attemptCount = AddAttempt(attempts, attemptCount, StructureKind.Camp, campWeight, chosenKind);
        attemptCount = AddAttempt(attempts, attemptCount, StructureKind.Watchtower, watchtowerWeight, chosenKind);
        attemptCount = AddAttempt(attempts, attemptCount, StructureKind.BuriedCache, buriedCacheWeight, chosenKind);
        attemptCount = AddAttempt(attempts, attemptCount, StructureKind.CaveCache, caveCacheWeight, chosenKind);
        attemptCount = AddAttempt(attempts, attemptCount, StructureKind.Ruin, ruinWeight, chosenKind);
        attemptCount = AddAttempt(attempts, attemptCount, StructureKind.MineShaft, mineShaftWeight, chosenKind);

        if (attemptCount > 1)
        {
            SortAttemptsByWeightDescending(attempts.Slice(1, attemptCount - 1));
        }

        for (int i = 0; i < attemptCount; i++)
        {
            if (TryPlaceStructure(attempts[i].Kind, chunk, surfaceHeights))
            {
                return;
            }
        }
    }

    private bool TryPlaceStructure(StructureKind kind, Chunk chunk, int[] surfaceHeights)
    {
        return kind switch
        {
            StructureKind.Camp => TryPlaceCamp(chunk, surfaceHeights),
            StructureKind.Watchtower => TryPlaceWatchtower(chunk, surfaceHeights),
            StructureKind.BuriedCache => TryPlaceBuriedCache(chunk, surfaceHeights),
            StructureKind.CaveCache => TryPlaceCaveCache(chunk, surfaceHeights),
            StructureKind.Ruin => TryPlaceRuin(chunk, surfaceHeights),
            StructureKind.MineShaft => TryPlaceMineShaft(chunk, surfaceHeights),
            _ => false
        };
    }

    private static int AddAttempt(Span<StructureAttempt> attempts, int count, StructureKind kind, float weight)
    {
        if (weight <= 0f)
        {
            return count;
        }

        attempts[count++] = new StructureAttempt(kind, weight);
        return count;
    }

    private static int AddAttempt(Span<StructureAttempt> attempts, int count, StructureKind kind, float weight, StructureKind excludedKind)
    {
        if (kind == excludedKind || weight <= 0f)
        {
            return count;
        }

        attempts[count++] = new StructureAttempt(kind, weight);
        return count;
    }

    private static void SortAttemptsByWeightDescending(Span<StructureAttempt> attempts)
    {
        for (int i = 0; i < attempts.Length - 1; i++)
        {
            int bestIndex = i;
            for (int j = i + 1; j < attempts.Length; j++)
            {
                if (attempts[j].Weight > attempts[bestIndex].Weight)
                {
                    bestIndex = j;
                }
            }

            if (bestIndex == i)
            {
                continue;
            }

            StructureAttempt current = attempts[i];
            attempts[i] = attempts[bestIndex];
            attempts[bestIndex] = current;
        }
    }

    private bool TryPlaceCamp(Chunk chunk, int[] surfaceHeights)
    {
        var rand = CreateRandom(chunk, CampSalt ^ 0x11);
        if (!TryFindSurfaceCandidate(
                chunk,
                surfaceHeights,
                rand,
                IsCampBiome,
                margin: GetStructureMargin(),
                minHeight: _settings.SeaLevel + 2,
                maxDelta: 2,
                attempts: 24,
                out int x,
                out int z,
                out int surfaceY))
        {
            return false;
        }

        BuildCamp(chunk, x, z, surfaceY);
        return true;
    }

    private bool TryPlaceWatchtower(Chunk chunk, int[] surfaceHeights)
    {
        var rand = CreateRandom(chunk, WatchtowerSalt ^ 0x11);
        if (!TryFindSurfaceCandidate(
                chunk,
                surfaceHeights,
                rand,
                IsWatchtowerBiome,
                margin: GetStructureMargin(),
                minHeight: _settings.SeaLevel + 6,
                maxDelta: 3,
                attempts: 18,
                out int x,
                out int z,
                out int surfaceY))
        {
            return false;
        }

        BuildWatchtower(chunk, x, z, surfaceY);
        return true;
    }

    private bool TryPlaceBuriedCache(Chunk chunk, int[] surfaceHeights)
    {
        var rand = CreateRandom(chunk, BuriedCacheSalt ^ 0x11);
        if (!TryFindSurfaceCandidate(
                chunk,
                surfaceHeights,
                rand,
                IsBuriedCacheBiome,
                margin: GetStructureMargin(),
                minHeight: _settings.SeaLevel + 1,
                maxDelta: 2,
                attempts: 20,
                out int x,
                out int z,
                out int surfaceY))
        {
            return false;
        }

        if (surfaceY < 3)
        {
            return false;
        }

        BuildBuriedCache(chunk, x, z, surfaceY);
        return true;
    }

    private bool TryPlaceCaveCache(Chunk chunk, int[] surfaceHeights)
    {
        var rand = CreateRandom(chunk, CaveCacheSalt ^ 0x11);
        if (!TryFindUndergroundCandidate(chunk, surfaceHeights, rand, IsCaveCacheBiome, GetStructureMargin(), 24, out int x, out int z, out int floorY))
        {
            return false;
        }

        BuildCaveCache(chunk, x, z, floorY);
        return true;
    }

    private bool TryPlaceRuin(Chunk chunk, int[] surfaceHeights)
    {
        var rand = CreateRandom(chunk, RuinSalt ^ 0x11);
        if (!TryFindSurfaceCandidate(
                chunk,
                surfaceHeights,
                rand,
                IsRuinBiome,
                margin: GetStructureMargin(),
                minHeight: _settings.SeaLevel + 18,
                maxDelta: 2,
                attempts: 20,
                out int x,
                out int z,
                out int surfaceY))
        {
            return false;
        }

        if (surfaceY > Chunk.SizeY - 6)
        {
            return false;
        }

        BuildRuin(chunk, x, z, surfaceY, rand);
        return true;
    }

    private bool TryPlaceMineShaft(Chunk chunk, int[] surfaceHeights)
    {
        var rand = CreateRandom(chunk, MineShaftSalt ^ 0x11);
        if (!TryFindUndergroundCandidate(chunk, surfaceHeights, rand, IsMineShaftBiome, GetStructureMargin(), 32, out int x, out int z, out int floorY))
        {
            return false;
        }

        BuildMineShaft(chunk, x, z, floorY, rand);
        return true;
    }

    private static bool TryFindSurfaceCandidate(
        Chunk chunk,
        int[] surfaceHeights,
        Random rand,
        Func<BiomeId, bool> biomePredicate,
        int margin,
        int minHeight,
        int maxDelta,
        int attempts,
        out int x,
        out int z,
        out int surfaceY)
    {
        int min = margin;
        int max = Chunk.SizeX - margin;
        if (max <= min)
        {
            x = 0;
            z = 0;
            surfaceY = 0;
            return false;
        }

        for (int i = 0; i < attempts; i++)
        {
            int candidateX = rand.Next(min, max);
            int candidateZ = rand.Next(min, max);
            BiomeId biome = chunk.GetBiome(candidateX, candidateZ);
            if (!biomePredicate(biome))
            {
                continue;
            }

            int index = Chunk.GetBiomeIndex(candidateX, candidateZ);
            int height = surfaceHeights[index];
            if (height < minHeight || height >= Chunk.SizeY - 3)
            {
                continue;
            }

            if (GetMaxNeighborDelta(surfaceHeights, candidateX, candidateZ) > maxDelta)
            {
                continue;
            }

            x = candidateX;
            z = candidateZ;
            surfaceY = height;
            return true;
        }

        x = 0;
        z = 0;
        surfaceY = 0;
        return false;
    }

    private static bool TryFindUndergroundCandidate(
        Chunk chunk,
        int[] surfaceHeights,
        Random rand,
        Func<BiomeId, bool> biomePredicate,
        int margin,
        int attempts,
        out int x,
        out int z,
        out int floorY)
    {
        margin = Math.Clamp(margin, 0, 6);
        int min = margin;
        int max = Chunk.SizeX - margin;
        if (max <= min)
        {
            min = 1;
            max = Chunk.SizeX - 1;
        }

        for (int i = 0; i < attempts; i++)
        {
            int candidateX = rand.Next(min, max);
            int candidateZ = rand.Next(min, max);
            BiomeId biome = chunk.GetBiome(candidateX, candidateZ);
            if (!biomePredicate(biome))
            {
                continue;
            }

            int surfaceY = surfaceHeights[Chunk.GetBiomeIndex(candidateX, candidateZ)];
            int topY = Math.Min(surfaceY - 4, Chunk.SizeY - 3);
            if (topY < 12)
            {
                continue;
            }

            for (int y = topY; y >= 12; y--)
            {
                if (chunk.GetBlock(candidateX, y, candidateZ) != BlockId.Air)
                {
                    continue;
                }

                if (chunk.GetBlock(candidateX, y + 1, candidateZ) != BlockId.Air)
                {
                    continue;
                }

                if (!BlockRegistry.Get(chunk.GetBlock(candidateX, y - 1, candidateZ)).IsSolid)
                {
                    continue;
                }

                x = candidateX;
                z = candidateZ;
                floorY = y;
                return true;
            }
        }

        x = 0;
        z = 0;
        floorY = 0;
        return false;
    }

    private void BuildCamp(Chunk chunk, int centerX, int centerZ, int surfaceY)
    {
        int baseY = surfaceY + 1;
        ClearVolume(chunk, centerX, centerZ, surfaceY + 1, surfaceY + 4, 2);

        PlaceFoundation(chunk, centerX, centerZ, baseY, 2, BlockId.Planks);
        PlacePillar(chunk, centerX - 2, centerZ - 2, baseY, 3, BlockId.Wood);
        PlacePillar(chunk, centerX + 2, centerZ - 2, baseY, 3, BlockId.Wood);
        PlacePillar(chunk, centerX - 2, centerZ + 2, baseY, 3, BlockId.Wood);
        PlacePillar(chunk, centerX + 2, centerZ + 2, baseY, 3, BlockId.Wood);

        SetBlock(chunk, centerX, baseY, centerZ, BlockId.Chest);
        SetBlock(chunk, centerX, baseY, centerZ - 1, BlockId.Furnace);
        SetBlock(chunk, centerX, baseY, centerZ + 1, BlockId.CraftingTable);
        SetTorchRing(chunk, centerX, centerZ, baseY + 1);
    }

    private void BuildWatchtower(Chunk chunk, int centerX, int centerZ, int surfaceY)
    {
        int baseY = surfaceY + 1;
        ClearVolume(chunk, centerX, centerZ, surfaceY + 1, surfaceY + 9, 3);

        PlaceFoundation(chunk, centerX, centerZ, baseY, 1, BlockId.Cobblestone);
        PlacePillar(chunk, centerX - 1, centerZ - 1, baseY, 7, BlockId.Wood);
        PlacePillar(chunk, centerX + 1, centerZ - 1, baseY, 7, BlockId.Wood);
        PlacePillar(chunk, centerX - 1, centerZ + 1, baseY, 7, BlockId.Wood);
        PlacePillar(chunk, centerX + 1, centerZ + 1, baseY, 7, BlockId.Wood);
        PlaceFloor(chunk, centerX, centerZ, baseY + 3, 1, BlockId.Planks);
        PlaceFloor(chunk, centerX, centerZ, baseY + 6, 1, BlockId.Planks);
        SetBlock(chunk, centerX, baseY + 6, centerZ, BlockId.Chest);
        SetTorchRing(chunk, centerX, centerZ, baseY + 7);
    }

    private void BuildBuriedCache(Chunk chunk, int centerX, int centerZ, int surfaceY)
    {
        int chestY = Math.Max(1, surfaceY - 1);
        SetBlock(chunk, centerX, chestY, centerZ, BlockId.Chest);
        SetBlock(chunk, centerX, surfaceY, centerZ, BlockId.Gravel);
        SetBlock(chunk, centerX - 1, surfaceY, centerZ, BlockId.Cobblestone);
        SetBlock(chunk, centerX + 1, surfaceY, centerZ, BlockId.Cobblestone);
        SetBlock(chunk, centerX, surfaceY, centerZ - 1, BlockId.Cobblestone);
        SetBlock(chunk, centerX, surfaceY, centerZ + 1, BlockId.Cobblestone);
    }

    private void BuildCaveCache(Chunk chunk, int centerX, int centerZ, int floorY)
    {
        ClearVolume(chunk, centerX, centerZ, floorY, floorY + 2, 1);

        SetBlock(chunk, centerX, floorY, centerZ, BlockId.Chest);
        SetBlock(chunk, centerX - 1, floorY + 1, centerZ, BlockId.Torch);
        SetBlock(chunk, centerX + 1, floorY + 1, centerZ, BlockId.Torch);
        SetBlock(chunk, centerX, floorY + 1, centerZ - 1, BlockId.Torch);
        SetBlock(chunk, centerX, floorY + 1, centerZ + 1, BlockId.Torch);
        SetBlock(chunk, centerX - 1, floorY - 1, centerZ, BlockId.Cobblestone);
        SetBlock(chunk, centerX + 1, floorY - 1, centerZ, BlockId.Cobblestone);
    }

    private void BuildRuin(Chunk chunk, int centerX, int centerZ, int surfaceY, Random rand)
    {
        int baseY = surfaceY + 1;
        ClearVolume(chunk, centerX, centerZ, surfaceY + 1, surfaceY + 6, 3);

        PlaceFoundation(chunk, centerX, centerZ, baseY, 2, BlockId.Cobblestone);
        PlacePillar(chunk, centerX - 2, centerZ - 2, baseY, 4, BlockId.Cobblestone);
        PlacePillar(chunk, centerX + 2, centerZ - 2, baseY, 4, BlockId.Cobblestone);
        PlacePillar(chunk, centerX - 2, centerZ + 2, baseY, 4, BlockId.Cobblestone);
        PlacePillar(chunk, centerX + 2, centerZ + 2, baseY, 4, BlockId.Cobblestone);

        for (int x = centerX - 1; x <= centerX + 1; x++)
        {
            for (int z = centerZ - 1; z <= centerZ + 1; z++)
            {
                if (x == centerX && z == centerZ)
                {
                    continue;
                }

                if (((x + z + chunk.ChunkX + chunk.ChunkZ) & 1) == 0)
                {
                    continue;
                }

                SetBlock(chunk, x, baseY + 3, z, BlockId.Cobblestone);
            }
        }

        SetBlock(chunk, centerX, baseY + 4, centerZ, BlockId.Chest);
        SetBlock(chunk, centerX - 1, baseY + 4, centerZ - 1, BlockId.Torch);
        SetBlock(chunk, centerX + 1, baseY + 4, centerZ - 1, BlockId.Torch);
        SetBlock(chunk, centerX - 1, baseY + 4, centerZ + 1, BlockId.Torch);
        SetBlock(chunk, centerX + 1, baseY + 4, centerZ + 1, BlockId.Torch);

        SetBlock(chunk, centerX - 2, baseY, centerZ, BlockId.Gravel);
        SetBlock(chunk, centerX + 2, baseY, centerZ, BlockId.Gravel);
        if (rand.NextDouble() < 0.5)
        {
            SetBlock(chunk, centerX, baseY, centerZ - 2, BlockId.Cobblestone);
        }
        else
        {
            SetBlock(chunk, centerX, baseY, centerZ + 2, BlockId.Cobblestone);
        }
    }

    private void BuildMineShaft(Chunk chunk, int centerX, int centerZ, int floorY, Random rand)
    {
        ClearVolume(chunk, centerX, centerZ, floorY - 1, floorY + 4, 3);

        PlaceFloor(chunk, centerX, centerZ, floorY, 2, BlockId.Planks);
        PlacePillar(chunk, centerX - 2, centerZ - 2, floorY, 4, BlockId.Wood);
        PlacePillar(chunk, centerX + 2, centerZ - 2, floorY, 4, BlockId.Wood);
        PlacePillar(chunk, centerX - 2, centerZ + 2, floorY, 4, BlockId.Wood);
        PlacePillar(chunk, centerX + 2, centerZ + 2, floorY, 4, BlockId.Wood);
        PlaceFloor(chunk, centerX, centerZ, floorY + 3, 1, BlockId.Planks);

        SetBlock(chunk, centerX - 1, floorY + 2, centerZ, BlockId.Wood);
        SetBlock(chunk, centerX + 1, floorY + 2, centerZ, BlockId.Wood);
        SetBlock(chunk, centerX, floorY + 2, centerZ - 1, BlockId.Wood);
        SetBlock(chunk, centerX, floorY + 2, centerZ + 1, BlockId.Wood);

        SetBlock(chunk, centerX, floorY, centerZ, BlockId.Chest);
        SetBlock(chunk, centerX - 1, floorY + 1, centerZ, BlockId.Torch);
        SetBlock(chunk, centerX + 1, floorY + 1, centerZ, BlockId.Torch);
        SetBlock(chunk, centerX, floorY + 1, centerZ - 1, BlockId.Torch);
        SetBlock(chunk, centerX, floorY + 1, centerZ + 1, BlockId.Torch);

        if (rand.NextDouble() < 0.5)
        {
            SetBlock(chunk, centerX - 3, floorY, centerZ, BlockId.Cobblestone);
        }
        else
        {
            SetBlock(chunk, centerX + 3, floorY, centerZ, BlockId.Cobblestone);
        }
    }

    private static void PlaceFoundation(Chunk chunk, int centerX, int centerZ, int y, int radius, BlockId block)
    {
        PlaceFloor(chunk, centerX, centerZ, y, radius, block);
    }

    private static void PlaceFloor(Chunk chunk, int centerX, int centerZ, int y, int radius, BlockId block)
    {
        for (int x = centerX - radius; x <= centerX + radius; x++)
        {
            if (x < 0 || x >= Chunk.SizeX)
            {
                continue;
            }

            for (int z = centerZ - radius; z <= centerZ + radius; z++)
            {
                if (z < 0 || z >= Chunk.SizeZ)
                {
                    continue;
                }

                chunk.SetBlock(x, y, z, block);
            }
        }
    }

    private static void PlacePillar(Chunk chunk, int x, int z, int baseY, int height, BlockId block)
    {
        if (x < 0 || x >= Chunk.SizeX || z < 0 || z >= Chunk.SizeZ)
        {
            return;
        }

        for (int y = baseY; y < baseY + height && y < Chunk.SizeY; y++)
        {
            chunk.SetBlock(x, y, z, block);
        }
    }

    private static void SetTorchRing(Chunk chunk, int centerX, int centerZ, int y)
    {
        SetBlock(chunk, centerX - 2, y, centerZ - 2, BlockId.Torch);
        SetBlock(chunk, centerX + 2, y, centerZ - 2, BlockId.Torch);
        SetBlock(chunk, centerX - 2, y, centerZ + 2, BlockId.Torch);
        SetBlock(chunk, centerX + 2, y, centerZ + 2, BlockId.Torch);
    }

    private static void ClearVolume(Chunk chunk, int centerX, int centerZ, int fromY, int toY, int radius)
    {
        int minY = Math.Max(0, fromY);
        int maxY = Math.Min(Chunk.SizeY - 1, toY);
        for (int x = centerX - radius; x <= centerX + radius; x++)
        {
            if (x < 0 || x >= Chunk.SizeX)
            {
                continue;
            }

            for (int z = centerZ - radius; z <= centerZ + radius; z++)
            {
                if (z < 0 || z >= Chunk.SizeZ)
                {
                    continue;
                }

                for (int y = minY; y <= maxY; y++)
                {
                    chunk.SetBlock(x, y, z, BlockId.Air);
                }
            }
        }
    }

    private static void SetBlock(Chunk chunk, int x, int y, int z, BlockId block)
    {
        if (x < 0 || x >= Chunk.SizeX || z < 0 || z >= Chunk.SizeZ || y < 0 || y >= Chunk.SizeY)
        {
            return;
        }

        chunk.SetBlock(x, y, z, block);
    }

    private bool RollChance(Chunk chunk, int salt, float chance)
    {
        if (chance <= 0f)
        {
            return false;
        }

        var rand = CreateRandom(chunk, salt);
        return rand.NextDouble() <= chance;
    }

    private Random CreateRandom(Chunk chunk, int salt)
    {
        return new Random(HashSeed(_seed, chunk.ChunkX, chunk.ChunkZ, salt));
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
                if (dx == 0 && dz == 0)
                {
                    continue;
                }

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

    private static bool IsCampBiome(BiomeId biome)
    {
        return biome is BiomeId.Forest or BiomeId.Plains or BiomeId.Taiga or BiomeId.Savanna or BiomeId.Shrubland or BiomeId.Swamp;
    }

    private static bool IsWatchtowerBiome(BiomeId biome)
    {
        return biome is BiomeId.Forest or BiomeId.Plains or BiomeId.Taiga or BiomeId.Savanna or BiomeId.Shrubland or BiomeId.Mountains;
    }

    private static bool IsBuriedCacheBiome(BiomeId biome)
    {
        return biome is BiomeId.Forest or BiomeId.Plains or BiomeId.Taiga or BiomeId.Savanna or BiomeId.Shrubland or BiomeId.Desert;
    }

    private static bool IsCaveCacheBiome(BiomeId biome)
    {
        return biome is BiomeId.Forest or BiomeId.Taiga or BiomeId.Tundra or BiomeId.Swamp or BiomeId.Mountains;
    }

    private static bool IsRuinBiome(BiomeId biome)
    {
        return biome is BiomeId.Forest or BiomeId.Plains or BiomeId.Desert or BiomeId.Mountains or BiomeId.Taiga or BiomeId.Savanna or BiomeId.Shrubland;
    }

    private static bool IsMineShaftBiome(BiomeId biome)
    {
        return biome is BiomeId.Forest or BiomeId.Taiga or BiomeId.Tundra or BiomeId.Swamp or BiomeId.Mountains or BiomeId.Shrubland;
    }

    private static int HashSeed(params int[] values)
    {
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < values.Length; i++)
            {
                hash = (hash * 31) ^ values[i];
            }

            return hash;
        }
    }

    private int GetStructureMargin()
    {
        return Math.Clamp(_settings.StructureMargin, 0, 6);
    }
}
