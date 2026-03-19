using System;
using System.IO;
using System.Numerics;
using System.Linq;
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
                BlockId.WoodenPickaxe,
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
            var counts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var durability = new[] { 37, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
            var data = new PlayerSaveData(new Vector3(1f, 2f, 3f), hotbar, counts, durability, 0);

            WorldSave.SavePlayer(root, data);
            var loaded = WorldSave.LoadPlayer(root, data);

            Assert.Equal(data.Position, loaded.Position);
            Assert.Equal(data.SelectedIndex, loaded.SelectedIndex);
            Assert.Equal(data.Hotbar.Length, loaded.Hotbar.Length);
            for (int i = 0; i < data.Hotbar.Length; i++)
            {
                Assert.Equal(data.Hotbar[i], loaded.Hotbar[i]);
                Assert.Equal(data.Counts[i], loaded.Counts[i]);
            }
            Assert.True(loaded.ToolDurability != null);
            Assert.True(durability.SequenceEqual(loaded.ToolDurability!));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void LoadPlayer_ReadsVersion2Format_AndAppliesDefaultCounts()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_player_v2_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            string path = Path.Combine(root, "player.dat");
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
                BlockId.Tnt
            };

            using (var writer = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write)))
            {
                writer.Write((byte)'M');
                writer.Write((byte)'C');
                writer.Write((byte)'P');
                writer.Write((byte)'S');
                writer.Write(2);
                writer.Write(12.5f);
                writer.Write(64f);
                writer.Write(-8.25f);
                writer.Write(hotbar.Length);
                for (int i = 0; i < hotbar.Length; i++)
                {
                    writer.Write((byte)hotbar[i]);
                }
                writer.Write(4);
            }

            var fallback = new PlayerSaveData(
                new Vector3(1f, 2f, 3f),
                new[] { BlockId.Stone, BlockId.Dirt, BlockId.Grass },
                2);

            var loaded = WorldSave.LoadPlayer(root, fallback);

            Assert.Equal(new Vector3(12.5f, 64f, -8.25f), loaded.Position);
            Assert.Equal(4, loaded.SelectedIndex);
            Assert.True(loaded.Hotbar.SequenceEqual(hotbar));
            Assert.True(loaded.Counts.SequenceEqual(new[]
            {
                1,
                1,
                1,
                1,
                13,
                1,
                1,
                16,
                4
            }));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void LoadPlayer_ReadsLegacyPositionOnlyFormat()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_player_legacy_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            string path = Path.Combine(root, "player.dat");
            using (var writer = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write)))
            {
                writer.Write(12.5f);
                writer.Write(64f);
                writer.Write(-8.25f);
            }

            var fallback = new PlayerSaveData(
                new Vector3(1f, 2f, 3f),
                new[] { BlockId.Stone, BlockId.Dirt, BlockId.Grass },
                2);

            var loaded = WorldSave.LoadPlayer(root, fallback);

            Assert.Equal(new Vector3(12.5f, 64f, -8.25f), loaded.Position);
            Assert.Equal(0, loaded.SelectedIndex);
            Assert.True(loaded.Hotbar.SequenceEqual(new[]
            {
                BlockId.Grass,
                BlockId.Dirt,
                BlockId.Stone,
                BlockId.Wood,
                BlockId.Planks,
                BlockId.Glass,
                BlockId.Sand,
                BlockId.Torch,
                BlockId.Tnt
            }));
            Assert.True(loaded.Counts.SequenceEqual(new[]
            {
                1,
                1,
                1,
                1,
                13,
                1,
                1,
                16,
                4
            }));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void LoadPlayer_ReadsVersion3Format_AndLeavesToolDurabilityMissing()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_player_v3_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            string path = Path.Combine(root, "player.dat");
            var hotbar = new[]
            {
                BlockId.WoodenPickaxe,
                BlockId.Stone,
                BlockId.Dirt
            };
            var counts = new[] { 1, 4, 7 };

            using (var writer = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write)))
            {
                writer.Write((byte)'M');
                writer.Write((byte)'C');
                writer.Write((byte)'P');
                writer.Write((byte)'S');
                writer.Write(3);
                writer.Write(8.5f);
                writer.Write(70f);
                writer.Write(-2.25f);
                writer.Write(hotbar.Length);
                for (int i = 0; i < hotbar.Length; i++)
                {
                    writer.Write((byte)hotbar[i]);
                }
                for (int i = 0; i < counts.Length; i++)
                {
                    writer.Write(counts[i]);
                }
                writer.Write(2);
            }

            var fallback = new PlayerSaveData(
                new Vector3(1f, 2f, 3f),
                new[] { BlockId.Stone, BlockId.Dirt, BlockId.Grass },
                2);

            var loaded = WorldSave.LoadPlayer(root, fallback);

            Assert.Equal(new Vector3(8.5f, 70f, -2.25f), loaded.Position);
            Assert.Equal(2, loaded.SelectedIndex);
            Assert.True(loaded.Hotbar.SequenceEqual(hotbar));
            Assert.True(loaded.Counts.SequenceEqual(counts));
            Assert.Null(loaded.ToolDurability);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
