using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using MiniCRUFT.Core;
using SharpFont;
using Veldrid;

namespace MiniCRUFT.Renderer;

public sealed class FreeTypeFontSystem : IDisposable
{
    private const int AtlasPadding = 1;
    private const int AtlasStartSize = 512;
    private const int AtlasMaxSize = 2048;

    private readonly GraphicsDevice _device;
    private readonly AssetStore _assets;
    private readonly UiConfig _uiConfig;
    private readonly Library _library;
    private readonly Face _face;
    private readonly Dictionary<int, FreeTypeFont> _fonts = new();
    private readonly string _fontPath;
    private bool _disposed;
    private bool _loggedPixelMode;

    public FreeTypeFontSystem(GraphicsDevice device, AssetStore assets, UiConfig uiConfig)
    {
        _device = device;
        _assets = assets;
        _uiConfig = uiConfig;
        _library = new Library();
        _fontPath = ResolveFontPath();
        _face = new Face(_library, _fontPath);
        SelectUnicodeCharmap(_face);
    }

    public FreeTypeFont GetFont(int pixelSize)
    {
        if (pixelSize < 8)
        {
            pixelSize = 8;
        }

        if (_fonts.TryGetValue(pixelSize, out var cached))
        {
            return cached;
        }

        var font = BuildFont(pixelSize);
        _fonts[pixelSize] = font;
        return font;
    }

    private FreeTypeFont BuildFont(int pixelSize)
    {
        _face.SetPixelSizes(0, (uint)pixelSize);

        int ascent = ToPixels(_face.Size.Metrics.Ascender);
        int descent = ToPixels(_face.Size.Metrics.Descender);
        int lineHeight = ToPixels(_face.Size.Metrics.Height);
        if (lineHeight <= 0)
        {
            lineHeight = pixelSize;
        }

        var glyphBitmaps = new List<GlyphBitmap>();
        int sampleMax = 0;
        int sampleCode = 'A';
        foreach (int codepoint in EnumerateCodepoints())
        {
            var glyph = LoadGlyph(codepoint);
            glyphBitmaps.Add(glyph);
            if ((codepoint == 'A' || codepoint == 'Ж') && glyph.Bitmap.Length > 0)
            {
                int max = 0;
                for (int i = 0; i < glyph.Bitmap.Length; i++)
                {
                    if (glyph.Bitmap[i] > max)
                    {
                        max = glyph.Bitmap[i];
                    }
                }
                if (max > sampleMax)
                {
                    sampleMax = max;
                    sampleCode = codepoint;
                }
            }
        }

        int atlasSize = AtlasStartSize;
        Dictionary<int, FreeTypeGlyph> glyphs;
        while (true)
        {
            if (TryPackGlyphs(glyphBitmaps, atlasSize, out glyphs))
            {
                break;
            }

            atlasSize *= 2;
            if (atlasSize > AtlasMaxSize)
            {
                throw new InvalidOperationException($"Font atlas exceeded max size ({AtlasMaxSize}).");
            }
        }

        var texture = new FontTexture(_device, atlasSize, atlasSize);
        texture.Clear();

        foreach (var glyph in glyphBitmaps)
        {
            if (glyph.Width == 0 || glyph.Height == 0)
            {
                continue;
            }

            if (!glyphs.TryGetValue(glyph.Codepoint, out var packed))
            {
                continue;
            }

            var rgba = ExpandToRgba(glyph.Bitmap, glyph.Width, glyph.Height);
            _device.UpdateTexture(
                texture.Texture,
                rgba,
                (uint)packed.AtlasRect.X,
                (uint)packed.AtlasRect.Y,
                0,
                (uint)packed.AtlasRect.Width,
                (uint)packed.AtlasRect.Height,
                1,
                0,
                0);
        }

        Log.Info($"UI font atlas built: size={atlasSize}, glyphs={glyphs.Count}, sampleMax={sampleMax} (U+{sampleCode:X4}).");
        return new FreeTypeFont(pixelSize, lineHeight, ascent, descent, texture, glyphs, _device);
    }

