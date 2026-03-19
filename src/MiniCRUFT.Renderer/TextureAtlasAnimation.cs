using System;
using System.Collections.Generic;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MiniCRUFT.Renderer;

public sealed class TextureAtlasAnimation
{
    public string Name { get; }
    public AtlasRegion Region { get; }
    public Image<Rgba32> SourceImage { get; }
    public int AtlasX { get; }
    public int AtlasY { get; }
    public int PaddedSize { get; }
    public int TileSize { get; }
    public int FrameSize { get; }
    public int FrameColumns { get; }
    public int FrameRows { get; }
    public IReadOnlyList<TextureAtlasAnimationFrame> Frames { get; }

    public int FrameCount => Frames.Count;
    public int Padding => Math.Max(0, (PaddedSize - TileSize) / 2);

    public TextureAtlasAnimation(
        string name,
        AtlasRegion region,
        Image<Rgba32> sourceImage,
        int atlasX,
        int atlasY,
        int paddedSize,
        int tileSize,
        int frameSize,
        int frameColumns,
        int frameRows,
        IReadOnlyList<TextureAtlasAnimationFrame> frames)
    {
        Name = name;
        Region = region;
        SourceImage = sourceImage;
        AtlasX = atlasX;
        AtlasY = atlasY;
        PaddedSize = paddedSize;
        TileSize = tileSize;
        FrameSize = frameSize;
        FrameColumns = Math.Max(1, frameColumns);
        FrameRows = Math.Max(1, frameRows);
        Frames = frames;
    }

    public Rectangle GetFrameRectangle(int frameIndex)
    {
        int safeIndex = Math.Clamp(frameIndex, 0, FrameCount - 1);
        int slotIndex = Frames[safeIndex].Index;
        int x = (slotIndex % FrameColumns) * FrameSize;
        int y = (slotIndex / FrameColumns) * FrameSize;
        return new Rectangle(x, y, FrameSize, FrameSize);
    }

    public float GetFrameDurationSeconds(int frameIndex)
    {
        int safeIndex = Math.Clamp(frameIndex, 0, FrameCount - 1);
        return Frames[safeIndex].DurationSeconds;
    }
}

public readonly struct TextureAtlasAnimationFrame
{
    private const float TicksPerSecond = 20f;

    public int Index { get; }
    public int DurationTicks { get; }
    public float DurationSeconds => DurationTicks / TicksPerSecond;

    public TextureAtlasAnimationFrame(int index, int durationTicks)
    {
        Index = index;
        DurationTicks = Math.Max(1, durationTicks);
    }
}
