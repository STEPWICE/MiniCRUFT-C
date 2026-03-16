using System;
using System.Collections.Generic;
using System.IO;

namespace MiniCRUFT.Core;

public sealed class AssetStore
{
    private readonly Dictionary<string, byte[]> _bytesCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _textCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();

    public string RootPath { get; }
    public string MinecraftPath => Path.Combine(RootPath, "minecraft");
    public string TexturesPath => Path.Combine(MinecraftPath, "textures");
    public string GuiPath => Path.Combine(MinecraftPath, "gui");
    public string FontPath => Path.Combine(MinecraftPath, "font");
    public string SoundsPath => Path.Combine(MinecraftPath, "sounds");

    public AssetStore(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Asset root path is empty.", nameof(rootPath));
        }

        RootPath = Path.GetFullPath(rootPath);
    }

    public string GetPath(params string[] parts)
    {
        if (parts.Length == 0)
        {
            return RootPath;
        }

        string combined = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            combined = Path.Combine(combined, parts[i]);
        }

        return Path.IsPathRooted(combined) ? combined : Path.Combine(RootPath, combined);
    }

    public IEnumerable<string> EnumerateFiles(string relativeOrAbsoluteDir, string pattern, SearchOption option)
    {
        string path = GetPath(relativeOrAbsoluteDir);
        if (!Directory.Exists(path))
        {
            return Array.Empty<string>();
        }

        return Directory.EnumerateFiles(path, pattern, option);
    }

    public byte[] ReadAllBytes(string relativeOrAbsolutePath)
    {
        string path = GetPath(relativeOrAbsolutePath);
        lock (_sync)
        {
            if (_bytesCache.TryGetValue(path, out var cached))
            {
                return cached;
            }
        }

        var data = File.ReadAllBytes(path);
        lock (_sync)
        {
            _bytesCache[path] = data;
        }
        return data;
    }

    public string ReadAllText(string relativeOrAbsolutePath)
    {
        string path = GetPath(relativeOrAbsolutePath);
        lock (_sync)
        {
            if (_textCache.TryGetValue(path, out var cached))
            {
                return cached;
            }
        }

        var text = File.ReadAllText(path);
        lock (_sync)
        {
            _textCache[path] = text;
        }
        return text;
    }

    public Stream OpenStream(string relativeOrAbsolutePath)
    {
        string path = GetPath(relativeOrAbsolutePath);
        return File.OpenRead(path);
    }
}
