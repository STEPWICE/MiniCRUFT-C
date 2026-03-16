using System.Numerics;
using Veldrid;

namespace MiniCRUFT.Renderer;

public readonly struct SpriteVertex
{
    public readonly Vector2 Position;
    public readonly Vector2 UV;
    public readonly Vector4 Color;

    public SpriteVertex(Vector2 position, Vector2 uv, Vector4 color)
    {
        Position = position;
        UV = uv;
        Color = color;
    }

    public static VertexLayoutDescription Layout => new(
        new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float2),
        new VertexElementDescription("UV", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
        new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Float4));
}
