using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using MiniCRUFT.Audio;
using MiniCRUFT.Core;
using MiniCRUFT.World;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Game;

public sealed class AmbientSoundSystem : IDisposable
{
    private readonly List<string> _ambient = new();
    private readonly List<string> _music = new();
    private readonly List<string> _weather = new();
    private readonly List<string> _thunder = new();
    private readonly List<string> _liquid = new();
    private readonly List<string> _liquidWater = new();
    private readonly List<string> _liquidLava = new();
    private readonly Random _rand = new();
    private readonly IAudioBackend _backend;
    private readonly AudioConfig _config;
    private readonly bool _enabled;
    private float _ambientTimer;
    private float _musicTimer;
    private float _weatherTimer;
    private float _liquidTimer;
    private bool _wasRaining;

    public AmbientSoundSystem(AssetStore assets, IAudioBackend backend, AudioConfig config)
    {
        _backend = backend;
        _config = config;

        try
        {
            LoadCategory(assets, "ambient", _ambient, null, null);
            LoadCategory(assets, "music", _music, null, null);
            LoadWeatherCategory(assets, Path.Combine("ambient", "weather"), _weather, _thunder);
            LoadCategory(assets, "liquid", _liquid, _liquidWater, _liquidLava);
            _enabled = _backend.IsAvailable && (_ambient.Count + _music.Count + _weather.Count + _thunder.Count + _liquid.Count) > 0;
            Log.Info($"AmbientSoundSystem: ambient={_ambient.Count}, music={_music.Count}, weather={_weather.Count}, thunder={_thunder.Count}, liquid={_liquid.Count}");
        }
        catch (Exception ex)
        {
            Log.Warn($"AmbientSoundSystem disabled: {ex.Message}");
            _enabled = false;
        }

        Reset();
    }

    public void Reset()
    {
        _ambientTimer = NextInterval(_config.AmbientIntervalSeconds);
        _musicTimer = NextInterval(_config.MusicIntervalSeconds);
        _weatherTimer = NextInterval(_config.WeatherIntervalSeconds);
        _liquidTimer = NextInterval(_config.LiquidIntervalSeconds);
        _wasRaining = false;
    }

    public void Update(float dt, WorldType world, Vector3 playerPos, float rainIntensity)
    {
        if (!_enabled || dt <= 0f)
        {
            return;
        }

        _ambientTimer -= dt;
        _musicTimer -= dt;
        _liquidTimer -= dt;

        bool raining = rainIntensity > 0.05f;
        if (raining && !_wasRaining)
        {
            _weatherTimer = 0f;
        }

        if (raining)
        {
            _weatherTimer -= dt;
        }

        if (_ambientTimer <= 0f)
        {
            PlayRandom(_ambient, _config.AmbientVolume);
            _ambientTimer = NextInterval(_config.AmbientIntervalSeconds);
        }

        if (_musicTimer <= 0f)
        {
            PlayRandom(_music, _config.MusicVolume);
            _musicTimer = NextInterval(_config.MusicIntervalSeconds);
        }

        if (raining && _weather.Count > 0 && _weatherTimer <= 0f)
        {
            float volume = Math.Clamp(_config.WeatherVolume * Math.Clamp(rainIntensity, 0.2f, 1f), 0f, 1f);
            PlayRandom(_weather, volume);
            _weatherTimer = NextInterval(_config.WeatherIntervalSeconds);
        }

        if (_liquidTimer <= 0f)
        {
            var nearbyLiquid = FindNearbyLiquidKind(world, playerPos, _config.LiquidRadius);
            if (nearbyLiquid == LiquidKind.Lava)
            {
                PlayRandom(_liquidLava.Count > 0 ? _liquidLava : _liquid, _config.LiquidVolume);
            }
            else if (nearbyLiquid == LiquidKind.Water)
            {
                PlayRandom(_liquidWater.Count > 0 ? _liquidWater : _liquid, _config.LiquidVolume);
            }
            _liquidTimer = NextInterval(_config.LiquidIntervalSeconds);
        }

        _wasRaining = raining;
    }

