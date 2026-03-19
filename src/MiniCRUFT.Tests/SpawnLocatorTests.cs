using System.Collections.Generic;
using MiniCRUFT.Core;
using MiniCRUFT.Game;
using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class SpawnLocatorTests
{
    [Fact]
    public void TryFindSpawn_IsDeterministicWhenRandomizeIsDisabled()
    {
        var settings = new WorldGenSettings
        {
            ForcedBiome = BiomeId.Plains
        };

        var config = new SpawnConfig
        {
            Randomize = false,
            SearchRadius = 128,
            MaxAttempts = 256,
            MaxSlope = 100f,
            MinHeightAboveSea = 1
        };

        bool foundFirst = SpawnLocator.TryFindSpawn(1337, settings, config, out var spawnFirst, out var biomeFirst);
        bool foundSecond = SpawnLocator.TryFindSpawn(1337, settings, config, out var spawnSecond, out var biomeSecond);

        Assert.True(foundFirst);
        Assert.True(foundSecond);
        Assert.Equal(spawnFirst, spawnSecond);
        Assert.Equal(biomeFirst, biomeSecond);
        Assert.Equal(BiomeId.Plains, biomeFirst);
    }

    [Fact]
    public void TryFindSpawn_RespectsExcludedBiomes()
    {
        var settings = new WorldGenSettings
        {
            ForcedBiome = BiomeId.Plains
        };

        var config = new SpawnConfig
        {
            Randomize = false,
            SearchRadius = 128,
            MaxAttempts = 64,
            MaxSlope = 100f,
            MinHeightAboveSea = 1,
            ExcludedBiomes = new List<string> { "NotABiome", "Plains" }
        };

        bool found = SpawnLocator.TryFindSpawn(1337, settings, config, out _, out _);

        Assert.False(found);
    }
}
