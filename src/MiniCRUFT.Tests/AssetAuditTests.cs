using System;
using System.IO;
using MiniCRUFT.Core;
using MiniCRUFT.Game;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class AssetAuditTests
{
    [Fact]
    public void StrictBetaAssetAudit_PassesForRepositoryAssets()
    {
        string assetsPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "assets"));
        var assets = new AssetStore(assetsPath);
        var audit = new AssetAudit(assets);

        var result = audit.Run(strictBetaMode: true);

        Assert.NotNull(result);
        Assert.Empty(result.MissingTextures);
        Assert.Empty(result.PlaceholderTextures);
        Assert.Empty(result.MobModelIssues);
        Assert.True(result.BlockTextureCount > 0);
        Assert.True(result.WaterTextureCount > 0);
        Assert.True(result.HudTextureCount > 0);
        Assert.True(result.ColormapCount >= 2);
        Assert.True(result.FontCount > 0);
    }
}
