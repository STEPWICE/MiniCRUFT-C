using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniCRUFT.Core;
using MiniCRUFT.Renderer;
using MiniCRUFT.World;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MiniCRUFT.Game;

public sealed class AssetAuditResult
{
    public int TextureCount { get; set; }
    public int BlockTextureCount { get; set; }
    public int WaterTextureCount { get; set; }
    public int ItemTextureCount { get; set; }
    public int EntityTextureCount { get; set; }
    public int HudTextureCount { get; set; }
    public int AtmosphereTextureCount { get; set; }
    public int ColormapCount { get; set; }
    public int SoundCount { get; set; }
    public int MobSoundCount { get; set; }
    public int FontCount { get; set; }
    public int LocalizationCount { get; set; }
    public List<string> MissingTextures { get; } = new();
    public List<string> PlaceholderTextures { get; } = new();
    public List<string> MobModelIssues { get; } = new();
}

public sealed class AssetAudit
{
    private readonly AssetStore _assets;

    public AssetAudit(AssetStore assets)
    {
        _assets = assets;
    }

    public AssetAuditResult Run(bool strictBetaMode = false)
    {
        var result = new AssetAuditResult();
        BlockRegistry.Initialize();

        var blockDir = _assets.GetPath("minecraft", "textures", "block");
        var waterDir = _assets.GetPath("minecraft", "textures", "water");
        var itemDir = _assets.GetPath("minecraft", "textures", "item");
        var entityDir = _assets.GetPath("minecraft", "textures", "entity");
        var hudDir = _assets.GetPath("minecraft", "gui", "sprites", "hud");
        var atmosphereDir = _assets.GetPath("minecraft", "textures", "environment");
        var colormapDir = _assets.GetPath("minecraft", "textures", "colormap");
        var fontDir = _assets.GetPath("minecraft", "font");
        var fireSoundDir = _assets.GetPath("minecraft", "sounds", "fire");
        var mobSoundDir = _assets.GetPath("minecraft", "sounds", "mob");
        var randomSoundDir = _assets.GetPath("minecraft", "sounds", "random");

        var blockTextures = CollectTextureNames(blockDir, out int blockTextureCount);
        var waterTextures = CollectTextureNames(waterDir, out int waterTextureCount);
        var itemTextures = CollectTextureNames(itemDir, out _);
        result.BlockTextureCount = blockTextureCount;
        result.WaterTextureCount = waterTextureCount;
        result.ItemTextureCount = CountTextureFiles(itemDir);
        result.EntityTextureCount = CountTextureFiles(entityDir);
        result.HudTextureCount = CountTextureFiles(hudDir);
        result.AtmosphereTextureCount = CountTextureFiles(atmosphereDir);
        result.ColormapCount = CountTextureFiles(colormapDir);
        result.TextureCount = result.BlockTextureCount +
                              result.WaterTextureCount +
                              result.ItemTextureCount +
                              result.EntityTextureCount +
                              result.HudTextureCount +
                              result.AtmosphereTextureCount +
                              result.ColormapCount;

        var availableTextures = new HashSet<string>(blockTextures, StringComparer.OrdinalIgnoreCase);
        availableTextures.UnionWith(waterTextures);

        foreach (var def in BlockRegistry.All.Values)
        {
            if (def.Id == BlockId.Air || !def.IsPlaceable)
            {
                continue;
            }

            CheckTexture(def.TextureTop, availableTextures, result.MissingTextures);
            CheckTexture(def.TextureBottom, availableTextures, result.MissingTextures);
            CheckTexture(def.TextureSide, availableTextures, result.MissingTextures);
        }

        foreach (var def in BlockRegistry.All.Values)
        {
            if (def.Id == BlockId.Air || def.IsPlaceable || def.Id == BlockId.Fire)
            {
                continue;
            }

            CheckTexture(def.TextureTop, itemTextures, result.MissingTextures);
        }

        CheckTexture("fire_0", availableTextures, result.MissingTextures);
        CheckTexture("fire_1", availableTextures, result.MissingTextures);

        CheckRequiredFile(Path.Combine(hudDir, "hotbar.png"), "minecraft/gui/sprites/hud/hotbar.png", result.MissingTextures);
        CheckRequiredFile(Path.Combine(hudDir, "hotbar_selection.png"), "minecraft/gui/sprites/hud/hotbar_selection.png", result.MissingTextures);
        CheckRequiredFile(Path.Combine(hudDir, "crosshair.png"), "minecraft/gui/sprites/hud/crosshair.png", result.MissingTextures);
        CheckRequiredFile(Path.Combine(hudDir, "heart", "full.png"), "minecraft/gui/sprites/hud/heart/full.png", result.MissingTextures);
        CheckRequiredFile(Path.Combine(hudDir, "heart", "half.png"), "minecraft/gui/sprites/hud/heart/half.png", result.MissingTextures);
        CheckRequiredFile(Path.Combine(hudDir, "heart", "container.png"), "minecraft/gui/sprites/hud/heart/container.png", result.MissingTextures);

        CheckRequiredFile(Path.Combine(atmosphereDir, "clouds.png"), "minecraft/textures/environment/clouds.png", result.MissingTextures);
        CheckRequiredFile(Path.Combine(atmosphereDir, "sun.png"), "minecraft/textures/environment/sun.png", result.MissingTextures);
        CheckRequiredFile(Path.Combine(atmosphereDir, "moon.png"), "minecraft/textures/environment/moon.png", result.MissingTextures);

        var mobTextureSizes = new Dictionary<string, (int Width, int Height)>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in MobModelCatalog.GetTextureSources())
        {
            string path = _assets.GetPath(source.RelativePath);
            CheckRequiredFile(path, source.RelativePath, result.MissingTextures);
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                using var stream = File.OpenRead(path);
                using var image = Image.Load<Rgba32>(stream);
                mobTextureSizes[source.Name] = (image.Width, image.Height);
            }
            catch (Exception ex)
            {
                result.MissingTextures.Add(source.RelativePath);
                Log.Warn($"Asset audit failed to inspect {source.RelativePath}: {ex.Message}");
            }
        }

        ValidateMobModels(mobTextureSizes, result.MobModelIssues);

        CheckRequiredFile(Path.Combine(mobSoundDir, "zombie", "death.ogg"), "minecraft/sounds/mob/zombie/death.ogg", result.MissingTextures);
        CheckRequiredFile(Path.Combine(mobSoundDir, "zombie", "hurt1.ogg"), "minecraft/sounds/mob/zombie/hurt1.ogg", result.MissingTextures);
        CheckRequiredFile(Path.Combine(mobSoundDir, "zombie", "step1.ogg"), "minecraft/sounds/mob/zombie/step1.ogg", result.MissingTextures);
        CheckRequiredFile(Path.Combine(mobSoundDir, "creeper", "death.ogg"), "minecraft/sounds/mob/creeper/death.ogg", result.MissingTextures);
        CheckRequiredFile(Path.Combine(mobSoundDir, "cow", "say1.ogg"), "minecraft/sounds/mob/cow/say1.ogg", result.MissingTextures);
        CheckRequiredFile(Path.Combine(mobSoundDir, "sheep", "say1.ogg"), "minecraft/sounds/mob/sheep/say1.ogg", result.MissingTextures);
        CheckRequiredFile(Path.Combine(mobSoundDir, "chicken", "say1.ogg"), "minecraft/sounds/mob/chicken/say1.ogg", result.MissingTextures);
        CheckRequiredFile(Path.Combine(fireSoundDir, "ignite.ogg"), "minecraft/sounds/fire/ignite.ogg", result.MissingTextures);
        CheckRequiredFile(Path.Combine(fireSoundDir, "fire.ogg"), "minecraft/sounds/fire/fire.ogg", result.MissingTextures);
        CheckRequiredFile(Path.Combine(randomSoundDir, "fuse.ogg"), "minecraft/sounds/random/fuse.ogg", result.MissingTextures);
        CheckRequiredFile(Path.Combine(randomSoundDir, "old_explode.ogg"), "minecraft/sounds/random/old_explode.ogg", result.MissingTextures);
        CheckRequiredFile(Path.Combine(randomSoundDir, "explode1.ogg"), "minecraft/sounds/random/explode1.ogg", result.MissingTextures);
        CheckRequiredFile(Path.Combine(randomSoundDir, "explode2.ogg"), "minecraft/sounds/random/explode2.ogg", result.MissingTextures);
        CheckRequiredFile(Path.Combine(randomSoundDir, "explode3.ogg"), "minecraft/sounds/random/explode3.ogg", result.MissingTextures);
        CheckRequiredFile(Path.Combine(randomSoundDir, "explode4.ogg"), "minecraft/sounds/random/explode4.ogg", result.MissingTextures);
        CheckRequiredFile(Path.Combine(randomSoundDir, "glass1.ogg"), "minecraft/sounds/random/glass1.ogg", result.MissingTextures);
        CheckRequiredFile(Path.Combine(randomSoundDir, "glass2.ogg"), "minecraft/sounds/random/glass2.ogg", result.MissingTextures);
        CheckRequiredFile(Path.Combine(randomSoundDir, "glass3.ogg"), "minecraft/sounds/random/glass3.ogg", result.MissingTextures);

        CheckRequiredFile(Path.Combine(fontDir, "consolas.ttf"), "minecraft/font/consolas.ttf", result.MissingTextures);

        CheckColorMap(Path.Combine(colormapDir, "grass.png"), "minecraft/textures/colormap/grass.png", result);
        CheckColorMap(Path.Combine(colormapDir, "foliage.png"), "minecraft/textures/colormap/foliage.png", result);

        result.SoundCount = _assets.EnumerateFiles(".", "*.*", SearchOption.AllDirectories)
            .Count(path => path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase));
        result.MobSoundCount = CountSoundFiles(mobSoundDir);

        result.FontCount = _assets.EnumerateFiles(".", "*.ttf", SearchOption.AllDirectories).Count();

        result.LocalizationCount = _assets.EnumerateFiles(".", "lang_*.json", SearchOption.AllDirectories).Count();

        if (strictBetaMode && (result.MissingTextures.Count > 0 || result.PlaceholderTextures.Count > 0 || result.MobModelIssues.Count > 0))
        {
            var missing = result.MissingTextures.Count > 0 ? string.Join(", ", result.MissingTextures.Distinct().Take(12)) : "<none>";
            var placeholders = result.PlaceholderTextures.Count > 0 ? string.Join(", ", result.PlaceholderTextures.Distinct().Take(12)) : "<none>";
            var modelIssues = result.MobModelIssues.Count > 0 ? string.Join(", ", result.MobModelIssues.Distinct().Take(12)) : "<none>";
            throw new InvalidOperationException($"Strict beta asset audit failed. Missing: {missing}. Placeholder: {placeholders}. Mob models: {modelIssues}.");
        }

        return result;
    }

    private static HashSet<string> CollectTextureNames(string directory, out int count)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(directory))
        {
            count = 0;
            return names;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*.png", SearchOption.TopDirectoryOnly))
        {
            names.Add(Path.GetFileNameWithoutExtension(file));
        }

        count = names.Count;
        return names;
    }

    private static int CountTextureFiles(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return 0;
        }

        return Directory.EnumerateFiles(directory, "*.png", SearchOption.AllDirectories).Count();
    }

    private static int CountSoundFiles(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return 0;
        }

        return Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
            .Count(path => path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase));
    }

    private static void CheckTexture(string name, HashSet<string> available, List<string> missing)
    {
        if (!available.Contains(name))
        {
            missing.Add(name);
        }
    }

    private static void CheckRequiredFile(string path, string label, List<string> missing)
    {
        if (!File.Exists(path))
        {
            missing.Add(label);
        }
    }

    private static void CheckColorMap(string path, string label, AssetAuditResult result)
    {
        if (!File.Exists(path))
        {
            result.MissingTextures.Add(label);
            return;
        }

        try
        {
            using var stream = File.OpenRead(path);
            using var image = Image.Load<Rgba32>(stream);
            if (IsFlatPlaceholder(image))
            {
                result.PlaceholderTextures.Add(label);
            }
        }
        catch (Exception ex)
        {
            result.MissingTextures.Add(label);
            Log.Warn($"Asset audit failed to inspect {label}: {ex.Message}");
        }
    }

    private static bool IsFlatPlaceholder(Image<Rgba32> image)
    {
        if (image.Width == 0 || image.Height == 0)
        {
            return true;
        }

        var first = image[0, 0];
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                if (!image[x, y].Equals(first))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void ValidateMobModels(IReadOnlyDictionary<string, (int Width, int Height)> textureSizes, List<string> issues)
    {
        foreach (var definition in MobModelCatalog.All.Values)
        {
            if (!textureSizes.TryGetValue(definition.TextureName, out var baseSize))
            {
                issues.Add($"{definition.Type}: missing texture '{definition.TextureName}' for model validation.");
                continue;
            }

            if (definition.OverlayTextureName is string overlayName)
            {
                if (!textureSizes.TryGetValue(overlayName, out var overlaySize))
                {
                    issues.Add($"{definition.Type}: missing overlay texture '{overlayName}'.");
                }
                else if (overlaySize.Width != baseSize.Width || overlaySize.Height != baseSize.Height)
                {
                    issues.Add($"{definition.Type}: overlay texture '{overlayName}' is {overlaySize.Width}x{overlaySize.Height}, expected {baseSize.Width}x{baseSize.Height}.");
                }
            }

            float maxY = float.MinValue;

            foreach (var box in definition.Boxes)
            {
                if (box.SizePixels.X <= 0f || box.SizePixels.Y <= 0f || box.SizePixels.Z <= 0f)
                {
                    issues.Add($"{definition.Type}: box '{box.Kind}' has invalid size {box.SizePixels}.");
                    continue;
                }

                float requiredWidth = box.TextureOriginPixels.X + (box.SizePixels.X * 2f) + (box.SizePixels.Z * 2f);
                float requiredHeight = box.TextureOriginPixels.Y + box.SizePixels.Y + box.SizePixels.Z;
                if (requiredWidth > baseSize.Width + 0.001f || requiredHeight > baseSize.Height + 0.001f)
                {
                    issues.Add($"{definition.Type}: box '{box.Kind}' exceeds texture bounds. Needs {MathF.Ceiling(requiredWidth)}x{MathF.Ceiling(requiredHeight)}, texture is {baseSize.Width}x{baseSize.Height}.");
                }

                maxY = Math.Max(maxY, box.MinPixels.Y + box.SizePixels.Y);
            }

            if (definition.Boxes.Count == 0)
            {
                issues.Add($"{definition.Type}: model has no boxes.");
                continue;
            }

            if (definition.BaseHeightPixels + 0.001f < maxY)
            {
                issues.Add($"{definition.Type}: model height {MathF.Ceiling(maxY)} exceeds BaseHeightPixels {definition.BaseHeightPixels}.");
            }
        }
    }
}
