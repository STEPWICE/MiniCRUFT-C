using System.Collections.Generic;
using MiniCRUFT.Core;
using Veldrid;

namespace MiniCRUFT.Renderer;

public sealed class HudSpriteAtlas
{
    public SpriteAtlas Atlas { get; }
    public SpriteRegion Hotbar { get; }
    public SpriteRegion HotbarSelection { get; }
    public SpriteRegion Crosshair { get; }
    public SpriteRegion HeartFull { get; }
    public SpriteRegion HeartHalf { get; }
    public SpriteRegion HeartEmpty { get; }

    private HudSpriteAtlas(SpriteAtlas atlas)
    {
        Atlas = atlas;
        Hotbar = atlas.GetRegion("hotbar");
        HotbarSelection = atlas.GetRegion("hotbar_selection");
        Crosshair = atlas.GetRegion("crosshair");
        HeartFull = atlas.GetRegion("heart_full");
        HeartHalf = atlas.GetRegion("heart_half");
        HeartEmpty = atlas.GetRegion("heart_empty");
    }

    public static HudSpriteAtlas Load(GraphicsDevice device, AssetStore assets)
    {
        var sources = new List<SpriteSource>
        {
            new("hotbar", "minecraft/gui/sprites/hud/hotbar.png"),
            new("hotbar_selection", "minecraft/gui/sprites/hud/hotbar_selection.png"),
            new("crosshair", "minecraft/gui/sprites/hud/crosshair.png"),
            new("heart_full", "minecraft/gui/sprites/hud/heart/full.png"),
            new("heart_half", "minecraft/gui/sprites/hud/heart/half.png"),
            new("heart_empty", "minecraft/gui/sprites/hud/heart/container.png")
        };

        var atlas = SpriteAtlas.Build(device, assets, sources, repeatSampler: false);
        return new HudSpriteAtlas(atlas);
    }
}
