namespace MiniCRUFT.World;

public sealed class Subchunk
{
    private readonly BlockId[] _blocks = new BlockId[ChunkConstants.SubchunkSize * ChunkConstants.SubchunkSize * ChunkConstants.SubchunkSize];

    public bool IsDirty { get; private set; } = true;
    public SubchunkMeshState MeshState { get; private set; } = SubchunkMeshState.NeedsBuild;
    public int Revision { get; private set; } = 1;
    public int BuildVersion { get; private set; }
    public int LastDrainedMeshingOutputBuildVersion { get; private set; } = -1;
    public SubchunkMeshingOutput? CurrentMeshingOutput { get; private set; }

    public bool HasUndrainedMeshingOutput => CurrentMeshingOutput is not null
        && !IsDirty
        && MeshState == SubchunkMeshState.Built
        && CurrentMeshingOutput.BuildVersion != LastDrainedMeshingOutputBuildVersion;

    public BlockId GetBlock(int x, int y, int z)
    {
        return _blocks[GetIndex(x, y, z)];
    }

    public void SetBlock(int x, int y, int z, BlockId blockId)
    {
        _blocks[GetIndex(x, y, z)] = blockId;
        MarkDirty();
    }

    public void Fill(BlockId blockId)
    {
        Array.Fill(_blocks, blockId);
        MarkDirty();
    }

    internal void RestoreBlocks(ReadOnlySpan<BlockId> blocks)
    {
        if (blocks.Length != _blocks.Length)
        {
            throw new ArgumentException($"Expected {_blocks.Length} blocks but got {blocks.Length}.", nameof(blocks));
        }

        blocks.CopyTo(_blocks);
        IsDirty = true;
        MeshState = SubchunkMeshState.NeedsBuild;
        Revision = 1;
        BuildVersion = 0;
        LastDrainedMeshingOutputBuildVersion = -1;
        CurrentMeshingOutput = null;
    }

    public ReadOnlySpan<BlockId> AsSpan() => _blocks;

    public void MarkDirty()
    {
        Revision++;
        IsDirty = true;
        MeshState = SubchunkMeshState.NeedsBuild;
        CurrentMeshingOutput = null;
    }

    public void MarkMeshQueued()
    {
        MeshState = SubchunkMeshState.Queued;
    }

    public bool TryMarkMeshBuilt(int revision)
    {
        if (revision != Revision)
        {
            return false;
        }

        IsDirty = false;
        MeshState = SubchunkMeshState.Built;
        BuildVersion++;
        CurrentMeshingOutput = null;
        return true;
    }

    public bool TryAcceptMeshingOutput(SubchunkMeshingOutput output)
    {
        if (output.Revision != Revision || output.BuildVersion != BuildVersion)
        {
            return false;
        }

        IsDirty = false;
        MeshState = SubchunkMeshState.Built;
        CurrentMeshingOutput = output;
        BuildVersion++;
        return true;
    }

    public bool TryDrainMeshingOutput(out SubchunkMeshingOutput? output)
    {
        if (!HasUndrainedMeshingOutput)
        {
            output = null;
            return false;
        }

        output = CurrentMeshingOutput;
        LastDrainedMeshingOutputBuildVersion = output!.BuildVersion;
        return true;
    }

    public void MarkMeshBuilt()
    {
        _ = TryMarkMeshBuilt(Revision);
    }

    public void ClearDirtyFlag()
    {
        IsDirty = false;
    }

    private static int GetIndex(int x, int y, int z)
    {
        if ((uint)x >= ChunkConstants.SubchunkSize)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        if ((uint)y >= ChunkConstants.SubchunkSize)
        {
            throw new ArgumentOutOfRangeException(nameof(y));
        }

        if ((uint)z >= ChunkConstants.SubchunkSize)
        {
            throw new ArgumentOutOfRangeException(nameof(z));
        }

        return x + (z * ChunkConstants.SubchunkSize) + (y * ChunkConstants.SubchunkSize * ChunkConstants.SubchunkSize);
    }
}
