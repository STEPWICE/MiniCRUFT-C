using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Numerics;
using MiniCRUFT.Audio;
using MiniCRUFT.Core;
using MiniCRUFT.Game;
using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class MobSoundSystemTests
{
    [Fact]
    public void PlaysCowStepSound_FromCowStepBank()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_cowsteps_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            CreateFile(root, "minecraft/sounds/mob/cow/step1.ogg");

            var assets = new AssetStore(root);
            using var backend = new RecordingAudioBackend();
            using var system = new MobSoundSystem(assets, backend, new AudioConfig());
            system.SetListener(Vector3.Zero, Vector3.UnitX);

            system.Handle(new MobEvent(MobEventKind.Step, MobType.Cow, Vector3.Zero));

            Assert.Single(backend.Plays);
            Assert.Equal("step1.ogg", Path.GetFileName(backend.Plays[0].Path));
            Assert.Contains($"{Path.DirectorySeparatorChar}cow{Path.DirectorySeparatorChar}", backend.Plays[0].Path);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ZombieStep_DoesNotPickWoodBreak()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_zombiestep_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            CreateFile(root, "minecraft/sounds/mob/zombie/woodbreak.ogg");

            var assets = new AssetStore(root);
            using var backend = new RecordingAudioBackend();
            using var system = new MobSoundSystem(assets, backend, new AudioConfig());
            system.SetListener(Vector3.Zero, Vector3.UnitX);

            system.Handle(new MobEvent(MobEventKind.Step, MobType.Zombie, Vector3.Zero));

            Assert.Empty(backend.Plays);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Creeper_UsesFuseAndExplosionSounds()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_creepersounds_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            CreateFile(root, "minecraft/sounds/random/fuse.ogg");
            CreateFile(root, "minecraft/sounds/random/explode1.ogg");

            var assets = new AssetStore(root);
            using var backend = new RecordingAudioBackend();
            using var system = new MobSoundSystem(assets, backend, new AudioConfig());
            system.SetListener(Vector3.Zero, Vector3.UnitX);

            system.Handle(new MobEvent(MobEventKind.Attack, MobType.Creeper, Vector3.Zero));
            system.Handle(new MobEvent(MobEventKind.Explosion, MobType.Creeper, Vector3.Zero));

            Assert.Equal(2, backend.Plays.Count);
            Assert.Equal("fuse.ogg", Path.GetFileName(backend.Plays[0].Path));
            Assert.Equal("explode1.ogg", Path.GetFileName(backend.Plays[1].Path));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void LoadsSounds_FromMobDirectory()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_mobsounds_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            string mobRoot = Path.Combine(root, "minecraft", "sounds", "mob");
            Directory.CreateDirectory(mobRoot);
            File.WriteAllBytes(Path.Combine(mobRoot, "chicken1.ogg"), Array.Empty<byte>());
            File.WriteAllBytes(Path.Combine(mobRoot, "zombie1.ogg"), Array.Empty<byte>());

            var assets = new AssetStore(root);
            using var backend = new NoAudioBackend();
            using var system = new MobSoundSystem(assets, backend, new AudioConfig());

            Assert.True(GetMobSoundCount(system, MobType.Chicken) > 0);
            Assert.True(GetMobSoundCount(system, MobType.Zombie) > 0);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static void CreateFile(string root, string relativePath)
    {
        string fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllBytes(fullPath, Array.Empty<byte>());
    }

    private static int GetMobSoundCount(MobSoundSystem system, MobType type)
    {
        var field = typeof(MobSoundSystem).GetField("_sounds", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException("MobSoundSystem._sounds field was not found.");
        }

        var dictionary = field.GetValue(system) as IEnumerable;
        if (dictionary == null)
        {
            throw new InvalidOperationException("MobSoundSystem._sounds could not be read.");
        }

        foreach (var entry in dictionary)
        {
            var entryType = entry.GetType();
            var key = (MobType)entryType.GetProperty("Key")!.GetValue(entry)!;
            if (key != type)
            {
                continue;
            }

            object sounds = entryType.GetProperty("Value")!.GetValue(entry)!;
            return (int)sounds.GetType().GetProperty("Count")!.GetValue(sounds)!;
        }

        throw new InvalidOperationException($"Mob sounds for {type} were not loaded.");
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
