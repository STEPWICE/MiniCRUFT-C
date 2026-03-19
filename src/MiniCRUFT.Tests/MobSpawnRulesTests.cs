using MiniCRUFT.Core;
using MiniCRUFT.Game;
using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class MobSpawnRulesTests
{
    [Fact]
    public void GetWeight_PrefersHostileMobsInShelter()
    {
        var config = new MobConfig();
        var zombie = MobCatalog.Get(MobType.Zombie);

        float skyWeight = MobSpawnRules.GetWeight(zombie, config, BiomeId.Plains, sunIntensity: 1f, rainIntensity: 0f, submerged: false, skyExposed: true);
        float shelterWeight = MobSpawnRules.GetWeight(zombie, config, BiomeId.Plains, sunIntensity: 1f, rainIntensity: 0f, submerged: false, skyExposed: false);

        Assert.True(shelterWeight > skyWeight);
    }

    [Fact]
    public void GetWeight_PrefersPassiveMobsInOpenAir()
    {
        var config = new MobConfig();
        var sheep = MobCatalog.Get(MobType.Sheep);

        float skyWeight = MobSpawnRules.GetWeight(sheep, config, BiomeId.Plains, sunIntensity: 1f, rainIntensity: 0f, submerged: false, skyExposed: true);
        float shelterWeight = MobSpawnRules.GetWeight(sheep, config, BiomeId.Plains, sunIntensity: 1f, rainIntensity: 0f, submerged: false, skyExposed: false);

        Assert.True(skyWeight > shelterWeight);
    }

    [Fact]
    public void GetWeight_DropsToZero_WhenBiomeIsUnsupported()
    {
        var config = new MobConfig();
        var cow = MobCatalog.Get(MobType.Cow);

        float weight = MobSpawnRules.GetWeight(cow, config, BiomeId.River, sunIntensity: 0.8f, rainIntensity: 0f, submerged: false, skyExposed: true);

        Assert.Equal(0f, weight);
    }
}
