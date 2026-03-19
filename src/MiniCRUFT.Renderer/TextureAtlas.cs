using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using MiniCRUFT.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Veldrid;

namespace MiniCRUFT.Renderer;

public sealed class TextureAtlas : IDisposable
{
    public const int TileSize = 16;
    public const int Padding = 4;
    public const int PaddedTileSize = TileSize + Padding * 2;

    private readonly Dictionary<string, AtlasRegion> _regions;
    private readonly TextureAtlasAnimation[] _animations;
    public int MipLevels { get; }
    public Texture Texture { get; }
    public Sampler Sampler { get; }
    public IReadOnlyList<TextureAtlasAnimation> Animations => _animations;

    public TextureAtlas(Texture texture, Sampler sampler, Dictionary<string, AtlasRegion> regions, TextureAtlasAnimation[] animations, int mipLevels)
    {
        Texture = texture;
        Sampler = sampler;
        _regions = regions;
        _animations = animations;
        MipLevels = mipLevels;
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
        var itemDir = assets.GetPath("minecraft", "textures", "item");

        var names = new HashSet<string>(textureNames, StringComparer.OrdinalIgnoreCase);
        names.Remove("missing");
        if (names.Count == 0)
        {
            Log.Warn("TextureAtlas: no texture names provided. Using fallback missing texture only.");
        }

        var images = new List<LoadedTexture>(names.Count + 1);
        var animations = new List<TextureAtlasAnimation>();
        const int maxTileSize = 64;

        foreach (var name in names)
        {
            string file = Path.Combine(blockDir, name + ".png");
            if (!File.Exists(file))
            {
                file = Path.Combine(waterDir, name + ".png");
            }
            if (!File.Exists(file))
            {
                file = Path.Combine(itemDir, name + ".png");
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

            TextureAtlasAnimationDefinition? animationDefinition = TryCreateAnimationDefinition(assets, file, img.Width, img.Height);
            var atlasImage = img.Clone();
            NormalizeTexture(file, atlasImage, TileSize, maxTileSize, animationDefinition);

            var padded = PadTile(atlasImage, Padding);
            atlasImage.Dispose();

            images.Add(new LoadedTexture(
                name,
                padded,
                animationDefinition is null ? null : img,
                animationDefinition));

            if (animationDefinition is null)
            {
                img.Dispose();
            }
        }

        using (var betaSmoke = CreateBetaSmokeTexture())
        {
            images.Add(new LoadedTexture("beta_smoke", PadTile(betaSmoke, Padding), null, null));
        }

        var missing = BuildMissingTexture(TileSize);
        images.Add(new LoadedTexture("missing", PadTile(missing, Padding), null, null));
        missing.Dispose();

        int count = images.Count;
        int columns = (int)MathF.Ceiling(MathF.Sqrt(count));
        int rows = (int)MathF.Ceiling(count / (float)columns);

        int atlasWidth = NextPow2(columns * PaddedTileSize);
        int atlasHeight = NextPow2(rows * PaddedTileSize);

        var atlas = new Image<Rgba32>(atlasWidth, atlasHeight);

        var regions = new Dictionary<string, AtlasRegion>(images.Count, StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < images.Count; i++)
        {
            int col = i % columns;
            int row = i / columns;
            int x = col * PaddedTileSize;
            int y = row * PaddedTileSize;

            var loaded = images[i];
            atlas.Mutate(ctx => ctx.DrawImage(loaded.AtlasImage, new SixLabors.ImageSharp.Point(x, y), 1f));

            var min = new Vector2((float)(x + Padding) / atlasWidth, (float)(y + Padding) / atlasHeight);
            var max = new Vector2((float)(x + Padding + TileSize) / atlasWidth, (float)(y + Padding + TileSize) / atlasHeight);
            regions[loaded.Name] = new AtlasRegion(min, max);

            if (loaded.AnimationDefinition is not null && loaded.SourceImage is not null)
            {
                animations.Add(new TextureAtlasAnimation(
                    loaded.Name,
                    regions[loaded.Name],
                    loaded.SourceImage,
                    x,
                    y,
                    PaddedTileSize,
                    TileSize,
                    loaded.AnimationDefinition.FrameSize,
                    loaded.AnimationDefinition.FrameColumns,
                    loaded.AnimationDefinition.FrameRows,
                    loaded.AnimationDefinition.Frames));
            }
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
            for (int level = 1; level < mipLevels; level++)
            {
                int w = Math.Max(1, atlasWidth >> level);
                int h = Math.Max(1, atlasHeight >> level);
                int cellSize = Math.Max(1, PaddedTileSize >> level);
                var levelAtlas = new Image<Rgba32>(w, h);

                levelAtlas.Mutate(ctx =>
                {
                    for (int i = 0; i < images.Count; i++)
                    {
                        int col = i % columns;
                        int row = i / columns;
                        int x = col * cellSize;
                        int y = row * cellSize;

                        using var resized = images[i].AtlasImage.Clone(imageCtx => imageCtx.Resize(cellSize, cellSize, KnownResamplers.Box));
                        ctx.DrawImage(resized, new SixLabors.ImageSharp.Point(x, y), 1f);
                    }
                });

                var data = new byte[w * h * 4];
                levelAtlas.CopyPixelDataTo(data);
                device.UpdateTexture(texture, data, 0, 0, 0, (uint)w, (uint)h, 1u, (uint)level, 0u);
                levelAtlas.Dispose();
            }
        }

        var samplerFilter = !renderConfig.UseMipmaps
            ? SamplerFilter.MinPoint_MagPoint_MipPoint
            : renderConfig.Anisotropy > 1
                ? SamplerFilter.Anisotropic
                : SamplerFilter.MinPoint_MagPoint_MipLinear;
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
            image.AtlasImage.Dispose();
        }
        atlas.Dispose();

        return new TextureAtlas(texture, sampler, regions, animations.ToArray(), mipLevels);
    }

