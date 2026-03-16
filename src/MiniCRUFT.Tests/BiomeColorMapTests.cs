using System.IO;
using MiniCRUFT.Core;
using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class BiomeColorMapTests
{
    [Fact]
    public void BiomeColorMap_ReturnsStableColors()
    {
        string assetsPath = FindAssetsPath();
        var assets = new AssetStore(assetsPath);
        var map = BiomeColorMap.Load(assets);

        BiomeRegistry.Initialize();
        var forest = BiomeRegistry.Get(BiomeId.Forest);

        var grass1 = map.GetGrassColor(forest);
        var grass2 = map.GetGrassColor(forest);

        Assert.Equal(grass1, grass2);
        Assert.InRange(grass1.X, 0f, 1f);
        Assert.InRange(grass1.Y, 0f, 1f);
        Assert.InRange(grass1.Z, 0f, 1f);
    }

    private static string FindAssetsPath()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "assets");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Assets folder not found.");
    }
}
