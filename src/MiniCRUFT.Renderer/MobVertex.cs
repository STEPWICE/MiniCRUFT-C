using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace MiniCRUFT.Renderer;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MobVertex
{
    public Vector3 Position;
    public Vector2 UV;
    public Vector4 Tint;
    public Vector2 Effects;

    public MobVertex(Vector3 position, Vector2 uv, Vector4 tint, Vector2 effects)
    {
        Position = position;
        UV = uv;
        Tint = tint;
        Effects = effects;
    }

    public static readonly VertexLayoutDescription Layout = new(
        new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3),
        new VertexElementDescription("UV", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
        new VertexElementDescription("Tint", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
        new VertexElementDescription("Effects", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));
}
