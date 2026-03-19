using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace MiniCRUFT.Renderer;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct SelectionVertex
{
    public readonly Vector3 Position;
    public readonly Vector4 Color;

    public SelectionVertex(Vector3 position, Vector4 color)
    {
        Position = position;
        Color = color;
    }

    public static readonly VertexLayoutDescription Layout = new(
        new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3),
        new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Float4));
}
