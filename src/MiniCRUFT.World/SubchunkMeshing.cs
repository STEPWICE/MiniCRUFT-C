namespace MiniCRUFT.World;

public readonly record struct VisibleBlockFace(
    int WorldX,
    int WorldY,
    int WorldZ,
    LocalBlockCoordinate LocalCoordinate,
    BlockId BlockId,
    BlockFace Face);

public sealed record SubchunkMeshingSnapshot(
    ChunkCoordinate ChunkCoordinate,
    int SubchunkIndex,
    int Revision,
    int BuildVersion,
    BlockId[] Blocks,
    BlockId[] NegativeXBorder,
    BlockId[] PositiveXBorder,
    BlockId[] NegativeYBorder,
    BlockId[] PositiveYBorder,
    BlockId[] NegativeZBorder,
    BlockId[] PositiveZBorder);

public sealed record SubchunkMeshingOutput(
    ChunkCoordinate ChunkCoordinate,
    int SubchunkIndex,
    int Revision,
    int BuildVersion,
    IReadOnlyList<VisibleBlockFace> VisibleFaces);

public static class SubchunkMeshing
{
    public static int CountVisibleFaces(WorldHost world, Chunk chunk, int subchunkIndex)
    {
        var count = 0;
        foreach (var _ in EnumerateVisibleFaces(world, chunk, subchunkIndex))
        {
            count++;
        }

        return count;
    }

    public static int CountVisibleFaces(SubchunkMeshingSnapshot snapshot)
    {
        var count = 0;
        foreach (var _ in EnumerateVisibleFaces(snapshot))
        {
            count++;
        }

        return count;
    }

    public static IReadOnlyList<VisibleBlockFace> BuildVisibleFaces(SubchunkMeshingSnapshot snapshot)
    {
        return EnumerateVisibleFaces(snapshot).ToArray();
    }

    public static SubchunkMeshingOutput BuildOutput(SubchunkMeshingSnapshot snapshot)
    {
        return new SubchunkMeshingOutput(
            snapshot.ChunkCoordinate,
            snapshot.SubchunkIndex,
            snapshot.Revision,
            snapshot.BuildVersion,
            BuildVisibleFaces(snapshot));
    }

    public static IEnumerable<VisibleBlockFace> EnumerateVisibleFaces(WorldHost world, Chunk chunk, int subchunkIndex)
    {
        if ((uint)subchunkIndex >= ChunkConstants.SubchunkCountPerChunk)
        {
            throw new ArgumentOutOfRangeException(nameof(subchunkIndex));
        }

        var subchunkBaseY = subchunkIndex * ChunkConstants.SubchunkSize;

        for (var localY = 0; localY < ChunkConstants.SubchunkSize; localY++)
        {
            for (var localZ = 0; localZ < ChunkConstants.SubchunkSize; localZ++)
            {
                for (var localX = 0; localX < ChunkConstants.SubchunkSize; localX++)
                {
                    var localCoordinate = new LocalBlockCoordinate(localX, subchunkBaseY + localY, localZ);
                    var blockId = chunk.GetBlock(localCoordinate);
                    if (blockId == BlockId.Air)
                    {
                        continue;
                    }

                    var worldX = chunk.Coordinate.OriginX + localCoordinate.X;
                    var worldY = localCoordinate.Y;
                    var worldZ = chunk.Coordinate.OriginZ + localCoordinate.Z;

                    foreach (BlockFace face in Enum.GetValues<BlockFace>())
                    {
                        if (world.IsBlockFaceVisible(worldX, worldY, worldZ, face))
                        {
                            yield return new VisibleBlockFace(worldX, worldY, worldZ, localCoordinate, blockId, face);
                        }
                    }
                }
            }
        }
    }

    public static IEnumerable<VisibleBlockFace> EnumerateVisibleFaces(SubchunkMeshingSnapshot snapshot)
    {
        ValidateSnapshot(snapshot);

        var subchunkBaseY = snapshot.SubchunkIndex * ChunkConstants.SubchunkSize;

        for (var localY = 0; localY < ChunkConstants.SubchunkSize; localY++)
        {
            for (var localZ = 0; localZ < ChunkConstants.SubchunkSize; localZ++)
            {
                for (var localX = 0; localX < ChunkConstants.SubchunkSize; localX++)
                {
                    var blockId = GetSnapshotBlock(snapshot.Blocks, localX, localY, localZ);
                    if (blockId == BlockId.Air)
                    {
                        continue;
                    }

                    var localCoordinate = new LocalBlockCoordinate(localX, subchunkBaseY + localY, localZ);
                    var worldX = snapshot.ChunkCoordinate.OriginX + localX;
                    var worldY = subchunkBaseY + localY;
                    var worldZ = snapshot.ChunkCoordinate.OriginZ + localZ;

                    foreach (BlockFace face in Enum.GetValues<BlockFace>())
                    {
                        if (!IsOccluded(snapshot, localX, localY, localZ, face))
                        {
                            yield return new VisibleBlockFace(worldX, worldY, worldZ, localCoordinate, blockId, face);
                        }
                    }
                }
            }
        }
    }

