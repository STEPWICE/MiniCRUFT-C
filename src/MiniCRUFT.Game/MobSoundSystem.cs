using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using MiniCRUFT.Audio;
using MiniCRUFT.Core;

namespace MiniCRUFT.Game;

public sealed class MobSoundSystem : IDisposable
{
    private readonly Dictionary<MobType, MobSounds> _sounds = new();
    private readonly Random _random = new();
    private readonly IAudioBackend _backend;
    private readonly AudioConfig _config;
    private readonly bool _enabled;
    private Vector3 _listenerPosition;
    private Vector3 _listenerRight = Vector3.UnitX;

    public MobSoundSystem(AssetStore assets, IAudioBackend backend, AudioConfig config)
    {
        _backend = backend;
        _config = config;

        try
        {
            _sounds[MobType.Zombie] = LoadZombie(assets);
            _sounds[MobType.Creeper] = LoadCreeper(assets);
            _sounds[MobType.Cow] = LoadCow(assets);
            _sounds[MobType.Sheep] = LoadSheep(assets);
            _sounds[MobType.Chicken] = LoadChicken(assets);

            _enabled = _backend.IsAvailable && HasAnySounds();
            Log.Info(
                $"MobSoundSystem: zombie={CountSounds(_sounds[MobType.Zombie])}, " +
                $"creeper={CountSounds(_sounds[MobType.Creeper])}, " +
                $"cow={CountSounds(_sounds[MobType.Cow])}, " +
                $"sheep={CountSounds(_sounds[MobType.Sheep])}, " +
                $"chicken={CountSounds(_sounds[MobType.Chicken])}");
        }
        catch (Exception ex)
        {
            Log.Warn($"MobSoundSystem disabled: {ex.Message}");
            _enabled = false;
        }
    }

    public void Handle(MobEvent mobEvent)
    {
        if (!_enabled || !_sounds.TryGetValue(mobEvent.Type, out var sounds))
        {
            return;
        }

        var list = SelectSounds(sounds, mobEvent.Kind);
        if (list is null || list.Count == 0)
        {
            return;
        }

        float baseVolume = mobEvent.Kind switch
        {
            MobEventKind.Step => _config.MobStepVolume,
            MobEventKind.Attack when mobEvent.Type == MobType.Creeper => _config.FuseVolume,
            MobEventKind.Explosion when mobEvent.Type == MobType.Creeper => _config.ExplosionVolume,
            _ => _config.MobVolume
        };
        float volume = Math.Clamp(baseVolume * _config.MasterVolume * Math.Clamp(mobEvent.Intensity, 0.2f, 2f), 0f, 1f);
        PlayRandom(list, volume, mobEvent.Position);
    }

    public void SetListener(Vector3 position, Vector3 rightVector)
    {
        _listenerPosition = position;
        _listenerRight = rightVector.LengthSquared() > float.Epsilon
            ? Vector3.Normalize(rightVector)
            : Vector3.UnitX;
    }

    public void Dispose()
    {
        // shared audio backend is disposed by GameApp
    }

