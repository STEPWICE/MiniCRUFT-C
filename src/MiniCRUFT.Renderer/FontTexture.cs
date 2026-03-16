using System;
using Veldrid;

namespace MiniCRUFT.Renderer;

public sealed class FontTexture : IDisposable
{
    private readonly GraphicsDevice _device;
    public Texture Texture { get; }
    public TextureView View { get; }
    public int Width { get; }
    public int Height { get; }

    public FontTexture(GraphicsDevice device, int width, int height)
    {
        _device = device;
        Width = width;
        Height = height;
        Texture = device.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
            (uint)width,
            (uint)height,
            1,
            1,
            PixelFormat.R8_UNorm,
            TextureUsage.Sampled));
        View = device.ResourceFactory.CreateTextureView(Texture);
    }

    public void Clear()
    {
        var clear = new byte[Width * Height];
        _device.UpdateTexture(Texture, clear, 0, 0, 0, (uint)Width, (uint)Height, 1, 0, 0);
    }

    public void Dispose()
    {
        View.Dispose();
        Texture.Dispose();
    }
}
