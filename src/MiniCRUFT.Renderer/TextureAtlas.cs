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

public sealed class TextureAtlas : IDisposable
{
    private readonly Dictionary<string, AtlasRegion> _regions;
    public Texture Texture { get; }
    public Sampler Sampler { get; }

    public TextureAtlas(Texture texture, Sampler sampler, Dictionary<string, AtlasRegion> regions)
    {
        Texture = texture;
        Sampler = sampler;
        _regions = regions;
    }

    public AtlasRegion GetRegion(string name)
    {
        if (_regions.TryGetValue(name, out var region))
        {
            return region;
        }

        return _regions["missing"];
    }

    public static TextureAtlas Build(GraphicsDevice device, AssetStore assets, RenderConfig renderConfig, IEnumerable<string> textureNames)
    {
        var blockDir = assets.GetPath("minecraft", "textures", "block");
        var waterDir = assets.GetPath("minecraft", "textures", "water");

        var names = new HashSet<string>(textureNames, StringComparer.OrdinalIgnoreCase);
        names.Remove("missing");
        if (names.Count == 0)
        {
            Log.Warn("TextureAtlas: no texture names provided. Using fallback missing texture only.");
        }

        var images = new List<(string name, Image<Rgba32> img)>();
        const int tileSize = 16;
        const int maxTileSize = 64;
        const int padding = 2;
        int paddedTile = tileSize + padding * 2;

        foreach (var name in names)
        {
            string file = Path.Combine(blockDir, name + ".png");
            if (!File.Exists(file))
            {
                file = Path.Combine(waterDir, name + ".png");
            }
            if (!File.Exists(file))
            {
                Log.Warn($"TextureAtlas: missing texture '{name}'.");
                continue;
            }

            Image<Rgba32> img;
            try
            {
                using var stream = assets.OpenStream(file);
                img = Image.Load<Rgba32>(stream);
            }
            catch (Exception ex)
            {
                Log.Warn($"TextureAtlas: failed to load '{name}' from '{file}': {ex.Message}");
                continue;
            }

            int srcSize = Math.Max(img.Width, img.Height);
            if (img.Width == tileSize && img.Height > tileSize && img.Height % tileSize == 0)
            {
                img.Mutate(ctx => ctx.Crop(new SixLabors.ImageSharp.Rectangle(0, 0, tileSize, tileSize)));
            }
            else
            {
                if (srcSize > maxTileSize)
                {
                    Log.Warn($"TextureAtlas: texture '{name}' is {img.Width}x{img.Height}. Resizing to {tileSize}x{tileSize}.");
                }

                if (img.Width != tileSize || img.Height != tileSize)
                {
                    img.Mutate(ctx => ctx.Resize(tileSize, tileSize, KnownResamplers.NearestNeighbor));
                }
            }

            var padded = PadTile(img, padding);
            img.Dispose();
            images.Add((name, padded));
        }

        var missing = BuildMissingTexture(tileSize);
        images.Add(("missing", PadTile(missing, padding)));
        missing.Dispose();

        int count = images.Count;
        int columns = (int)MathF.Ceiling(MathF.Sqrt(count));
        int rows = (int)MathF.Ceiling(count / (float)columns);

        int atlasWidth = NextPow2(columns * paddedTile);
        int atlasHeight = NextPow2(rows * paddedTile);

        var atlas = new Image<Rgba32>(atlasWidth, atlasHeight);

        var regions = new Dictionary<string, AtlasRegion>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < images.Count; i++)
        {
            int col = i % columns;
            int row = i / columns;
            int x = col * paddedTile;
            int y = row * paddedTile;

            var (name, img) = images[i];
            atlas.Mutate(ctx => ctx.DrawImage(img, new SixLabors.ImageSharp.Point(x, y), 1f));

            var min = new Vector2((float)(x + padding) / atlasWidth, (float)(y + padding) / atlasHeight);
            var max = new Vector2((float)(x + padding + tileSize) / atlasWidth, (float)(y + padding + tileSize) / atlasHeight);
            regions[name] = new AtlasRegion(min, max);
        }