    private static void NormalizeTexture(string file, Image<Rgba32> img, int tileSize, int maxTileSize, TextureAtlasAnimationDefinition? animationDefinition)
    {
        if (animationDefinition is not null)
        {
            var firstFrame = animationDefinition.GetFrameRectangle(0);
            img.Mutate(ctx => ctx.Crop(firstFrame));
            if (img.Width != tileSize || img.Height != tileSize)
            {
                img.Mutate(ctx => ctx.Resize(tileSize, tileSize, KnownResamplers.NearestNeighbor));
            }

            return;
        }

        if (img.Width == tileSize && img.Height > tileSize && img.Height % tileSize == 0)
        {
            img.Mutate(ctx => ctx.Crop(new SixLabors.ImageSharp.Rectangle(0, 0, tileSize, tileSize)));
            return;
        }

        int srcSize = Math.Max(img.Width, img.Height);
        if (srcSize > maxTileSize)
        {
            Log.Warn($"TextureAtlas: texture '{Path.GetFileNameWithoutExtension(file)}' is {img.Width}x{img.Height}. Resizing to {tileSize}x{tileSize}.");
        }

        if (img.Width != tileSize || img.Height != tileSize)
        {
            img.Mutate(ctx => ctx.Resize(tileSize, tileSize, KnownResamplers.NearestNeighbor));
        }
    }

    private static TextureAtlasAnimationDefinition? TryCreateAnimationDefinition(AssetStore assets, string file, int sourceWidth, int sourceHeight)
    {
        string metaPath = file + ".mcmeta";
        if (!File.Exists(metaPath))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(assets.ReadAllText(metaPath));
            if (!document.RootElement.TryGetProperty("animation", out var animation))
            {
                return null;
            }

            if (!TryDetermineFrameSize(sourceWidth, sourceHeight, out int frameSize))
            {
                return null;
            }

            int frameColumns = Math.Max(1, sourceWidth / frameSize);
            int frameRows = Math.Max(1, sourceHeight / frameSize);
            int defaultFrameTime = 1;
            if (animation.TryGetProperty("frametime", out var frametime) && frametime.TryGetInt32(out int parsedFrameTime))
            {
                defaultFrameTime = Math.Max(1, parsedFrameTime);
            }

            var frames = ParseAnimationFrames(animation, frameColumns, frameRows, defaultFrameTime);
            if (frames is null || frames.Count < 2)
            {
                return null;
            }

            return new TextureAtlasAnimationDefinition(frameSize, frameColumns, frameRows, frames);
        }
        catch (Exception ex)
        {
            Log.Warn($"TextureAtlas: failed to parse animation metadata '{metaPath}': {ex.Message}");
        }

