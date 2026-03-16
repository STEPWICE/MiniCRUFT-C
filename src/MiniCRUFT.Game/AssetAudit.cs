using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public sealed class AssetAuditResult
{
    public int TextureCount { get; set; }
    public int SoundCount { get; set; }
    public int FontCount { get; set; }
    public int LocalizationCount { get; set; }
    public List<string> MissingTextures { get; } = new();
}

public sealed class AssetAudit
{
    private readonly AssetStore _assets;

    public AssetAudit(AssetStore assets)
    {
        _assets = assets;
    }

    public AssetAuditResult Run()
    {
        var result = new AssetAuditResult();
        BlockRegistry.Initialize();

        var blockDir = _assets.GetPath("minecraft", "textures", "block");
        var waterDir = _assets.GetPath("minecraft", "textures", "water");

        var textureNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (Directory.Exists(blockDir))
        {
            foreach (var file in Directory.EnumerateFiles(blockDir, "*.png", SearchOption.TopDirectoryOnly))
            {
                textureNames.Add(Path.GetFileNameWithoutExtension(file));
            }
        }
        if (Directory.Exists(waterDir))
        {
            foreach (var file in Directory.EnumerateFiles(waterDir, "*.png", SearchOption.TopDirectoryOnly))
            {
                textureNames.Add(Path.GetFileNameWithoutExtension(file));
            }
        }

        result.TextureCount = textureNames.Count;

        foreach (var def in BlockRegistry.All.Values)
        {
            if (def.Id == BlockId.Air)
            {
                continue;
            }

            CheckTexture(def.TextureTop, textureNames, result.MissingTextures);
            CheckTexture(def.TextureBottom, textureNames, result.MissingTextures);
            CheckTexture(def.TextureSide, textureNames, result.MissingTextures);
        }

        var soundFiles = _assets.EnumerateFiles(".", "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            .ToList();

        result.SoundCount = soundFiles.Count;

        var fontFiles = _assets.EnumerateFiles(".", "*.ttf", SearchOption.AllDirectories).ToList();
        result.FontCount = fontFiles.Count;

        var localizationFiles = _assets.EnumerateFiles(".", "lang_*.json", SearchOption.AllDirectories).ToList();
        result.LocalizationCount = localizationFiles.Count;

        return result;
    }

    private static void CheckTexture(string name, HashSet<string> available, List<string> missing)
    {
        if (!available.Contains(name))
        {
            missing.Add(name);
        }
    }
}
