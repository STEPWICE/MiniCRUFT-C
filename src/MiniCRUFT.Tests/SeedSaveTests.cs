using System;
using System.IO;
using MiniCRUFT.IO;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class SeedSaveTests
{
    [Fact]
    public void LoadSeed_ReadsLegacyBinaryInt32Format()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_seed_legacy_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            string path = Path.Combine(root, "seed.dat");
            using (var writer = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write)))
            {
                writer.Write(13371337);
            }

            int loaded = WorldSave.LoadSeed(root, -1);

            Assert.Equal(13371337, loaded);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
