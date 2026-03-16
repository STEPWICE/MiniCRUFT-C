using System.Numerics;
using FontStashSharp;
using FontStashSharp.Interfaces;
using Veldrid;

namespace MiniCRUFT.Renderer;

public sealed class FontStashRenderer : IFontStashRenderer
{
    private readonly UiTextRenderer _textRenderer;

    public FontStashRenderer(UiTextRenderer textRenderer, GraphicsDevice device)
    {
        _textRenderer = textRenderer;
        TextureManager = new FontTextureManager(device);
    }

    public ITexture2DManager TextureManager { get; }

    public void Draw(object texture, Vector2 position, System.Drawing.Rectangle? sourceRect, FSColor color, float rotation, Vector2 scale, float depth)
    {
        if (texture is FontTexture fontTexture)
        {
            _textRenderer.Queue(fontTexture, position, sourceRect, color, scale);
        }
    }
}
