namespace MiniCRUFT.World;

public sealed class Chunk
{
    private readonly Subchunk[] _subchunks = new Subchunk[ChunkConstants.SubchunkCountPerChunk];

    public Chunk(ChunkCoordinate coordinate)
    {
        Coordinate = coordinate;

        for (var i = 0; i < _subchunks.Length; i++)
        {
            _subchunks[i] = new Subchunk();
        }
    }

    public ChunkCoordinate Coordinate { get; }
    public ChunkState State { get; internal set; } = ChunkState.Unloaded;
    public IReadOnlyList<Subchunk> Subchunks => _subchunks;

    public Subchunk GetSubchunk(int subchunkIndex)
    {
        if ((uint)subchunkIndex >= _subchunks.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(subchunkIndex));
        }

        return _subchunks[subchunkIndex];
    }

    public BlockId GetBlock(LocalBlockCoordinate localCoordinate)
    {
        return GetSubchunk(localCoordinate.SubchunkIndex).GetBlock(
            localCoordinate.X,
            localCoordinate.SubchunkLocalY,
            localCoordinate.Z);
    }

    public void SetBlock(LocalBlockCoordinate localCoordinate, BlockId blockId)
    {
        GetSubchunk(localCoordinate.SubchunkIndex).SetBlock(
            localCoordinate.X,
            localCoordinate.SubchunkLocalY,
            localCoordinate.Z,
            blockId);
    }

    public void MarkSubchunkDirty(int subchunkIndex)
    {
        GetSubchunk(subchunkIndex).MarkDirty();
    }

    public int DirtySubchunkCount
    {
        get
        {
            var count = 0;
            foreach (var subchunk in _subchunks)
            {
                if (subchunk.IsDirty)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
