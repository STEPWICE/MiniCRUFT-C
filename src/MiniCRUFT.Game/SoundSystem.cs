using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniCRUFT.Audio;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public sealed class SoundSystem : IDisposable
{
    private readonly Dictionary<SoundGroup, List<string>> _dig = new();
    private readonly Dictionary<SoundGroup, List<string>> _step = new();
    private readonly Random _rand = new();
    private readonly bool _enabled;
    private readonly IAudioBackend _backend;
    private readonly AudioConfig _config;

    public SoundSystem(AssetStore assets, IAudioBackend backend, AudioConfig config)
    {
        _backend = backend;
        _config = config;
        try
        {
            var soundRoot = assets.SoundsPath;
            LoadGroup(assets, soundRoot, "dig", _dig);
            LoadGroup(assets, soundRoot, "step", _step);
            _enabled = _backend.IsAvailable && (_dig.Count > 0 || _step.Count > 0);
            Log.Info($"SoundSystem: dig={CountFiles(_dig)}, step={CountFiles(_step)}");
        }
        catch (Exception ex)
        {
            Log.Warn($"SoundSystem disabled: {ex.Message}");
            _enabled = false;
        }
    }

    public void PlayDig(BlockId block)
    {
        if (!_enabled)
        {
            return;
        }

        var group = GetGroup(block);
        PlayRandom(_dig, group, _config.DigVolume);
    }

    public void PlayPlace(BlockId block)
    {
        if (!_enabled)
        {
            return;
        }

        var group = GetGroup(block);
        PlayRandom(_step, group, _config.PlaceVolume);
    }

    private void PlayRandom(Dictionary<SoundGroup, List<string>> bank, SoundGroup group, float volume)
    {
        if (!bank.TryGetValue(group, out var list) || list.Count == 0)
        {
            if (!bank.TryGetValue(SoundGroup.Stone, out list) || list.Count == 0)
            {
                return;
            }
        }

        string path = list[_rand.Next(list.Count)];
        PlayFile(path, volume);
    }

    private void PlayFile(string path, float volume)
    {
        try
        {
            _backend.Play(path, volume);
        }
        catch (Exception ex)
        {
            Log.Warn($"SoundSystem: failed to play '{path}': {ex.Message}");
        }
    }

    private static void LoadGroup(AssetStore assets, string root, string groupName, Dictionary<SoundGroup, List<string>> target)
    {
        string groupDir = Path.Combine(root, groupName);
        foreach (var file in assets.EnumerateFiles(groupDir, "*.ogg", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
            var group = InferGroup(name);
            if (!target.TryGetValue(group, out var list))
            {
                list = new List<string>();
                target[group] = list;
            }
            list.Add(file);
        }
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
            BlockId.TallGrass => SoundGroup.Grass,
            BlockId.Sand => SoundGroup.Sand,
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

    public void Dispose()
    {
        _backend.Dispose();
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