        return null;
    }

    private static bool TryDetermineFrameSize(int width, int height, out int frameSize)
    {
        frameSize = 0;
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        if (width <= height && height % width == 0)
        {
            frameSize = width;
            return true;
        }

        if (height < width && width % height == 0)
        {
            frameSize = height;
            return true;
        }

        return false;
    }

    private static IReadOnlyList<TextureAtlasAnimationFrame>? ParseAnimationFrames(JsonElement animation, int frameColumns, int frameRows, int defaultFrameTime)
    {
        int slotCount = frameColumns * frameRows;
        if (slotCount < 1)
        {
            return null;
        }

        if (animation.TryGetProperty("frames", out var frames) &&
            frames.ValueKind == JsonValueKind.Array &&
            frames.GetArrayLength() > 0)
        {
            var result = new List<TextureAtlasAnimationFrame>(frames.GetArrayLength());
            foreach (var item in frames.EnumerateArray())
            {
                if (TryParseFrame(item, slotCount, defaultFrameTime, out var frame))
                {
                    result.Add(frame);
                }
            }

            return result.Count > 1 ? result : null;
        }

        if (slotCount < 2)
        {
            return null;
        }

        var sequential = new TextureAtlasAnimationFrame[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            sequential[i] = new TextureAtlasAnimationFrame(i, defaultFrameTime);
        }

        return sequential;
    }

    private static bool TryParseFrame(JsonElement item, int slotCount, int defaultFrameTime, out TextureAtlasAnimationFrame frame)
    {
        frame = default;
        if (item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out int index))
        {
            frame = new TextureAtlasAnimationFrame(Math.Clamp(index, 0, slotCount - 1), defaultFrameTime);
            return true;
        }

        if (item.ValueKind == JsonValueKind.Object &&
            item.TryGetProperty("index", out var indexElement) &&
            indexElement.TryGetInt32(out index))
        {
            int duration = defaultFrameTime;
            if (item.TryGetProperty("time", out var timeElement) && timeElement.TryGetInt32(out int parsedTime))
            {
                duration = Math.Max(1, parsedTime);
            }

            frame = new TextureAtlasAnimationFrame(Math.Clamp(index, 0, slotCount - 1), duration);
            return true;
        }

        return false;
    }

    private sealed class TextureAtlasAnimationDefinition
    {
        public int FrameSize { get; }
        public int FrameColumns { get; }
        public int FrameRows { get; }
        public IReadOnlyList<TextureAtlasAnimationFrame> Frames { get; }

        public TextureAtlasAnimationDefinition(int frameSize, int frameColumns, int frameRows, IReadOnlyList<TextureAtlasAnimationFrame> frames)
        {
            FrameSize = frameSize;
            FrameColumns = Math.Max(1, frameColumns);
            FrameRows = Math.Max(1, frameRows);
            Frames = frames;
        }

        public SixLabors.ImageSharp.Rectangle GetFrameRectangle(int frameIndex)
        {
            int safeIndex = Math.Clamp(frameIndex, 0, Frames.Count - 1);
            int slotIndex = Frames[safeIndex].Index;
            int x = (slotIndex % FrameColumns) * FrameSize;
            int y = (slotIndex / FrameColumns) * FrameSize;
            return new SixLabors.ImageSharp.Rectangle(x, y, FrameSize, FrameSize);
        }
    }

    private sealed class LoadedTexture
    {
        public string Name { get; }
        public Image<Rgba32> AtlasImage { get; }
        public Image<Rgba32>? SourceImage { get; }
        public TextureAtlasAnimationDefinition? AnimationDefinition { get; }

        public LoadedTexture(string name, Image<Rgba32> atlasImage, Image<Rgba32>? sourceImage, TextureAtlasAnimationDefinition? animationDefinition)
        {
            Name = name;
            AtlasImage = atlasImage;
            SourceImage = sourceImage;
            AnimationDefinition = animationDefinition;
        }
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

    private static Image<Rgba32> CreateBetaSmokeTexture()
    {
        var img = new Image<Rgba32>(TileSize, TileSize);
        float center = (TileSize - 1) * 0.5f;
        float radius = TileSize * 0.48f;

        img.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < TileSize; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(y);
                for (int x = 0; x < TileSize; x++)
                {
                    float dx = (x - center) / radius;
                    float dy = (y - center) / radius;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);

                    float core = Math.Clamp(1f - dist * 1.35f, 0f, 1f);
                    float lobe1 = Blob(x, y, center - 1.5f, center - 1.0f, radius * 0.72f);
                    float lobe2 = Blob(x, y, center + 2.2f, center - 0.2f, radius * 0.62f);
                    float lobe3 = Blob(x, y, center - 0.8f, center + 2.0f, radius * 0.58f);
                    float noise = 0.82f + 0.18f * Noise(x, y);

                    float alpha = Math.Clamp((core * 0.9f + lobe1 * 0.7f + lobe2 * 0.55f + lobe3 * 0.45f) * noise, 0f, 1f);
                    alpha *= alpha;

                    byte shade = (byte)Math.Clamp(146f + (1f - alpha) * 28f, 0f, 255f);
                    byte a = (byte)Math.Clamp(alpha * 255f, 0f, 255f);
                    row[x] = new Rgba32(shade, shade, shade, a);
                }
            }
        });

        return img;
    }

    private static float Blob(int x, int y, float centerX, float centerY, float radius)
    {
        float dx = (x - centerX) / radius;
        float dy = (y - centerY) / radius;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        return Math.Clamp(1f - dist, 0f, 1f);
    }

    private static float Noise(int x, int y)
    {
        unchecked
        {
            uint value = (uint)(x * 73856093) ^ (uint)(y * 19349663);
            value ^= value >> 13;
            value *= 1274126177u;
            return (value & 0xFFFF) / 65535f;
        }
    }

    internal static Image<Rgba32> PadTile(Image<Rgba32> src, int padding)
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
        for (int i = 0; i < _animations.Length; i++)
        {
            _animations[i].SourceImage.Dispose();
        }

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
