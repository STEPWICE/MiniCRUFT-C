using System;
using System.IO;
using MiniCRUFT.IO;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class WorldResetTests
{
    [Fact]
    public void TryReset_RecreatesWorldDirectory_AndWritesSeed()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_worldreset_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(Path.Combine(root, "leftover.txt"), "old");

            bool reset = WorldReset.TryReset(root, 424242, out var error);

            Assert.True(reset);
            Assert.Equal(string.Empty, error);
            Assert.True(Directory.Exists(root));
            Assert.False(File.Exists(Path.Combine(root, "leftover.txt")));
            Assert.Equal(424242, WorldSave.LoadSeed(root, -1));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }
}
