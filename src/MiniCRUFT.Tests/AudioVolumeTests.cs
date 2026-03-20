using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using MiniCRUFT.Audio;
using MiniCRUFT.Core;
using MiniCRUFT.Game;
using MiniCRUFT.World;
using Xunit;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Tests;

public sealed class AudioVolumeTests
{
    [Fact]
    public void SoundSystem_RespectsMasterVolume()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_sound_master_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            CreateFile(root, "minecraft/sounds/dig/stone.ogg");

            var assets = new AssetStore(root);
            using var backend = new RecordingAudioBackend();
            var config = new AudioConfig
            {
                MasterVolume = 0.5f,
                DigVolume = 0.8f
            };
            using var system = new SoundSystem(assets, backend, config);
            system.SetListener(Vector3.Zero, Vector3.UnitX);

            system.PlayDig(BlockId.Stone, Vector3.Zero);

            Assert.Single(backend.Plays);
            Assert.Equal("stone.ogg", Path.GetFileName(backend.Plays[0].Path));
            Assert.InRange(backend.Plays[0].Volume, 0.39f, 0.41f);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void AmbientSoundSystem_RespectsMasterVolume()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_ambient_master_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            CreateFile(root, "minecraft/sounds/ambient/forest.ogg");

            var assets = new AssetStore(root);
            using var backend = new RecordingAudioBackend();
            var config = new AudioConfig
            {
                MasterVolume = 0.5f,
                AmbientVolume = 0.8f,
                AmbientIntervalSeconds = 0f,
                MusicIntervalSeconds = 999f,
                WeatherIntervalSeconds = 999f,
                LiquidIntervalSeconds = 999f
            };
            using var system = new AmbientSoundSystem(assets, backend, config);

            system.Update(0.01f, CreateFlatWorld(), Vector3.Zero, 0f);

            Assert.Single(backend.Plays);
            Assert.Equal("forest.ogg", Path.GetFileName(backend.Plays[0].Path));
            Assert.InRange(backend.Plays[0].Volume, 0.39f, 0.41f);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void MobSoundSystem_RespectsMasterVolume()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_mob_master_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            CreateFile(root, "minecraft/sounds/mob/cow/step1.ogg");

            var assets = new AssetStore(root);
            using var backend = new RecordingAudioBackend();
            var config = new AudioConfig
            {
                MasterVolume = 0.5f,
                MobStepVolume = 0.8f
            };
            using var system = new MobSoundSystem(assets, backend, config);
            system.SetListener(Vector3.Zero, Vector3.UnitX);

            system.Handle(new MobEvent(MobEventKind.Step, MobType.Cow, Vector3.Zero));

            Assert.Single(backend.Plays);
            Assert.Equal("step1.ogg", Path.GetFileName(backend.Plays[0].Path));
            Assert.InRange(backend.Plays[0].Volume, 0.39f, 0.41f);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static WorldType CreateFlatWorld()
    {
        var world = new WorldType(1337, new WorldGenSettings());
        var chunk = new Chunk(0, 0);

        for (int x = 0; x < Chunk.SizeX; x++)
        {
            for (int z = 0; z < Chunk.SizeZ; z++)
            {
                chunk.SetBlock(x, 64, z, BlockId.Stone);
            }
        }

        world.SetChunk(chunk);
        return world;
    }

    private static void CreateFile(string root, string relativePath)
    {
        string fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllBytes(fullPath, Array.Empty<byte>());
    }

    private sealed class RecordingAudioBackend : IAudioBackend
    {
        public bool IsAvailable => true;
        public List<PlayCall> Plays { get; } = new();

        public void Play(string path, float volume, float pan = 0f)
        {
            Plays.Add(new PlayCall(path, volume, pan));
        }

        public void Dispose()
        {
        }
    }

    private readonly record struct PlayCall(string Path, float Volume, float Pan);
}
