using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using MiniCRUFT.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Veldrid;

namespace MiniCRUFT.Renderer;

public sealed class SpriteAtlas : IDisposable
{
    private readonly Dictionary<string, SpriteRegion> _regions;
    public Texture Texture { get; }
    public Sampler Sampler { get; }

    private SpriteAtlas(Texture texture, Sampler sampler, Dictionary<string, SpriteRegion> regions)
    {
        Texture = texture;
        Sampler = sampler;
        _regions = regions;
    }

    public SpriteRegion GetRegion(string name)
    {
        if (_regions.TryGetValue(name, out var region))
        {
            return region;
        }

        return _regions["missing"];
    }

    public static SpriteAtlas Build(GraphicsDevice device, AssetStore assets, IReadOnlyList<SpriteSource> sources, bool repeatSampler)
    {
        return Build(device, assets, sources, repeatSampler, null);
    }

    public static SpriteAtlas Build(GraphicsDevice device, AssetStore assets, IReadOnlyList<SpriteSource> sources, bool repeatSampler, int maxSpriteSize)
    {
        return Build(device, assets, sources, repeatSampler, maxSpriteSize <= 0 ? null : maxSpriteSize);
    }

    private static SpriteAtlas Build(GraphicsDevice device, AssetStore assets, IReadOnlyList<SpriteSource> sources, bool repeatSampler, int? maxSpriteSize)
    {
        var images = new List<(string name, Image<Rgba32> img)>(sources.Count + 1);
        var imagesByName = new Dictionary<string, Image<Rgba32>>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in sources)
        {
            using var stream = assets.OpenStream(source.RelativePath);
            var img = Image.Load<Rgba32>(stream);
            NormalizeSprite(img, maxSpriteSize);
            images.Add((source.Name, img));
            imagesByName[source.Name] = img;
        }

        var missing = BuildMissingTexture(16);
        NormalizeSprite(missing, maxSpriteSize);
        images.Add(("missing", missing));
        imagesByName["missing"] = missing;

        const int padding = 2;
        int atlasWidth = 512;
        int atlasHeight;
        Dictionary<string, SpritePlacement> placements;

        while (true)
        {
            placements = TryPack(images, atlasWidth, padding, out atlasHeight);
            if (placements.Count == images.Count)
            {
                break;
            }
            atlasWidth *= 2;
        }

        atlasWidth = NextPow2(atlasWidth);
        atlasHeight = NextPow2(atlasHeight);

        if (atlasWidth <= 0 || atlasHeight <= 0)
        {
            throw new InvalidOperationException("SpriteAtlas build failed: invalid atlas dimensions.");
        }

        var atlas = new Image<Rgba32>(atlasWidth, atlasHeight);
        var regions = new Dictionary<string, SpriteRegion>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in placements)
        {
            var (name, placement) = (entry.Key, entry.Value);
            var img = imagesByName[name];
            atlas.Mutate(ctx => ctx.DrawImage(img, new SixLabors.ImageSharp.Point(placement.X, placement.Y), 1f));

            var min = new Vector2((float)placement.X / atlasWidth, (float)placement.Y / atlasHeight);
            var max = new Vector2((float)(placement.X + img.Width) / atlasWidth, (float)(placement.Y + img.Height) / atlasHeight);
            regions[name] = new SpriteRegion(min, max, new Vector2(img.Width, img.Height));
        }

        var pixelData = new byte[atlasWidth * atlasHeight * 4];
        atlas.CopyPixelDataTo(pixelData);

        var texture = device.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
            (uint)atlasWidth, (uint)atlasHeight, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm_SRgb, TextureUsage.Sampled));
        device.UpdateTexture(texture, pixelData, 0, 0, 0, (uint)atlasWidth, (uint)atlasHeight, 1, 0, 0);

        var sampler = device.ResourceFactory.CreateSampler(new SamplerDescription
        {
            AddressModeU = repeatSampler ? SamplerAddressMode.Wrap : SamplerAddressMode.Clamp,
            AddressModeV = repeatSampler ? SamplerAddressMode.Wrap : SamplerAddressMode.Clamp,
            AddressModeW = SamplerAddressMode.Clamp,
            Filter = SamplerFilter.MinPoint_MagPoint_MipPoint,
            MaximumAnisotropy = 1
        });

        foreach (var img in images)
        {
            img.img.Dispose();
        }
        atlas.Dispose();

        return new SpriteAtlas(texture, sampler, regions);
    }

    private static void NormalizeSprite(Image<Rgba32> img, int? maxSpriteSize)
    {
        if (!maxSpriteSize.HasValue)
        {
            return;
        }

        int target = maxSpriteSize.Value;
        if (img.Width == target && img.Height == target)
        {
            return;
        }

        img.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(target, target),
            Mode = ResizeMode.Stretch,
            Sampler = KnownResamplers.NearestNeighbor
        }));
    }

    private static Dictionary<string, SpritePlacement> TryPack(List<(string name, Image<Rgba32> img)> images, int atlasWidth, int padding, out int atlasHeight)
    {
        var placements = new Dictionary<string, SpritePlacement>(StringComparer.OrdinalIgnoreCase);
        int x = padding;
        int y = padding;
        int rowHeight = 0;

        foreach (var (name, img) in images)
        {
            if (img.Width + padding * 2 > atlasWidth)
            {
                atlasHeight = 0;
                return new Dictionary<string, SpritePlacement>();
            }

            if (x + img.Width + padding > atlasWidth)
            {
                x = padding;
                y += rowHeight + padding;
                rowHeight = 0;
            }

            placements[name] = new SpritePlacement(x, y);
            x += img.Width + padding;
            rowHeight = Math.Max(rowHeight, img.Height);
        }

        atlasHeight = y + rowHeight + padding;
        return placements;
    }

    private static int NextPow2(int value)
    {
        int pow = 1;
        while (pow < value)
        {
            pow <<= 1;
        }
        return pow;
    }

    private static Image<Rgba32> BuildMissingTexture(int size)
    {
        var img = new Image<Rgba32>(size, size);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool magenta = ((x / 4) + (y / 4)) % 2 == 0;
                img[x, y] = magenta ? new Rgba32(255, 0, 255, 255) : new Rgba32(0, 0, 0, 255);
            }
        }
        return img;
    }

    public void Dispose()
    {
        Texture.Dispose();
        Sampler.Dispose();
    }

    private readonly struct SpritePlacement
    {
        public int X { get; }
        public int Y { get; }

        public SpritePlacement(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}

public readonly struct SpriteSource
{
    public string Name { get; }
    public string RelativePath { get; }

    public SpriteSource(string name, string relativePath)
    {
        Name = name;
        RelativePath = relativePath;
    }
}

public readonly struct SpriteRegion
{
    public Vector2 Min { get; }
    public Vector2 Max { get; }
    public Vector2 Size { get; }

    public SpriteRegion(Vector2 min, Vector2 max, Vector2 size)
    {
        Min = min;
        Max = max;
        Size = size;
    }
}
