using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace MiniCRUFT.Renderer;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VoxelVertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 LocalUV;
    public Vector2 AtlasMin;
    public Vector2 AtlasSize;
    public float Light;
    public Vector4 Tint;
    public float MaterialId;

    public VoxelVertex(Vector3 position, Vector3 normal, Vector2 localUV, Vector2 atlasMin, Vector2 atlasSize, float light, Vector4 tint, float materialId)
    {
        Position = position;
        Normal = normal;
        LocalUV = localUV;
        AtlasMin = atlasMin;
        AtlasSize = atlasSize;
        Light = light;
        Tint = tint;
        MaterialId = materialId;
    }

    public static readonly VertexLayoutDescription Layout = new(
        new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3),
        new VertexElementDescription("Normal", VertexElementSemantic.Normal, VertexElementFormat.Float3),
        new VertexElementDescription("LocalUV", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
        new VertexElementDescription("AtlasMin", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
        new VertexElementDescription("AtlasSize", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
        new VertexElementDescription("Light", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1),
        new VertexElementDescription("Tint", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
        new VertexElementDescription("MaterialId", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1)
    );
}
