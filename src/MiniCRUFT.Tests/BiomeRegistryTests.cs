using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class BiomeRegistryTests
{
    [Theory]
    [InlineData(0.99f, 0.95f, BiomeId.Forest)]
    [InlineData(0.99f, 0.50f, BiomeId.Forest)]
    [InlineData(0.99f, 0.30f, BiomeId.Plains)]
    [InlineData(0.96f, 0.10f, BiomeId.Desert)]
    [InlineData(0.65f, 0.10f, BiomeId.Savanna)]
    [InlineData(0.65f, 0.25f, BiomeId.Shrubland)]
    [InlineData(0.30f, 0.30f, BiomeId.Taiga)]
    [InlineData(0.30f, 0.10f, BiomeId.Tundra)]
    [InlineData(0.30f, 0.70f, BiomeId.Swamp)]
    [InlineData(0.80f, 0.70f, BiomeId.Forest)]
    [InlineData(0.60f, 0.70f, BiomeId.Swamp)]
    public void PickStrictBeta_UsesClimateThresholds(float temperature, float humidity, BiomeId expected)
    {
        BiomeRegistry.Initialize();

        BiomeId actual = BiomeRegistry.PickStrictBeta(temperature, humidity);

        Assert.Equal(expected, actual);
    }
}