    public void PlayThunder()
    {
        if (_thunder.Count > 0)
        {
            PlayRandom(_thunder, _config.WeatherVolume);
            return;
        }

        PlayRandom(_weather, _config.WeatherVolume);
    }

    private void PlayRandom(List<string> list, float volume)
    {
        if (list.Count == 0)
        {
            return;
        }

        string path = list[_rand.Next(list.Count)];
        try
        {
            _backend.Play(path, volume);
        }
        catch (Exception ex)
        {
            Log.Warn($"AmbientSoundSystem: failed to play '{path}': {ex.Message}");
        }
    }

    private static void LoadWeatherCategory(AssetStore assets, string name, List<string> weather, List<string> thunder)
    {
        string dir = Path.Combine(assets.SoundsPath, name);
        foreach (var file in assets.EnumerateFiles(dir, "*.ogg", SearchOption.AllDirectories))
        {
            AddWeatherSound(file, weather, thunder);
        }

        foreach (var file in assets.EnumerateFiles(dir, "*.wav", SearchOption.AllDirectories))
        {
            AddWeatherSound(file, weather, thunder);
        }
    }

    private static void AddWeatherSound(string path, List<string> weather, List<string> thunder)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        if (name.Contains("thunder", StringComparison.OrdinalIgnoreCase))
        {
            thunder.Add(path);
            return;
        }

        weather.Add(path);
    }

    private static void LoadCategory(AssetStore assets, string name, List<string> target, List<string>? water, List<string>? lava)
    {
        string dir = Path.Combine(assets.SoundsPath, name);
        foreach (var file in assets.EnumerateFiles(dir, "*.ogg", SearchOption.AllDirectories))
        {
            target.Add(file);
            if (water != null && lava != null)
            {
                ClassifyLiquidSound(file, water, lava);
            }
        }
        foreach (var file in assets.EnumerateFiles(dir, "*.wav", SearchOption.AllDirectories))
        {
            target.Add(file);
            if (water != null && lava != null)
            {
                ClassifyLiquidSound(file, water, lava);
            }
        }
    }

    private static void ClassifyLiquidSound(string path, List<string> water, List<string> lava)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        if (name.Contains("lava", StringComparison.OrdinalIgnoreCase))
        {
            lava.Add(path);
        }
        else
        {
            water.Add(path);
        }
    }

    private static LiquidKind? FindNearbyLiquidKind(WorldType world, Vector3 position, float radius)
    {
        if (radius <= 0f)
        {
            return null;
        }

        int r = Math.Min(12, (int)MathF.Ceiling(radius));
        int baseX = (int)MathF.Floor(position.X);
        int baseY = (int)MathF.Floor(position.Y);
        int baseZ = (int)MathF.Floor(position.Z);

        int minY = Math.Max(1, baseY - 2);
        int maxY = Math.Min(Chunk.SizeY - 2, baseY + 2);

        for (int y = minY; y <= maxY; y++)
        {
            for (int dz = -r; dz <= r; dz++)
            {
                int z = baseZ + dz;
                for (int dx = -r; dx <= r; dx++)
                {
                    int x = baseX + dx;
                    var id = world.GetBlock(x, y, z);
                    if (LiquidBlocks.IsLava(id))
                    {
                        return LiquidKind.Lava;
                    }

                    if (LiquidBlocks.IsWater(id))
                    {
                        return LiquidKind.Water;
                    }
                }
            }
        }

        return null;
    }

    private float NextInterval(float baseSeconds)
    {
        if (baseSeconds <= 0f)
        {
            return 0f;
        }

        return baseSeconds * (0.7f + (float)_rand.NextDouble() * 0.6f);
    }

    public void Dispose()
    {
        // shared backend disposed elsewhere
    }
}
