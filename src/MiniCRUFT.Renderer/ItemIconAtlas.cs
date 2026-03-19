using System;
using System.Collections.Generic;
using System.IO;
using MiniCRUFT.Core;
using MiniCRUFT.World;
using Veldrid;

namespace MiniCRUFT.Renderer;

public sealed class ItemIconAtlas : IDisposable
{
    private readonly Dictionary<BlockId, SpriteRegion> _regions;
    public SpriteAtlas Atlas { get; }

    private ItemIconAtlas(SpriteAtlas atlas, Dictionary<BlockId, SpriteRegion> regions)
    {
        Atlas = atlas;
        _regions = regions;
    }

    public SpriteRegion GetRegion(BlockId id)
    {
        if (_regions.TryGetValue(id, out var region))
        {
            return region;
        }

        return Atlas.GetRegion("missing");
    }

    public static ItemIconAtlas Build(GraphicsDevice device, AssetStore assets)
    {
        BlockRegistry.Initialize();
        var sources = new List<SpriteSource>(BlockRegistry.All.Count);
        var missing = new HashSet<BlockId>(BlockRegistry.All.Count);
        var regions = new Dictionary<BlockId, SpriteRegion>(BlockRegistry.All.Count);

        foreach (var def in BlockRegistry.All.Values)
        {
            if (def.Id == BlockId.Air)
            {
                continue;
            }

            string tex = PickIconTexture(def);
            if (!TryResolveTexturePath(assets, tex, out var path))
            {
                missing.Add(def.Id);
                continue;
            }

            sources.Add(new SpriteSource(def.Id.ToString(), path));
        }

        var atlas = SpriteAtlas.Build(device, assets, sources, repeatSampler: false, maxSpriteSize: 16);

        foreach (var def in BlockRegistry.All.Values)
        {
            if (def.Id == BlockId.Air)
            {
                continue;
            }

            if (missing.Contains(def.Id))
            {
                regions[def.Id] = atlas.GetRegion("missing");
            }
            else
            {
                regions[def.Id] = atlas.GetRegion(def.Id.ToString());
            }
        }

        return new ItemIconAtlas(atlas, regions);
    }

    private static string PickIconTexture(BlockDefinition def)
    {
        if (def.Id == BlockId.Grass)
        {
            return def.TextureTop;
        }

        if (LiquidBlocks.IsLiquid(def.Id))
        {
            return def.TextureTop;
        }

        if (def.RenderMode == RenderMode.Cross || def.RenderMode == RenderMode.Cutout || def.RenderMode == RenderMode.Torch)
        {
            return def.TextureSide;
        }

        if (!def.TextureTop.Equals(def.TextureSide, StringComparison.OrdinalIgnoreCase))
        {
            return def.TextureTop;
        }

        return def.TextureSide;
    }

    private static bool TryResolveTexturePath(AssetStore assets, string name, out string path)
    {
        var itemPath = assets.GetPath("minecraft", "textures", "item", name + ".png");
        if (File.Exists(itemPath))
        {
            path = itemPath;
            return true;
        }

        var blockPath = assets.GetPath("minecraft", "textures", "block", name + ".png");
        if (File.Exists(blockPath))
        {
            path = blockPath;
            return true;
        }

        var waterPath = assets.GetPath("minecraft", "textures", "water", name + ".png");
        if (File.Exists(waterPath))
        {
            path = waterPath;
            return true;
        }

        path = string.Empty;
        return false;
    }

    public void Dispose()
    {
        Atlas.Dispose();
    }
}