    private static bool IsOccluded(SubchunkMeshingSnapshot snapshot, int localX, int localY, int localZ, BlockFace face)
    {
        var neighborBlock = face switch
        {
            BlockFace.NegativeX => localX == 0
                ? GetBorderBlock(snapshot.NegativeXBorder, localY, localZ)
                : GetSnapshotBlock(snapshot.Blocks, localX - 1, localY, localZ),
            BlockFace.PositiveX => localX == ChunkConstants.SubchunkSize - 1
                ? GetBorderBlock(snapshot.PositiveXBorder, localY, localZ)
                : GetSnapshotBlock(snapshot.Blocks, localX + 1, localY, localZ),
            BlockFace.NegativeY => localY == 0
                ? GetBorderBlock(snapshot.NegativeYBorder, localX, localZ)
                : GetSnapshotBlock(snapshot.Blocks, localX, localY - 1, localZ),
            BlockFace.PositiveY => localY == ChunkConstants.SubchunkSize - 1
                ? GetBorderBlock(snapshot.PositiveYBorder, localX, localZ)
                : GetSnapshotBlock(snapshot.Blocks, localX, localY + 1, localZ),
            BlockFace.NegativeZ => localZ == 0
                ? GetBorderBlock(snapshot.NegativeZBorder, localX, localY)
                : GetSnapshotBlock(snapshot.Blocks, localX, localY, localZ - 1),
            BlockFace.PositiveZ => localZ == ChunkConstants.SubchunkSize - 1
                ? GetBorderBlock(snapshot.PositiveZBorder, localX, localY)
                : GetSnapshotBlock(snapshot.Blocks, localX, localY, localZ + 1),
            _ => BlockId.Air
        };

        return neighborBlock != BlockId.Air;
    }

    private static void ValidateSnapshot(SubchunkMeshingSnapshot snapshot)
    {
        if ((uint)snapshot.SubchunkIndex >= ChunkConstants.SubchunkCountPerChunk)
        {
            throw new ArgumentOutOfRangeException(nameof(snapshot.SubchunkIndex));
        }

        var blockCount = ChunkConstants.SubchunkSize * ChunkConstants.SubchunkSize * ChunkConstants.SubchunkSize;
        var borderCount = ChunkConstants.SubchunkSize * ChunkConstants.SubchunkSize;

        ValidateLength(snapshot.Blocks, blockCount, nameof(snapshot.Blocks));
        ValidateLength(snapshot.NegativeXBorder, borderCount, nameof(snapshot.NegativeXBorder));
        ValidateLength(snapshot.PositiveXBorder, borderCount, nameof(snapshot.PositiveXBorder));
        ValidateLength(snapshot.NegativeYBorder, borderCount, nameof(snapshot.NegativeYBorder));
        ValidateLength(snapshot.PositiveYBorder, borderCount, nameof(snapshot.PositiveYBorder));
        ValidateLength(snapshot.NegativeZBorder, borderCount, nameof(snapshot.NegativeZBorder));
        ValidateLength(snapshot.PositiveZBorder, borderCount, nameof(snapshot.PositiveZBorder));
    }

    private static void ValidateLength(BlockId[] blocks, int expectedLength, string paramName)
    {
        if (blocks.Length != expectedLength)
        {
            throw new ArgumentException($"Expected {expectedLength} entries.", paramName);
        }
    }

    private static BlockId GetSnapshotBlock(BlockId[] blocks, int x, int y, int z)
    {
        return blocks[x + (z * ChunkConstants.SubchunkSize) + (y * ChunkConstants.SubchunkSize * ChunkConstants.SubchunkSize)];
    }

    private static BlockId GetBorderBlock(BlockId[] blocks, int a, int b)
    {
        return blocks[a + (b * ChunkConstants.SubchunkSize)];
    }
}
