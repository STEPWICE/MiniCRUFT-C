using System;
using System.Collections.Generic;
using System.Numerics;
using MiniCRUFT.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MiniCRUFT.World;

public sealed class BiomeColorMap
{
    private readonly Vector3[] _grass;
    private readonly Vector3[] _foliage;
    private readonly int _width;
    private readonly int _height;
    private readonly Dictionary<BiomeId, Vector3> _grassCache = new();
    private readonly Dictionary<BiomeId, Vector3> _foliageCache = new();

    private BiomeColorMap(Vector3[] grass, Vector3[] foliage, int width, int height)
    {
        _grass = grass;
        _foliage = foliage;
        _width = width;
        _height = height;
    }

    public static BiomeColorMap Load(AssetStore assets, bool strict = false)
    {
        var grass = LoadMap(assets, "minecraft/textures/colormap/grass.png", out int width, out int height);
        var foliage = LoadMap(assets, "minecraft/textures/colormap/foliage.png", out _, out _);
        if (strict)
        {
            ValidateMap("grass", grass, width, height);
            ValidateMap("foliage", foliage, width, height);
        }
        return new BiomeColorMap(grass, foliage, width, height);
    }

    public Vector3 GetGrassColor(BiomeDefinition biome)
    {
        if (_grassCache.TryGetValue(biome.Id, out var cached))
        {
            return cached;
        }

        var color = Sample(_grass, biome.Temperature, biome.Humidity);
        _grassCache[biome.Id] = color;
        return color;
    }

    public Vector3 GetFoliageColor(BiomeDefinition biome)
    {
        if (_foliageCache.TryGetValue(biome.Id, out var cached))
        {
            return cached;
        }

        var color = Sample(_foliage, biome.Temperature, biome.Humidity);
        _foliageCache[biome.Id] = color;
        return color;
    }

    private Vector3 Sample(Vector3[] map, float temperature, float humidity)
    {
        float t = Math.Clamp(temperature, 0f, 1f);
        float h = Math.Clamp(humidity, 0f, 1f);
        h *= t;

        int x = (int)((1f - t) * (_width - 1));
        int y = (int)((1f - h) * (_height - 1));

        int index = y * _width + x;
        if (index < 0 || index >= map.Length)
        {
            return Vector3.One;
        }

        return map[index];
    }

    private static Vector3[] LoadMap(AssetStore assets, string path, out int width, out int height)
    {
        using var stream = assets.OpenStream(path);
        using var image = Image.Load<Rgba32>(stream);
        width = image.Width;
        height = image.Height;
        var data = new Vector3[width * height];
        int i = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var px = image[x, y];
                data[i++] = new Vector3(px.R / 255f, px.G / 255f, px.B / 255f);
            }
        }
        return data;
    }

    private static void ValidateMap(string name, Vector3[] map, int width, int height)
    {
        if (map.Length == 0)
        {
            throw new InvalidOperationException($"Biome colormap '{name}' is empty.");
        }

        var first = map[0];
        for (int i = 1; i < map.Length; i++)
        {
            var current = map[i];
            if (!current.Equals(first))
            {
                return;
            }
        }

        throw new InvalidOperationException($"Biome colormap '{name}' appears to be a flat placeholder ({width}x{height}).");
    }
}
