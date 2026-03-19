using System;
using System.IO;
using MiniCRUFT.Core;
using MiniCRUFT.UI;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class LocalizationTests
{
    [Fact]
    public void Constructor_FallsBackToKeys_WhenJsonIsInvalid()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_localization_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(Path.Combine(root, "broken.json"), "{ invalid json");

            var localization = new Localization(new AssetStore(root), "broken.json");

            Assert.Equal("hud.seed", localization.Get("hud.seed"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
