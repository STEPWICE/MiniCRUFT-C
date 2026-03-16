using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace MiniCRUFT.Renderer;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct UiTextVertex
{
    public Vector2 Position;
    public Vector2 TexCoord;
    public Vector4 Color;

    public UiTextVertex(Vector2 position, Vector2 texCoord, Vector4 color)
    {
        Position = position;
        TexCoord = texCoord;
        Color = color;
    }

    public static readonly VertexLayoutDescription Layout = new(
        new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float2),
        new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
        new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Float4));
}