    private GlyphBitmap LoadGlyph(int codepoint)
    {
        _face.LoadChar((uint)codepoint, LoadFlags.Render, LoadTarget.Normal);
        var glyph = _face.Glyph;
        var bitmap = glyph.Bitmap;

        int width = bitmap.Width;
        int height = bitmap.Rows;
        int advance = ToPixels(glyph.Advance.X);
        int bearingX = glyph.BitmapLeft;
        int bearingY = glyph.BitmapTop;

        byte[] buffer = Array.Empty<byte>();
        if (width > 0 && height > 0)
        {
            buffer = CopyBitmap(bitmap, width, height);
            if (!_loggedPixelMode)
            {
                _loggedPixelMode = true;
                Log.Info($"UI font pixel mode: {bitmap.PixelMode}, pitch={bitmap.Pitch}, size={width}x{height}.");
            }
        }

        return new GlyphBitmap(codepoint, width, height, bearingX, bearingY, advance, buffer);
    }

    private static byte[] CopyBitmap(FTBitmap bitmap, int width, int height)
    {
        int pitch = bitmap.Pitch;
        int stride = Math.Abs(pitch);
        var output = new byte[width * height];
        if (stride == 0)
        {
            return output;
        }

        byte[] source;
        if (bitmap.BufferData != null && bitmap.BufferData.Length >= stride * height)
        {
            source = bitmap.BufferData;
        }
        else
        {
            source = new byte[stride * height];
            if (bitmap.Buffer != IntPtr.Zero)
            {
                Marshal.Copy(bitmap.Buffer, source, 0, source.Length);
            }
        }

        switch (bitmap.PixelMode)
        {
            case PixelMode.Mono:
                for (int y = 0; y < height; y++)
                {
                    int row = (pitch >= 0 ? y : (height - 1 - y)) * stride;
                    int dst = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        int b = source[row + (x >> 3)];
                        int mask = 0x80 >> (x & 7);
                        output[dst + x] = (b & mask) != 0 ? (byte)255 : (byte)0;
                    }
                }
                break;
            case PixelMode.Lcd:
                for (int y = 0; y < height; y++)
                {
                    int row = (pitch >= 0 ? y : (height - 1 - y)) * stride;
                    int dst = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        int idx = row + x * 3;
                        byte r = source[idx];
                        byte g = source[idx + 1];
                        byte b = source[idx + 2];
                        output[dst + x] = (byte)Math.Max(r, Math.Max(g, b));
                    }
                }
                break;
            case PixelMode.VerticalLcd:
                {
                    int realHeight = height / 3;
                    int outHeight = Math.Min(realHeight, height);
                    for (int y = 0; y < outHeight; y++)
                    {
                        int dst = y * width;
                        int row0 = ((pitch >= 0 ? (y * 3) : (height - 1 - (y * 3))) * stride);
                        int row1 = ((pitch >= 0 ? (y * 3 + 1) : (height - 1 - (y * 3 + 1))) * stride);
                        int row2 = ((pitch >= 0 ? (y * 3 + 2) : (height - 1 - (y * 3 + 2))) * stride);
                        for (int x = 0; x < width; x++)
                        {
                            byte r = source[row0 + x];
                            byte g = source[row1 + x];
                            byte b = source[row2 + x];
                            output[dst + x] = (byte)Math.Max(r, Math.Max(g, b));
                        }
                    }
                }
                break;
            default:
                for (int y = 0; y < height; y++)
                {
                    int row = (pitch >= 0 ? y : (height - 1 - y)) * stride;
                    int dst = y * width;
                    Array.Copy(source, row, output, dst, width);
                }
                break;
        }

        return output;
    }

    private static byte[] ExpandToRgba(byte[] mask, int width, int height)
    {
        int pixelCount = width * height;
        var rgba = new byte[pixelCount * 4];
        int src = 0;
        int dst = 0;
        for (int i = 0; i < pixelCount; i++)
        {
            byte a = mask[src++];
            rgba[dst++] = 255;
            rgba[dst++] = 255;
            rgba[dst++] = 255;
            rgba[dst++] = a;
        }
        return rgba;
    }

    private static int ToPixels(Fixed26Dot6 value)
    {
        return (int)MathF.Round(value.ToSingle());
    }

    private static int ToPixels(Fixed26Dot6? value)
    {
        return value.HasValue ? ToPixels(value.Value) : 0;
    }

    private static bool TryPackGlyphs(List<GlyphBitmap> glyphs, int atlasSize, out Dictionary<int, FreeTypeGlyph> packed)
    {
        packed = new Dictionary<int, FreeTypeGlyph>();

        int x = 0;
        int y = 0;
        int rowHeight = 0;

        foreach (var glyph in glyphs)
        {
            if (glyph.Width == 0 || glyph.Height == 0)
            {
                packed[glyph.Codepoint] = new FreeTypeGlyph(glyph.Codepoint, glyph.Width, glyph.Height, glyph.BearingX, glyph.BearingY, glyph.Advance, new System.Drawing.Rectangle(0, 0, 0, 0));
                continue;
            }

            if (x + glyph.Width > atlasSize)
            {
                x = 0;
                y += rowHeight + AtlasPadding;
                rowHeight = 0;
            }

            if (y + glyph.Height > atlasSize)
            {
                return false;
            }

            var rect = new System.Drawing.Rectangle(x, y, glyph.Width, glyph.Height);
            packed[glyph.Codepoint] = new FreeTypeGlyph(glyph.Codepoint, glyph.Width, glyph.Height, glyph.BearingX, glyph.BearingY, glyph.Advance, rect);

            x += glyph.Width + AtlasPadding;
            rowHeight = Math.Max(rowHeight, glyph.Height);
        }

        return true;
    }

    private string ResolveFontPath()
    {
        var tried = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new[]
        {
            _uiConfig.FontFile,
            "minecraft/font/NotoSans-Regular.ttf",
            "minecraft/font/consolas.ttf"
        };

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate) || !tried.Add(candidate))
            {
                continue;
            }

            string path = _assets.GetPath(candidate);
            if (File.Exists(path))
            {
                Log.Info($"UI font loaded: {Path.GetFileName(path)}");
                return path;
            }
        }

        throw new FileNotFoundException("UI font file not found.", _uiConfig.FontFile);
    }

    private static void SelectUnicodeCharmap(Face face)
    {
        try
        {
            face.SelectCharmap(Encoding.Unicode);
        }
        catch
        {
            if (face.CharmapsCount > 0)
            {
                face.SetCharmap(face.CharMaps[0]);
            }
        }
    }

    private static IEnumerable<int> EnumerateCodepoints()
    {
        for (int c = 0x20; c <= 0x7E; c++)
        {
            yield return c;
        }

        for (int c = 0x0400; c <= 0x04FF; c++)
        {
            yield return c;
        }

        yield return 0x00A0; // non-breaking space
        yield return 0x2116; // Numero sign
        yield return 0x20BD; // Ruble sign
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var font in _fonts.Values)
        {
            font.Dispose();
        }
        _fonts.Clear();
        _face.Dispose();
        _library.Dispose();
        _disposed = true;
    }

    public void Flush(CommandList commandList)
    {
        foreach (var font in _fonts.Values)
        {
            font.Flush(commandList);
        }
    }

    private readonly struct GlyphBitmap
    {
        public readonly int Codepoint;
        public readonly int Width;
        public readonly int Height;
        public readonly int BearingX;
        public readonly int BearingY;
        public readonly int Advance;
        public readonly byte[] Bitmap;

        public GlyphBitmap(int codepoint, int width, int height, int bearingX, int bearingY, int advance, byte[] bitmap)
        {
            Codepoint = codepoint;
            Width = width;
            Height = height;
            BearingX = bearingX;
            BearingY = bearingY;
            Advance = advance;
            Bitmap = bitmap;
        }
    }
}