        int mipLevels = renderConfig.UseMipmaps ? 1 + (int)MathF.Floor(MathF.Log2(MathF.Max(atlasWidth, atlasHeight))) : 1;

        var texture = device.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
            (uint)atlasWidth,
            (uint)atlasHeight,
            mipLevels: (uint)mipLevels,
            arrayLayers: 1,
            format: PixelFormat.R8_G8_B8_A8_UNorm_SRgb,
            usage: TextureUsage.Sampled));

        var pixelData = new byte[atlasWidth * atlasHeight * 4];
        atlas.CopyPixelDataTo(pixelData);
        device.UpdateTexture(texture, pixelData, 0, 0, 0, (uint)atlasWidth, (uint)atlasHeight, 1u, 0u, 0u);

        if (renderConfig.UseMipmaps && mipLevels > 1)
        {
            var current = atlas.Clone();
            for (int level = 1; level < mipLevels; level++)
            {
                int w = Math.Max(1, atlasWidth >> level);
                int h = Math.Max(1, atlasHeight >> level);
                current.Mutate(ctx => ctx.Resize(w, h, KnownResamplers.NearestNeighbor));

                var data = new byte[w * h * 4];
                current.CopyPixelDataTo(data);
                device.UpdateTexture(texture, data, 0, 0, 0, (uint)w, (uint)h, 1u, (uint)level, 0u);
            }
            current.Dispose();
        }

        var samplerFilter = renderConfig.Anisotropy > 1 ? SamplerFilter.Anisotropic : SamplerFilter.MinPoint_MagPoint_MipPoint;
        var sampler = device.ResourceFactory.CreateSampler(new SamplerDescription
        {
            AddressModeU = SamplerAddressMode.Clamp,
            AddressModeV = SamplerAddressMode.Clamp,
            AddressModeW = SamplerAddressMode.Clamp,
            Filter = samplerFilter,
            MaximumAnisotropy = (uint)Math.Clamp(renderConfig.Anisotropy, 1, 16)
        });

        foreach (var image in images)
        {
            image.img.Dispose();
        }
        atlas.Dispose();

        return new TextureAtlas(texture, sampler, regions);
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

    private static Image<Rgba32> PadTile(Image<Rgba32> src, int padding)
    {
        int size = src.Width;
        int padded = size + padding * 2;
        var img = new Image<Rgba32>(padded, padded);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                img[x + padding, y + padding] = src[x, y];
            }
        }

        for (int x = 0; x < size; x++)
        {
            var top = src[x, 0];
            var bottom = src[x, size - 1];
            for (int p = 0; p < padding; p++)
            {
                img[x + padding, p] = top;
                img[x + padding, padding + size + p] = bottom;
            }
        }

        for (int y = 0; y < size; y++)
        {
            var left = src[0, y];
            var right = src[size - 1, y];
            for (int p = 0; p < padding; p++)
            {
                img[p, y + padding] = left;
                img[padding + size + p, y + padding] = right;
            }
        }

        var tl = src[0, 0];
        var tr = src[size - 1, 0];
        var bl = src[0, size - 1];
        var br = src[size - 1, size - 1];
        for (int py = 0; py < padding; py++)
        {
            for (int px = 0; px < padding; px++)
            {
                img[px, py] = tl;
                img[padding + size + px, py] = tr;
                img[px, padding + size + py] = bl;
                img[padding + size + px, padding + size + py] = br;
            }
        }

        return img;
    }

    public void Dispose()
    {
        Texture.Dispose();
        Sampler.Dispose();
    }
}

public readonly struct AtlasRegion
{
    public Vector2 Min { get; }
    public Vector2 Max { get; }

    public AtlasRegion(Vector2 min, Vector2 max)
    {
        Min = min;
        Max = max;
    }
}
