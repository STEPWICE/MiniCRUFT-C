using System;
using System.IO;
using System.Numerics;
using System.Linq;
using MiniCRUFT.Core;
using MiniCRUFT.IO;
using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class WorldSaveSmokeTests
{
    [Fact]
    public void WorldSave_SmokeRoundTrip_PreservesSeedPlayerAndChunk()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_worldsave_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            BlockRegistry.Initialize();
            BiomeRegistry.Initialize();

            const int seed = 2468;
            WorldSave.SaveSeed(root, seed);

            var player = new PlayerSaveData(
                new Vector3(4.5f, 72f, -9.25f),
                new[]
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
                },
                new[]
                {
                    1,
                    2,
                    3,
                    4,
                    5,
                    6,
                    7,
                    8,
                    9,
                    10
                },
                new[]
                {
                    31,
                    -1,
                    -1,
                    -1,
                    -1,
                    -1,
                    -1,
                    -1,
                    -1,
                    -1
                },
                0);

            WorldSave.SavePlayer(root, player);

            var chunk = new Chunk(1, -2);
            chunk.SetBlock(1, 1, 1, BlockId.Stone);
            chunk.SetBlock(2, 2, 2, BlockId.Dirt);
            chunk.SetBiome(3, 4, BiomeId.Forest);

            var storage = new FileChunkStorage(root);
            storage.SaveChunk(chunk);

            int loadedSeed = WorldSave.LoadSeed(root, -1);
            var loadedPlayer = WorldSave.LoadPlayer(root, player);
            var loadedChunk = storage.LoadChunk(new ChunkCoord(1, -2));

            Assert.Equal(seed, loadedSeed);
            Assert.Equal(player.Position, loadedPlayer.Position);
            Assert.Equal(player.SelectedIndex, loadedPlayer.SelectedIndex);
            Assert.True(player.Hotbar.SequenceEqual(loadedPlayer.Hotbar));
            Assert.True(player.Counts.SequenceEqual(loadedPlayer.Counts));
            Assert.True(player.ToolDurability!.SequenceEqual(loadedPlayer.ToolDurability!));
            Assert.NotNull(loadedChunk);
            Assert.Equal(BlockId.Stone, loadedChunk!.GetBlock(1, 1, 1));
            Assert.Equal(BlockId.Dirt, loadedChunk.GetBlock(2, 2, 2));
            Assert.Equal(BiomeId.Forest, loadedChunk.GetBiome(3, 4));
            Assert.False(loadedChunk.SaveDirty);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void WorldSave_SmokeRoundTrip_PreservesMobs()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_worldsave_mobs_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var mobs = new[]
            {
                new MobSaveData(
                    MobType.Zombie,
                    new Vector3(12.5f, 64f, -8.25f),
                    new Vector3(0.15f, 0.2f, -0.05f),
                    new Vector3(10f, 64f, -8f),
                    90f,
                    1.1f,
                    17,
                    0.5f,
                    1.25f,
                    0.75f,
                    0.3f,
                0.2f,
                true,
                true,
                0.6f,
                42.5f,
                elite: true,
                eliteVariant: EliteMobVariant.Warden),
                new MobSaveData(
                    MobType.Cow,
                    new Vector3(-4.5f, 71f, 6.75f),
                    new Vector3(-0.05f, 0f, 0.12f),
                    new Vector3(-4f, 71f, 6f),
                    180f,
                    2.2f,
                    8,
                    0.1f,
                    2.5f,
                    1.5f,
                    0.2f,
                    0.0f,
                    false,
                    false,
                    0.4f,
                    9.5f,
                    elite: false)
            };

            WorldSave.SaveMobs(root, mobs);
            var loaded = WorldSave.LoadMobs(root);

            Assert.Equal(2, loaded.Count);

            Assert.Equal(mobs[0].Type, loaded[0].Type);
            Assert.Equal(mobs[0].Position, loaded[0].Position);
            Assert.Equal(mobs[0].Velocity, loaded[0].Velocity);
            Assert.Equal(mobs[0].Yaw, loaded[0].Yaw);
            Assert.Equal(mobs[0].Health, loaded[0].Health);
            Assert.Equal(mobs[0].AttackCooldown, loaded[0].AttackCooldown);
            Assert.Equal(mobs[0].WanderTimer, loaded[0].WanderTimer);
            Assert.Equal(mobs[0].IdleTimer, loaded[0].IdleTimer);
            Assert.Equal(mobs[0].HurtTimer, loaded[0].HurtTimer);
            Assert.Equal(mobs[0].SpecialTimer, loaded[0].SpecialTimer);
            Assert.Equal(mobs[0].SpecialActive, loaded[0].SpecialActive);
            Assert.Equal(mobs[0].StepDistance, loaded[0].StepDistance);
            Assert.Equal(mobs[0].WanderAngle, loaded[0].WanderAngle);
            Assert.Equal(mobs[0].OnGround, loaded[0].OnGround);
            Assert.Equal(mobs[0].HomePosition, loaded[0].HomePosition);
            Assert.Equal(mobs[0].Age, loaded[0].Age);
            Assert.Equal(mobs[0].Elite, loaded[0].Elite);
            Assert.Equal(mobs[0].EliteVariant, loaded[0].EliteVariant);

            Assert.Equal(mobs[1].Type, loaded[1].Type);
            Assert.Equal(mobs[1].Position, loaded[1].Position);
            Assert.Equal(mobs[1].Velocity, loaded[1].Velocity);
            Assert.Equal(mobs[1].Yaw, loaded[1].Yaw);
            Assert.Equal(mobs[1].Health, loaded[1].Health);
            Assert.Equal(mobs[1].AttackCooldown, loaded[1].AttackCooldown);
            Assert.Equal(mobs[1].WanderTimer, loaded[1].WanderTimer);
            Assert.Equal(mobs[1].IdleTimer, loaded[1].IdleTimer);
            Assert.Equal(mobs[1].HurtTimer, loaded[1].HurtTimer);
            Assert.Equal(mobs[1].SpecialTimer, loaded[1].SpecialTimer);
            Assert.Equal(mobs[1].SpecialActive, loaded[1].SpecialActive);
            Assert.Equal(mobs[1].StepDistance, loaded[1].StepDistance);
            Assert.Equal(mobs[1].WanderAngle, loaded[1].WanderAngle);
            Assert.Equal(mobs[1].OnGround, loaded[1].OnGround);
            Assert.Equal(mobs[1].HomePosition, loaded[1].HomePosition);
            Assert.Equal(mobs[1].Age, loaded[1].Age);
            Assert.Equal(mobs[1].Elite, loaded[1].Elite);
            Assert.Equal(mobs[1].EliteVariant, loaded[1].EliteVariant);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void WorldSave_SmokeRoundTrip_PreservesTnt()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_worldsave_tnt_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tnts = new[]
            {
                new TntSaveData(new BlockCoord(12, 64, -8), 3.25f, 4f),
                new TntSaveData(new BlockCoord(-4, 70, 6), 1.5f, 0.35f)
            };

            WorldSave.SaveTnt(root, tnts);
            var loaded = WorldSave.LoadTnt(root);

            Assert.Equal(2, loaded.Count);
            Assert.Equal(tnts[0].Position, loaded[0].Position);
            Assert.Equal(tnts[0].FuseRemaining, loaded[0].FuseRemaining);
            Assert.Equal(tnts[0].FuseDuration, loaded[0].FuseDuration);
            Assert.Equal(tnts[1].Position, loaded[1].Position);
            Assert.Equal(tnts[1].FuseRemaining, loaded[1].FuseRemaining);
            Assert.Equal(tnts[1].FuseDuration, loaded[1].FuseDuration);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void WorldSave_LoadMobs_V1FormatDefaultsEliteToFalse()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_worldsave_mobsv1_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            string path = Path.Combine(root, "mobs.dat");
            using (var writer = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write)))
            {
                writer.Write(new byte[] { (byte)'M', (byte)'C', (byte)'M', (byte)'B' });
                writer.Write(1);
                writer.Write(1);
                writer.Write((byte)MobType.Zombie);
                writer.Write(1.5f);
                writer.Write(64f);
                writer.Write(-2.5f);
                writer.Write(0.25f);
                writer.Write(0.5f);
                writer.Write(-0.1f);
                writer.Write(1f);
                writer.Write(64f);
                writer.Write(-3f);
                writer.Write(90f);
                writer.Write(1.2f);
                writer.Write(17);
                writer.Write(0.4f);
                writer.Write(0.75f);
                writer.Write(0.35f);
                writer.Write(0.12f);
                writer.Write(0.08f);
                writer.Write(true);
                writer.Write(false);
                writer.Write(0.6f);
                writer.Write(12.5f);
            }

            var loaded = WorldSave.LoadMobs(root);

            Assert.Single(loaded);
            Assert.Equal(MobType.Zombie, loaded[0].Type);
            Assert.False(loaded[0].Elite);
            Assert.Equal(17, loaded[0].Health);
            Assert.Equal(EliteMobVariant.None, loaded[0].EliteVariant);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void WorldSave_LoadMobs_V2FormatDefaultsEliteVariantToNone()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_worldsave_mobsv2_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            string path = Path.Combine(root, "mobs.dat");
            using (var writer = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write)))
            {
                writer.Write(new byte[] { (byte)'M', (byte)'C', (byte)'M', (byte)'B' });
                writer.Write(2);
                writer.Write(1);
                writer.Write((byte)MobType.Zombie);
                writer.Write(1.5f);
                writer.Write(64f);
                writer.Write(-2.5f);
                writer.Write(0.25f);
                writer.Write(0.5f);
                writer.Write(-0.1f);
                writer.Write(1f);
                writer.Write(64f);
                writer.Write(-3f);
                writer.Write(90f);
                writer.Write(1.2f);
                writer.Write(17);
                writer.Write(0.4f);
                writer.Write(0.75f);
                writer.Write(0.35f);
                writer.Write(0.12f);
                writer.Write(0.08f);
                writer.Write(true);
                writer.Write(false);
                writer.Write(0.6f);
                writer.Write(12.5f);
                writer.Write(true);
            }

            var loaded = WorldSave.LoadMobs(root);

            Assert.Single(loaded);
            Assert.Equal(MobType.Zombie, loaded[0].Type);
            Assert.True(loaded[0].Elite);
            Assert.Equal(EliteMobVariant.None, loaded[0].EliteVariant);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void WorldSave_SmokeRoundTrip_PreservesHerobrineState()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_worldsave_herobrine_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var state = new HerobrineSaveData(
                1337,
                new Vector3(18.5f, 66f, -12.25f),
                new Vector3(20f, 64.5f, -9f),
                0.75f,
                14f,
                9.5f,
                33f,
                2.25f,
                4,
                true);

            WorldSave.SaveHerobrine(root, state);
            HerobrineSaveData loaded = WorldSave.LoadHerobrine(root, default);

            Assert.Equal(state.Seed, loaded.Seed);
            Assert.Equal(state.LastManifestPosition, loaded.LastManifestPosition);
            Assert.Equal(state.LastObservedPlayerPosition, loaded.LastObservedPlayerPosition);
            Assert.Equal(state.HauntPressure, loaded.HauntPressure);
            Assert.Equal(state.ManifestCooldown, loaded.ManifestCooldown);
            Assert.Equal(state.EventCooldown, loaded.EventCooldown);
            Assert.Equal(state.WorldEffectCooldown, loaded.WorldEffectCooldown);
            Assert.Equal(state.ActiveTimer, loaded.ActiveTimer);
            Assert.Equal(state.EncounterCount, loaded.EncounterCount);
            Assert.Equal(state.IsManifested, loaded.IsManifested);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void WorldSave_LoadHerobrine_ReturnsFallback_WhenStateFileIsMissing()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_worldsave_herobrine_missing_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var fallback = new HerobrineSaveData(
                404,
                new Vector3(1f, 2f, 3f),
                new Vector3(4f, 5f, 6f),
                0.2f,
                7f,
                8f,
                9f,
                1.5f,
                2,
                false);

            HerobrineSaveData loaded = WorldSave.LoadHerobrine(root, fallback);

            Assert.Equal(fallback.Seed, loaded.Seed);
            Assert.Equal(fallback.LastManifestPosition, loaded.LastManifestPosition);
            Assert.Equal(fallback.LastObservedPlayerPosition, loaded.LastObservedPlayerPosition);
            Assert.Equal(fallback.HauntPressure, loaded.HauntPressure);
            Assert.Equal(fallback.ManifestCooldown, loaded.ManifestCooldown);
            Assert.Equal(fallback.EventCooldown, loaded.EventCooldown);
            Assert.Equal(fallback.WorldEffectCooldown, loaded.WorldEffectCooldown);
            Assert.Equal(fallback.ActiveTimer, loaded.ActiveTimer);
            Assert.Equal(fallback.EncounterCount, loaded.EncounterCount);
            Assert.Equal(fallback.IsManifested, loaded.IsManifested);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
