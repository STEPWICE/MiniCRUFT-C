using System.Collections.Generic;
using System.Numerics;
using MiniCRUFT.Core;
using Veldrid;

namespace MiniCRUFT.Renderer;

public sealed class TntFuseRenderer : IDisposable
{
    private const float FuseMinSizePx = 8f;
    private const float FuseMaxSizePx = 40f;
    private const float FuseBaseScale = 16f;
    private const float FuseBaseOffsetY = 1.10f;

    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteRegion _flame0;
    private readonly SpriteRegion _flame1;

    public TntFuseRenderer(GraphicsDevice device, TextureAtlas atlas)
    {
        _spriteBatch = new SpriteBatch(device, atlas.Texture, atlas.Sampler);
        _flame0 = ToSpriteRegion(atlas.GetRegion("fire_0"));
        _flame1 = ToSpriteRegion(atlas.GetRegion("fire_1"));
    }

    public void Draw(CommandList commandList, Camera camera, int width, int height, IReadOnlyList<TntRenderInstance> tnts)
    {
        if (tnts.Count == 0)
        {
            return;
        }

        _spriteBatch.Begin(width, height);

        var viewProj = camera.View * camera.Projection;
        float focalLength = height / (2f * MathF.Tan(MathF.PI * camera.Fov / 360f));

        for (int i = 0; i < tnts.Count; i++)
        {
            var tnt = tnts[i];
            var clip = Vector4.Transform(new Vector4(tnt.Position, 1f), viewProj);
            if (clip.W <= 0.001f)
            {
                continue;
            }

            float invW = 1f / clip.W;
            float ndcX = clip.X * invW;
            float ndcY = clip.Y * invW;
            if (ndcX < -1.1f || ndcX > 1.1f || ndcY < -1.1f || ndcY > 1.1f)
            {
                continue;
            }

            float depth = MathF.Max(clip.W, 0.1f);
            float fuseDuration = Math.Max(tnt.FuseDuration, 0.1f);
            float fuseProgress = 1f - Math.Clamp(tnt.FuseRemaining / fuseDuration, 0f, 1f);
            float pulse = 0.5f + 0.5f * MathF.Sin(tnt.FuseRemaining * 28f + tnt.Position.X * 1.7f + tnt.Position.Z * 2.3f);
            float sizePx = Math.Clamp((FuseBaseScale + fuseProgress * 14f) * focalLength / depth, FuseMinSizePx, FuseMaxSizePx);

            var screen = new Vector2(
                (ndcX * 0.5f + 0.5f) * width,
                (1f - (ndcY * 0.5f + 0.5f)) * height);
            var basePos = new Vector2(screen.X - sizePx * 0.5f, screen.Y - sizePx * FuseBaseOffsetY);

            var glowColor = new Vector4(0.95f, 0.25f + 0.25f * pulse, 0.05f, 0.20f + 0.18f * pulse);
            var flameColor = new Vector4(1f, 0.72f + 0.22f * pulse, 0.1f, 0.88f);
            var emberColor = new Vector4(1f, 0.9f, 0.4f, 0.45f + 0.25f * pulse);

            _spriteBatch.Draw(_flame0, basePos + new Vector2(0f, -sizePx * 0.08f), new Vector2(sizePx * 1.02f, sizePx * 1.35f), glowColor);
            _spriteBatch.Draw(_flame1, basePos + new Vector2(sizePx * 0.08f, -sizePx * 0.20f), new Vector2(sizePx * 0.72f, sizePx * 1.08f), flameColor);

            if (fuseProgress > 0.72f)
            {
                _spriteBatch.Draw(_flame0, basePos + new Vector2(sizePx * 0.18f, -sizePx * 0.42f), new Vector2(sizePx * 0.25f, sizePx * 0.32f), emberColor);
            }
        }

        _spriteBatch.Flush(commandList);
    }

    public void Dispose()
    {
        _spriteBatch.Dispose();
    }

    private static SpriteRegion ToSpriteRegion(AtlasRegion region)
    {
        return new SpriteRegion(region.Min, region.Max, new Vector2(16f, 16f));
    }
}
