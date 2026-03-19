using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;

namespace MiniCRUFT.Renderer;

public sealed class FreeTypeFont : IDisposable
{
    private readonly Dictionary<int, FreeTypeGlyph> _glyphs;
    private readonly FreeTypeGlyph _fallback;
    private readonly SpriteBatch _batch;
    private readonly Sampler _sampler;
    private readonly SpriteRegion _region;
    private bool _batchActive;
    private int _batchWidth;
    private int _batchHeight;

    public int Size { get; }
    public int LineHeight { get; }
    public int Ascent { get; }
    public int Descent { get; }
    public FontTexture Texture { get; }

    public FreeTypeFont(int size, int lineHeight, int ascent, int descent, FontTexture texture, Dictionary<int, FreeTypeGlyph> glyphs, GraphicsDevice device)
    {
        Size = size;
        LineHeight = Math.Max(1, lineHeight);
        Ascent = ascent;
        Descent = descent;
        Texture = texture;
        _glyphs = glyphs;
        _glyphs.TryGetValue('?', out _fallback);
        _sampler = device.ResourceFactory.CreateSampler(new SamplerDescription
        {
            AddressModeU = SamplerAddressMode.Clamp,
            AddressModeV = SamplerAddressMode.Clamp,
            AddressModeW = SamplerAddressMode.Clamp,
            Filter = SamplerFilter.MinPoint_MagPoint_MipPoint
        });
        _batch = new SpriteBatch(device, texture.Texture, _sampler);
        _region = new SpriteRegion(Vector2.Zero, Vector2.One, new Vector2(texture.Width, texture.Height));
    }

    public Vector2 MeasureString(string text)
    {
        return MeasureString(text.AsSpan());
    }

    public Vector2 MeasureString(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return Vector2.Zero;
        }

        float maxWidth = 0f;
        float lineWidth = 0f;
        int lines = 1;

        foreach (char ch in text)
        {
            if (ch == '\r')
            {
                continue;
            }

            if (ch == '\n')
            {
                maxWidth = MathF.Max(maxWidth, lineWidth);
                lineWidth = 0f;
                lines++;
                continue;
            }

            var glyph = ResolveGlyph(ch);
            lineWidth += glyph.Advance;
        }

        maxWidth = MathF.Max(maxWidth, lineWidth);
        float height = lines * LineHeight;
        return new Vector2(maxWidth, height);
    }

    public void DrawText(int width, int height, string text, Vector2 position, Vector4 colorSrgb, float scale)
    {
        DrawText(width, height, text.AsSpan(), position, colorSrgb, scale);
    }

    public void DrawText(int width, int height, ReadOnlySpan<char> text, Vector2 position, Vector4 colorSrgb, float scale)
    {
        if (text.IsEmpty)
        {
            return;
        }

        EnsureBatch(width, height);

        float penX = position.X;
        float penY = position.Y + Ascent * scale;
        float startX = position.X;

        foreach (char ch in text)
        {
            if (ch == '\r')
            {
                continue;
            }

            if (ch == '\n')
            {
                penX = startX;
                penY += LineHeight * scale;
                continue;
            }

            var glyph = ResolveGlyph(ch);
            if (glyph.Width > 0 && glyph.Height > 0)
            {
                float gx = penX + glyph.BearingX * scale;
                float gy = penY - glyph.BearingY * scale;
                var uvOffset = new Vector2(
                    glyph.AtlasRect.X / (float)Texture.Width,
                    glyph.AtlasRect.Y / (float)Texture.Height);
                var uvScale = new Vector2(
                    glyph.AtlasRect.Width / (float)Texture.Width,
                    glyph.AtlasRect.Height / (float)Texture.Height);
                _batch.Draw(_region, new Vector2(gx, gy), new Vector2(glyph.Width * scale, glyph.Height * scale), colorSrgb, uvOffset, uvScale);
            }

            penX += glyph.Advance * scale;
        }
    }

    public void Flush(CommandList commandList)
    {
        if (!_batchActive)
        {
            return;
        }

        _batch.Flush(commandList);
        _batchActive = false;
    }

    private void EnsureBatch(int width, int height)
    {
        if (_batchActive && _batchWidth == width && _batchHeight == height)
        {
            return;
        }

        _batchWidth = width;
        _batchHeight = height;
        _batch.Begin(width, height);
        _batchActive = true;
    }

    private FreeTypeGlyph ResolveGlyph(int codepoint)
    {
        return _glyphs.TryGetValue(codepoint, out var glyph) ? glyph : _fallback;
    }

    public void Dispose()
    {
        _batch.Dispose();
        _sampler.Dispose();
        Texture.Dispose();
    }
}

public readonly struct FreeTypeGlyph
{
    public readonly int Codepoint;
    public readonly int Width;
    public readonly int Height;
    public readonly int BearingX;
    public readonly int BearingY;
    public readonly int Advance;
    public readonly System.Drawing.Rectangle AtlasRect;

    public FreeTypeGlyph(int codepoint, int width, int height, int bearingX, int bearingY, int advance, System.Drawing.Rectangle atlasRect)
    {
        Codepoint = codepoint;
        Width = width;
        Height = height;
        BearingX = bearingX;
        BearingY = bearingY;
        Advance = advance;
        AtlasRect = atlasRect;
    }
}
