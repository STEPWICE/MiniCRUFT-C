using System;
using Veldrid;

namespace MiniCRUFT.Renderer;

public sealed class ChunkMesh : IDisposable
{
    public MeshPart Solid { get; private set; }
    public MeshPart Cutout { get; private set; }
    public MeshPart Transparent { get; private set; }

    public void Update(GraphicsDevice device, MeshData data)
    {
        Solid = BuildPart(device, data.SolidVertices, data.SolidIndices, Solid);
        Cutout = BuildPart(device, data.CutoutVertices, data.CutoutIndices, Cutout);
        Transparent = BuildPart(device, data.TransparentVertices, data.TransparentIndices, Transparent);
    }

    private static MeshPart BuildPart(GraphicsDevice device, List<VoxelVertex> vertices, List<uint> indices, MeshPart previous)
    {
        previous.Dispose();

        if (vertices.Count == 0 || indices.Count == 0)
        {
            return new MeshPart();
        }

        uint vertexBufferSize = (uint)(vertices.Count * System.Runtime.InteropServices.Marshal.SizeOf<VoxelVertex>());
        uint indexBufferSize = (uint)(indices.Count * sizeof(uint));

        var vertexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer));
        var indexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(indexBufferSize, BufferUsage.IndexBuffer));

        device.UpdateBuffer(vertexBuffer, 0, vertices.ToArray());
        device.UpdateBuffer(indexBuffer, 0, indices.ToArray());

        return new MeshPart(vertexBuffer, indexBuffer, (uint)indices.Count);
    }

    public void Dispose()
    {
        Solid.Dispose();
        Cutout.Dispose();
        Transparent.Dispose();
    }
}

public struct MeshPart : IDisposable
{
    public DeviceBuffer? VertexBuffer;
    public DeviceBuffer? IndexBuffer;
    public uint IndexCount;

    public MeshPart(DeviceBuffer? vertexBuffer, DeviceBuffer? indexBuffer, uint indexCount)
    {
        VertexBuffer = vertexBuffer;
        IndexBuffer = indexBuffer;
        IndexCount = indexCount;
    }

    public void Dispose()
    {
        VertexBuffer?.Dispose();
        IndexBuffer?.Dispose();
        VertexBuffer = null;
        IndexBuffer = null;
        IndexCount = 0;
    }
}
