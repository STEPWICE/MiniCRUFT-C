using System;
using FontStashSharp.Interfaces;
using Veldrid;

namespace MiniCRUFT.Renderer;

public sealed class FontTextureManager : ITexture2DManager
{
    private readonly GraphicsDevice _device;

    public FontTextureManager(GraphicsDevice device)
    {
        _device = device;
    }

    public object CreateTexture(int width, int height)
    {
        return new FontTexture(_device, width, height);
    }

    public System.Drawing.Point GetTextureSize(object texture)
    {
        var fontTexture = (FontTexture)texture;
        return new System.Drawing.Point(fontTexture.Width, fontTexture.Height);
    }

    public void SetTextureData(object texture, System.Drawing.Rectangle bounds, byte[] data)
    {
        var fontTexture = (FontTexture)texture;
        _device.UpdateTexture(
            fontTexture.Texture,
            data,
            (uint)bounds.X,
            (uint)bounds.Y,
            0,
            (uint)bounds.Width,
            (uint)bounds.Height,
            1,
            0,
            0);
    }
}
