using System.Collections.Generic;

namespace MiniCRUFT.Renderer;

public sealed class MeshData
{
    public List<VoxelVertex> SolidVertices { get; } = new();
    public List<uint> SolidIndices { get; } = new();

    public List<VoxelVertex> CutoutVertices { get; } = new();
    public List<uint> CutoutIndices { get; } = new();

    public List<VoxelVertex> TransparentVertices { get; } = new();
    public List<uint> TransparentIndices { get; } = new();
}
