using MiniCRUFT.Core;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class HerobrineModeCatalogTests
{
    [Fact]
    public void Normalize_InvalidMode_FallsBackToClassic()
    {
        Assert.Equal(HerobrineModeCatalog.Classic, HerobrineModeCatalog.Normalize(null));
        Assert.Equal(HerobrineModeCatalog.Classic, HerobrineModeCatalog.Normalize(string.Empty));
        Assert.Equal(HerobrineModeCatalog.Classic, HerobrineModeCatalog.Normalize("unknown"));
    }

    [Fact]
    public void Next_CyclesThroughModesInOrder()
    {
        Assert.Equal(HerobrineModeCatalog.Stalker, HerobrineModeCatalog.Next(HerobrineModeCatalog.Classic));
        Assert.Equal(HerobrineModeCatalog.Haunt, HerobrineModeCatalog.Next(HerobrineModeCatalog.Stalker));
        Assert.Equal(HerobrineModeCatalog.Nightmare, HerobrineModeCatalog.Next(HerobrineModeCatalog.Haunt));
        Assert.Equal(HerobrineModeCatalog.Classic, HerobrineModeCatalog.Next(HerobrineModeCatalog.Nightmare));
    }
}
