using MiniCRUFT.IO;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class HungerSaveTests
{
    [Fact]
    public void HungerSave_RoundTrip_PreservesValues()
    {
        string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"minicruft_hunger_{System.Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(root);

        try
        {
            var data = new HungerSaveData(7.5f, 3.25f);

            WorldSave.SaveHunger(root, data);
            var loaded = WorldSave.LoadHunger(root, new HungerSaveData(1f, 0f));

            Assert.Equal(data.Hunger, loaded.Hunger, 4);
            Assert.Equal(data.StarvationTimer, loaded.StarvationTimer, 4);
        }
        finally
        {
            System.IO.Directory.Delete(root, true);
        }
    }
}
