using System.Linq;
using MiniCRUFT.Renderer;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class MobModelCatalogTests
{
    [Fact]
    public void TextureSources_IncludeHerobrineBaseAndEyes()
    {
        var sources = MobModelCatalog.GetTextureSources();

        Assert.Contains(sources, source => source.Name == "herobrine" && source.RelativePath == "minecraft\\mob\\char.png");
        Assert.Contains(sources, source => source.Name == "herobrine_eyes" && source.RelativePath == "minecraft\\mob\\herobrine_eyes.png");
        Assert.Equal(sources.Count, sources.Select(source => source.Name).Distinct().Count());
    }
}
