using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace MiniCRUFT.Renderer;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct UiVertex
{
    public Vector2 Position;
    public Vector4 Color;

    public UiVertex(Vector2 position, Vector4 color)
    {
        Position = position;
        Color = color;
    }

    public static readonly VertexLayoutDescription Layout = new(
        new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float2),
        new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Float4));
}