    private bool HasAnySounds()
    {
        foreach (var sounds in _sounds.Values)
        {
            if (sounds.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    private void PlayRandom(List<string> sounds, float volume, Vector3 sourcePosition)
    {
        if (sounds.Count == 0)
        {
            return;
        }

        if (volume <= 0f)
        {
            return;
        }

        string path = sounds[_random.Next(sounds.Count)];
        try
        {
            var mix = SpatialAudio.Calculate(_listenerPosition, sourcePosition, _listenerRight, _config, volume);
            if (mix.Volume <= 0f)
            {
                return;
            }

            _backend.Play(path, mix.Volume, mix.Pan);
        }
        catch (Exception ex)
        {
            Log.Warn($"MobSoundSystem: failed to play '{path}': {ex.Message}");
        }
    }

    private static List<string>? SelectSounds(MobSounds sounds, MobEventKind kind)
    {
        return kind switch
        {
            MobEventKind.Step => FirstNonEmpty(sounds.Step, sounds.Ambient, sounds.Hurt, sounds.Death),
            MobEventKind.Hurt => FirstNonEmpty(sounds.Hurt, sounds.Attack, sounds.Ambient, sounds.Death),
            MobEventKind.Death => FirstNonEmpty(sounds.Death, sounds.Hurt, sounds.Ambient),
            MobEventKind.Attack => FirstNonEmpty(sounds.Attack, sounds.Ambient, sounds.Hurt),
            MobEventKind.Explosion => FirstNonEmpty(sounds.Explosion, sounds.Death, sounds.Hurt, sounds.Ambient),
            _ => FirstNonEmpty(sounds.Ambient, sounds.Hurt, sounds.Death)
        };
    }

    private static List<string>? FirstNonEmpty(params List<string>[] lists)
    {
        for (int i = 0; i < lists.Length; i++)
        {
            if (lists[i].Count > 0)
            {
                return lists[i];
            }
        }

        return null;
    }

    private static int CountSounds(MobSounds sounds)
    {
        return sounds.Count;
    }

    private static MobSounds LoadZombie(AssetStore assets)
    {
        var sounds = new MobSounds();
        LoadMobFiles(assets, "zombie", sounds.Ambient, name =>
            IsNumberedVariant(name, "zombie") ||
            StartsWithAny(name, "say", "idle"));
        LoadMobFiles(assets, "zombie", sounds.Step, name =>
            StartsWithAny(name, "step") ||
            IsNumberedVariant(name, "wood") ||
            IsNumberedVariant(name, "metal"));
        LoadMobFiles(assets, "zombie", sounds.Hurt, name =>
            name.Contains("hurt", StringComparison.OrdinalIgnoreCase));
        LoadMobFiles(assets, "zombie", sounds.Death, name =>
            name.Contains("death", StringComparison.OrdinalIgnoreCase));
        LoadMobFiles(assets, "zombie", sounds.Attack, name =>
            name.Contains("infect", StringComparison.OrdinalIgnoreCase) ||
            StartsWithAny(name, "say"));
        sounds.Explosion.AddRange(sounds.Death);
        return sounds;
    }

    private static MobSounds LoadCreeper(AssetStore assets)
    {
        var sounds = new MobSounds();
        LoadMobFiles(assets, "creeper", sounds.Ambient, name =>
            IsNumberedVariant(name, "creeper") ||
            StartsWithAny(name, "say"));
        LoadMobFiles(assets, "creeper", sounds.Death, name =>
            name.Contains("death", StringComparison.OrdinalIgnoreCase));
        string randomRoot = Path.Combine(assets.SoundsPath, "random");
        AddFilteredFiles(assets, randomRoot, "*.ogg", SearchOption.TopDirectoryOnly, sounds.Attack, name =>
            StartsWithAny(name, "fuse"));
        AddFilteredFiles(assets, randomRoot, "*.wav", SearchOption.TopDirectoryOnly, sounds.Attack, name =>
            StartsWithAny(name, "fuse"));
        AddFilteredFiles(assets, randomRoot, "*.ogg", SearchOption.TopDirectoryOnly, sounds.Explosion, name =>
            StartsWithAny(name, "explode", "old_explode"));
        AddFilteredFiles(assets, randomRoot, "*.wav", SearchOption.TopDirectoryOnly, sounds.Explosion, name =>
            StartsWithAny(name, "explode", "old_explode"));
        return sounds;
    }

    private static MobSounds LoadCow(AssetStore assets)
    {
        var sounds = new MobSounds();
        LoadMobFiles(assets, "cow", sounds.Ambient, name =>
            IsNumberedVariant(name, "cow") ||
            StartsWithAny(name, "say"));
        LoadMobFiles(assets, "cow", sounds.Step, name =>
            StartsWithAny(name, "step"));
        LoadMobFiles(assets, "cow", sounds.Hurt, name =>
            name.Contains("hurt", StringComparison.OrdinalIgnoreCase));
        sounds.Attack.AddRange(sounds.Ambient);
        sounds.Death.AddRange(sounds.Hurt);
        sounds.Explosion.AddRange(sounds.Death);
        return sounds;
    }

    private static MobSounds LoadSheep(AssetStore assets)
    {
        var sounds = new MobSounds();
        LoadMobFiles(assets, "sheep", sounds.Ambient, name =>
            IsNumberedVariant(name, "sheep") ||
            StartsWithAny(name, "say"));
        LoadMobFiles(assets, "sheep", sounds.Step, name =>
            StartsWithAny(name, "step"));
        sounds.Attack.AddRange(sounds.Ambient);
        sounds.Hurt.AddRange(sounds.Ambient);
        sounds.Death.AddRange(sounds.Ambient);
        sounds.Explosion.AddRange(sounds.Ambient);
        return sounds;
    }

    private static MobSounds LoadChicken(AssetStore assets)
    {
        var sounds = new MobSounds();
        LoadMobFiles(assets, "chicken", sounds.Ambient, name =>
            IsNumberedVariant(name, "chicken") ||
            StartsWithAny(name, "say"));
        LoadMobFiles(assets, "chicken", sounds.Step, name =>
            StartsWithAny(name, "step"));
        LoadMobFiles(assets, "chicken", sounds.Hurt, name =>
            name.Contains("hurt", StringComparison.OrdinalIgnoreCase));
        sounds.Attack.AddRange(sounds.Ambient);
        sounds.Death.AddRange(sounds.Hurt);
        sounds.Explosion.AddRange(sounds.Death);
        return sounds;
    }

    private static void LoadMobFiles(AssetStore assets, string mobName, List<string> target, Func<string, bool> predicate)
    {
        string root = assets.SoundsPath;
        string mobRoot = Path.Combine(root, "mob");
        string nested = Path.Combine(mobRoot, mobName);

        AddFilteredFiles(assets, nested, "*.ogg", SearchOption.AllDirectories, target, predicate);
        AddFilteredFiles(assets, nested, "*.wav", SearchOption.AllDirectories, target, predicate);
        AddFilteredFiles(assets, mobRoot, $"{mobName}*.ogg", SearchOption.TopDirectoryOnly, target, predicate);
        AddFilteredFiles(assets, mobRoot, $"{mobName}*.wav", SearchOption.TopDirectoryOnly, target, predicate);

        AddFilteredFiles(assets, root, $"{mobName}*.ogg", SearchOption.TopDirectoryOnly, target, predicate);
        AddFilteredFiles(assets, root, $"{mobName}*.wav", SearchOption.TopDirectoryOnly, target, predicate);
    }

    private static void AddFilteredFiles(AssetStore assets, string directory, string pattern, SearchOption searchOption, List<string> target, Func<string, bool> predicate)
    {
        foreach (var path in assets.EnumerateFiles(directory, pattern, searchOption))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (!predicate(name))
            {
                continue;
            }

            if (target.Exists(existing => string.Equals(existing, path, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            target.Add(path);
        }
    }

    private static bool StartsWithAny(string value, params string[] prefixes)
    {
        for (int i = 0; i < prefixes.Length; i++)
        {
            if (value.StartsWith(prefixes[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNumberedVariant(string name, string prefix)
    {
        if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (name.Length == prefix.Length)
        {
            return true;
        }

        for (int i = prefix.Length; i < name.Length; i++)
        {
            if (!char.IsDigit(name[i]))
            {
                return false;
            }
        }

        return true;
    }

    private sealed class MobSounds
    {
        public List<string> Ambient { get; } = new();
        public List<string> Step { get; } = new();
        public List<string> Hurt { get; } = new();
        public List<string> Death { get; } = new();
        public List<string> Attack { get; } = new();
        public List<string> Explosion { get; } = new();

        public int Count => Ambient.Count + Step.Count + Hurt.Count + Death.Count + Attack.Count + Explosion.Count;
    }
}
