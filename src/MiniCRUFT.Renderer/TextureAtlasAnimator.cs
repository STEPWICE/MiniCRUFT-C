using System;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Veldrid;

namespace MiniCRUFT.Renderer;

public sealed class TextureAtlasAnimator
{
    private readonly GraphicsDevice _device;
    private readonly TextureAtlas _atlas;
    private readonly TextureAtlasAnimation[] _animations;
    private readonly AnimationState[] _states;

    public TextureAtlasAnimator(GraphicsDevice device, TextureAtlas atlas)
    {
        _device = device;
        _atlas = atlas;
        _animations = atlas.Animations.ToArray();

        _states = new AnimationState[_animations.Length];
        for (int i = 0; i < _animations.Length; i++)
        {
            _states[i] = new AnimationState
            {
                FrameIndex = 0,
                TimeRemaining = _animations[i].GetFrameDurationSeconds(0)
            };
        }
    }

    public void Update(float dt)
    {
        if (dt <= 0f || _animations.Length == 0)
        {
            return;
        }

        for (int i = 0; i < _animations.Length; i++)
        {
            var animation = _animations[i];
            var state = _states[i];
            float remaining = dt;
            bool advanced = false;

            while (remaining > 0f)
            {
                if (state.TimeRemaining > remaining)
                {
                    state.TimeRemaining -= remaining;
                    remaining = 0f;
                    break;
                }

                remaining -= state.TimeRemaining;
                state.FrameIndex = (state.FrameIndex + 1) % animation.FrameCount;
                state.TimeRemaining = animation.GetFrameDurationSeconds(state.FrameIndex);
                advanced = true;
            }

            _states[i] = state;
            if (advanced)
            {
                UploadFrame(animation, state.FrameIndex);
            }
        }
    }

    private void UploadFrame(TextureAtlasAnimation animation, int frameIndex)
    {
        SixLabors.ImageSharp.Rectangle frameRect = animation.GetFrameRectangle(frameIndex);
        using var frame = animation.SourceImage.Clone(ctx =>
        {
            ctx.Crop(frameRect);
            if (frameRect.Width != TextureAtlas.TileSize || frameRect.Height != TextureAtlas.TileSize)
            {
                ctx.Resize(TextureAtlas.TileSize, TextureAtlas.TileSize, KnownResamplers.NearestNeighbor);
            }
        });

        using var paddedFrame = TextureAtlas.PadTile(frame, animation.Padding);
        UploadImage(animation, paddedFrame, 0);

        for (int level = 1; level < _atlas.MipLevels; level++)
        {
            int width = Math.Max(1, paddedFrame.Width >> level);
            int height = Math.Max(1, paddedFrame.Height >> level);
            using var mipFrame = paddedFrame.Clone(ctx => ctx.Resize(width, height, KnownResamplers.Box));
            UploadImage(animation, mipFrame, level);
        }
    }

    private void UploadImage(TextureAtlasAnimation animation, Image<Rgba32> image, int level)
    {
        var data = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(data);
        int cellSize = Math.Max(1, TextureAtlas.PaddedTileSize >> level);
        int column = animation.AtlasX / TextureAtlas.PaddedTileSize;
        int row = animation.AtlasY / TextureAtlas.PaddedTileSize;
        _device.UpdateTexture(
            _atlas.Texture,
            data,
            (uint)(column * cellSize),
            (uint)(row * cellSize),
            0u,
            (uint)image.Width,
            (uint)image.Height,
            1u,
            (uint)level,
            0u);
    }

    private sealed class AnimationState
    {
        public int FrameIndex { get; set; }
        public float TimeRemaining { get; set; }
    }
}
