using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using MiniCRUFT.Audio;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public sealed class SoundSystem : IDisposable
{
    private readonly Dictionary<SoundGroup, List<string>> _dig = new();
    private readonly Dictionary<SoundGroup, List<string>> _step = new();
    private readonly List<string> _glass = new();
    private readonly List<string> _fuse = new();
    private readonly List<string> _fireIgnite = new();
    private readonly List<string> _fireCrackle = new();
    private readonly List<string> _explosionAlpha = new();
    private readonly List<string> _explosionFallback = new();
    private readonly List<string> _liquidSplash = new();
    private readonly List<string> _liquidSwim = new();
    private readonly List<string> _liquidWater = new();
    private readonly List<string> _liquidLava = new();
    private readonly Random _rand = new();
    private readonly bool _enabled;
    private readonly IAudioBackend _backend;
    private readonly AudioConfig _config;
    private readonly bool _ownsBackend;
    private Vector3 _listenerPosition;
    private Vector3 _listenerRight = Vector3.UnitX;

    public SoundSystem(AssetStore assets, IAudioBackend backend, AudioConfig config, bool ownsBackend = true)
    {
        _backend = backend;
        _config = config;
        _ownsBackend = ownsBackend;
        try
        {
            var soundRoot = assets.SoundsPath;
            LoadGroup(assets, soundRoot, "dig", _dig);
            LoadGroup(assets, soundRoot, "step", _step);
            LoadPrefixedGroup(assets, soundRoot, "random", "glass", _glass);
            LoadPrefixedGroup(assets, soundRoot, "random", "fuse", _fuse);
            LoadPrefixedGroup(assets, soundRoot, "fire", "ignite", _fireIgnite);
            LoadPrefixedGroup(assets, soundRoot, "fire", "fire", _fireCrackle);
            LoadPrefixedGroup(assets, soundRoot, "random", "old_explode", _explosionAlpha);
            LoadPrefixedGroup(assets, soundRoot, "random", "explode", _explosionFallback);
            LoadLiquidSounds(assets, Path.Combine(soundRoot, "liquid"));
            _enabled = _backend.IsAvailable && (_dig.Count > 0 || _step.Count > 0 || _glass.Count > 0 || _fuse.Count > 0 || _fireIgnite.Count > 0 || _fireCrackle.Count > 0 || _explosionAlpha.Count > 0 || _explosionFallback.Count > 0 || _liquidSplash.Count > 0 || _liquidSwim.Count > 0 || _liquidWater.Count > 0 || _liquidLava.Count > 0);
            Log.Info($"SoundSystem: dig={CountFiles(_dig)}, step={CountFiles(_step)}, glass={_glass.Count}, fuse={_fuse.Count}, fireIgnite={_fireIgnite.Count}, fireCrackle={_fireCrackle.Count}, explosion={_explosionAlpha.Count + _explosionFallback.Count}, liquidSplash={_liquidSplash.Count}, liquidSwim={_liquidSwim.Count}, liquidWater={_liquidWater.Count}, liquidLava={_liquidLava.Count}");
        }
        catch (Exception ex)
        {
            Log.Warn($"SoundSystem disabled: {ex.Message}");
            _enabled = false;
        }
    }

    public void PlayDig(BlockId block)
    {
        PlayDig(block, _listenerPosition);
    }

    public void PlayDig(BlockId block, Vector3 sourcePosition)
    {
        if (!_enabled)
        {
            return;
        }

        PlaySoundForBlock(block, _dig, _config.DigVolume, sourcePosition);
    }

    public void PlayPlace(BlockId block)
    {
        PlayPlace(block, _listenerPosition);
    }

    public void PlayPlace(BlockId block, Vector3 sourcePosition)
    {
        if (!_enabled)
        {
            return;
        }

        PlaySoundForBlock(block, _step, _config.PlaceVolume, sourcePosition);
    }

    public void PlayStep(BlockId block)
    {
        PlayStep(block, _listenerPosition);
    }

    public void PlayStep(BlockId block, Vector3 sourcePosition)
    {
        if (!_enabled)
        {
            return;
        }

        PlaySoundForBlock(block, _step, _config.StepVolume, sourcePosition);
    }

    public void PlayRun(BlockId block)
    {
        PlayRun(block, _listenerPosition);
    }

    public void PlayRun(BlockId block, Vector3 sourcePosition)
    {
        if (!_enabled)
        {
            return;
        }

        PlaySoundForBlock(block, _step, _config.RunVolume, sourcePosition);
    }

    public void PlayJump(BlockId block)
    {
        PlayJump(block, _listenerPosition);
    }

    public void PlayJump(BlockId block, Vector3 sourcePosition)
    {
        if (!_enabled)
        {
            return;
        }

        PlaySoundForBlock(block, _step, _config.JumpVolume, sourcePosition);
    }

    public void PlayFuse()
    {
        PlayFuse(_listenerPosition);
    }

    public void PlayFuse(Vector3 sourcePosition)
    {
        if (!_enabled)
        {
            return;
        }

        PlayRandom(_fuse, _config.FuseVolume, sourcePosition);
    }

    public void PlayFire(FireEventKind kind, Vector3 sourcePosition, float intensity = 1f)
    {
        if (!_enabled)
        {
            return;
        }

        float scale = Math.Clamp(intensity, 0f, 1.5f);
        switch (kind)
        {
            case FireEventKind.Ignited:
                PlayRandom(_fireIgnite.Count > 0 ? _fireIgnite : _fireCrackle, Math.Clamp(_config.FireVolume * scale, 0f, 1f), sourcePosition);
                break;
            case FireEventKind.Crackle:
                PlayRandom(_fireCrackle.Count > 0 ? _fireCrackle : _fireIgnite, Math.Clamp(_config.FireVolume * 0.35f * scale, 0f, 1f), sourcePosition);
                break;
            case FireEventKind.Consumed:
                PlayRandom(_fireCrackle.Count > 0 ? _fireCrackle : _fireIgnite, Math.Clamp(_config.FireVolume * 0.55f * scale, 0f, 1f), sourcePosition);
                break;
            case FireEventKind.Extinguished:
                PlayRandom(_fireCrackle.Count > 0 ? _fireCrackle : _fireIgnite, Math.Clamp(_config.FireVolume * 0.25f * scale, 0f, 1f), sourcePosition);
                break;
        }
    }

    public void PlayExplosion(float intensity = 1f)
    {
        PlayExplosion(intensity, _listenerPosition);
    }

    public void PlayExplosion(float intensity, Vector3 sourcePosition)
    {
        if (!_enabled)
        {
            return;
        }

        float volume = Math.Clamp(_config.ExplosionVolume * intensity, 0f, 1f);
        if (_explosionAlpha.Count > 0)
        {
            PlayRandom(_explosionAlpha, volume, sourcePosition);
            return;
        }

        PlayRandom(_explosionFallback, volume, sourcePosition);
    }

    public void PlayLiquidSplash(LiquidKind kind, Vector3 sourcePosition, float intensity = 1f)
    {
        if (!_enabled)
        {
            return;
        }

        float volume = Math.Clamp(_config.LiquidVolume * Math.Clamp(intensity, 0.25f, 1.5f), 0f, 1f);
        if (kind == LiquidKind.Lava)
        {
            PlayRandom(_liquidLava.Count > 0 ? _liquidLava : _liquidSplash, volume, sourcePosition);
            return;
        }

        PlayRandom(_liquidSplash.Count > 0 ? _liquidSplash : _liquidWater, volume, sourcePosition);
    }

    public void PlayLiquidSwim(LiquidKind kind, Vector3 sourcePosition, float intensity = 1f)
    {
        if (!_enabled)
        {
            return;
        }

        float volume = Math.Clamp(_config.LiquidVolume * 0.75f * Math.Clamp(intensity, 0.25f, 1.5f), 0f, 1f);
        if (kind == LiquidKind.Lava)
        {
            PlayRandom(_liquidLava.Count > 0 ? _liquidLava : _liquidWater, volume, sourcePosition);
            return;
        }

        PlayRandom(_liquidSwim.Count > 0 ? _liquidSwim : _liquidWater, volume, sourcePosition);
    }

    public void PlayWaterSplash(Vector3 sourcePosition, float intensity = 1f)
    {
        PlayLiquidSplash(LiquidKind.Water, sourcePosition, intensity);
    }

    public void PlayWaterSwim(Vector3 sourcePosition, float intensity = 1f)
    {
        PlayLiquidSwim(LiquidKind.Water, sourcePosition, intensity);
    }

    public void SetListener(Vector3 position, Vector3 rightVector)
    {
        _listenerPosition = position;
        _listenerRight = rightVector.LengthSquared() > float.Epsilon
            ? Vector3.Normalize(rightVector)
            : Vector3.UnitX;
    }

    private void PlaySoundForBlock(BlockId block, Dictionary<SoundGroup, List<string>> bank, float volume, Vector3 sourcePosition)
    {
        if (block == BlockId.Glass && _glass.Count > 0)
        {
            PlayRandom(_glass, volume, sourcePosition);
            return;
        }

        var group = GetGroup(block);
        PlayRandom(bank, group, volume, sourcePosition);
    }

    private void PlayRandom(Dictionary<SoundGroup, List<string>> bank, SoundGroup group, float volume, Vector3 sourcePosition)
    {
        if (!bank.TryGetValue(group, out var list) || list.Count == 0)
        {
            if (!bank.TryGetValue(SoundGroup.Stone, out list) || list.Count == 0)
            {
                return;
            }
        }

        string path = list[_rand.Next(list.Count)];
        PlayFile(path, volume, sourcePosition);
    }

    private void PlayRandom(List<string> list, float volume, Vector3 sourcePosition)
    {
        if (list.Count == 0)
        {
            return;
        }

        string path = list[_rand.Next(list.Count)];
        PlayFile(path, volume, sourcePosition);
    }

    private void PlayFile(string path, float volume, Vector3 sourcePosition)
    {
        try
        {
            float masterVolume = Math.Clamp(_config.MasterVolume, 0f, 1f);
            var mix = SpatialAudio.Calculate(_listenerPosition, sourcePosition, _listenerRight, _config, volume * masterVolume);
            if (mix.Volume <= 0f)
            {
                return;
            }

            _backend.Play(path, mix.Volume, mix.Pan);
        }
        catch (Exception ex)
        {
            Log.Warn($"SoundSystem: failed to play '{path}': {ex.Message}");
        }
    }

    private static void LoadGroup(AssetStore assets, string root, string groupName, Dictionary<SoundGroup, List<string>> target)
    {
        string groupDir = Path.Combine(root, groupName);
        if (TryReadManifestFiles(assets, groupDir, out var files))
        {
            foreach (var fileName in files)
            {
                AddSoundFile(Path.Combine(groupDir, fileName), target);
            }
            return;
        }

        foreach (var file in assets.EnumerateFiles(groupDir, "*.ogg", SearchOption.TopDirectoryOnly))
        {
            AddSoundFile(file, target);
        }
    }

    private static void LoadPrefixedGroup(AssetStore assets, string root, string groupName, string prefix, List<string> target)
    {
        string groupDir = Path.Combine(root, groupName);
        if (TryReadManifestFiles(assets, groupDir, out var files))
        {
            foreach (var fileName in files)
            {
                if (Path.GetFileNameWithoutExtension(fileName).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    target.Add(Path.Combine(groupDir, fileName));
                }
            }
            return;
        }

        foreach (var file in assets.EnumerateFiles(groupDir, "*.ogg", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                target.Add(file);
            }
        }
    }

    private void LoadLiquidSounds(AssetStore assets, string liquidDir)
    {
        foreach (var file in assets.EnumerateFiles(liquidDir, "*.ogg", SearchOption.AllDirectories))
        {
            ClassifyLiquidSound(file);
        }

        foreach (var file in assets.EnumerateFiles(liquidDir, "*.wav", SearchOption.AllDirectories))
        {
            ClassifyLiquidSound(file);
        }
    }

    private void ClassifyLiquidSound(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        if (name.StartsWith("swim", StringComparison.OrdinalIgnoreCase))
        {
            _liquidSwim.Add(path);
            return;
        }

        if (name.StartsWith("splash", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("heavy_splash", StringComparison.OrdinalIgnoreCase))
        {
            _liquidSplash.Add(path);
            return;
        }

        if (name.Contains("lava", StringComparison.OrdinalIgnoreCase))
        {
            _liquidLava.Add(path);
            return;
        }

        _liquidWater.Add(path);
    }

    private static bool TryReadManifestFiles(AssetStore assets, string groupDir, out List<string> files)
    {
        files = new List<string>();
        string manifestPath = Path.Combine(groupDir, "_list.json");
        if (!File.Exists(manifestPath))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(assets.ReadAllText(manifestPath));
            if (!document.RootElement.TryGetProperty("files", out var filesElement) || filesElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var item in filesElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var value = item.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        files.Add(value);
                    }
                }
            }

            return files.Count > 0;
        }
        catch (Exception ex)
        {
            Log.Warn($"SoundSystem: failed to read manifest '{manifestPath}': {ex.Message}");
            return false;
        }
    }

    private static void AddSoundFile(string path, Dictionary<SoundGroup, List<string>> target)
    {
        string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        var group = InferGroup(name);
        if (!target.TryGetValue(group, out var list))
        {
            list = new List<string>();
            target[group] = list;
        }

        list.Add(path);
    }

    private static SoundGroup InferGroup(string name)
    {
        if (name.Contains("grass")) return SoundGroup.Grass;
        if (name.Contains("gravel")) return SoundGroup.Gravel;
        if (name.Contains("sand")) return SoundGroup.Sand;
        if (name.Contains("snow")) return SoundGroup.Snow;
        if (name.Contains("wood")) return SoundGroup.Wood;
        if (name.Contains("cloth")) return SoundGroup.Cloth;
        if (name.Contains("stone")) return SoundGroup.Stone;
        return SoundGroup.Stone;
    }

    private static SoundGroup GetGroup(BlockId block)
    {
        return block switch
        {
            BlockId.Grass => SoundGroup.Grass,
            BlockId.Dirt => SoundGroup.Grass,
            BlockId.Leaves => SoundGroup.Grass,
            BlockId.BirchLeaves => SoundGroup.Grass,
            BlockId.SpruceLeaves => SoundGroup.Grass,
            BlockId.TallGrass => SoundGroup.Grass,
            BlockId.DeadBush => SoundGroup.Grass,
            BlockId.SugarCane => SoundGroup.Grass,
            BlockId.Sand => SoundGroup.Sand,
            BlockId.Cactus => SoundGroup.Sand,
            BlockId.Gravel => SoundGroup.Gravel,
            BlockId.Snow => SoundGroup.Snow,
            BlockId.Clay => SoundGroup.Gravel,
            BlockId.Wood => SoundGroup.Wood,
            BlockId.Planks => SoundGroup.Wood,
            BlockId.BirchWood => SoundGroup.Wood,
            BlockId.SpruceWood => SoundGroup.Wood,
            _ => SoundGroup.Stone
        };
    }

    private static int CountFiles(Dictionary<SoundGroup, List<string>> bank)
    {
        int count = 0;
        foreach (var list in bank.Values)
        {
            count += list.Count;
        }
        return count;
    }

    private static int CountFiles(List<string> bank)
    {
        return bank.Count;
    }

    public void Dispose()
    {
        if (_ownsBackend)
        {
            _backend.Dispose();
        }
    }

    private enum SoundGroup
    {
        Grass,
        Gravel,
        Sand,
        Snow,
        Wood,
        Cloth,
        Stone
    }
}
