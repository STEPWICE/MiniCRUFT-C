using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniCRUFT.Core;

namespace MiniCRUFT.Game;

public sealed class SoundRegistry
{
    public IReadOnlyList<string> Sounds { get; }

    public SoundRegistry(AssetStore assets)
    {
        var files = assets.EnumerateFiles(".", "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Sounds = files;
    }
}
