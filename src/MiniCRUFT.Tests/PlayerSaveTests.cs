using System;
using System.IO;
using System.Numerics;
using MiniCRUFT.IO;
using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class PlayerSaveTests
{
    [Fact]
    public void PlayerSave_RoundTrip_PreservesHotbar()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_player_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var hotbar = new[]
            {
                BlockId.Grass,
                BlockId.Dirt,
                BlockId.Stone,
                BlockId.Wood,
                BlockId.Planks,
                BlockId.Glass,
                BlockId.Sand,
                BlockId.Torch,
                BlockId.Water
            };
            var data = new PlayerSaveData(new Vector3(1f, 2f, 3f), hotbar, 3);

            WorldSave.SavePlayer(root, data);
            var loaded = WorldSave.LoadPlayer(root, data);

            Assert.Equal(data.Position, loaded.Position);
            Assert.Equal(data.SelectedIndex, loaded.SelectedIndex);
            Assert.Equal(data.Hotbar.Length, loaded.Hotbar.Length);
            for (int i = 0; i < data.Hotbar.Length; i++)
            {
                Assert.Equal(data.Hotbar[i], loaded.Hotbar[i]);
            }
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
