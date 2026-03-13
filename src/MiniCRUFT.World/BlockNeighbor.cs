namespace MiniCRUFT.World;

public readonly record struct BlockNeighbor(
    int WorldX,
    int WorldY,
    int WorldZ,
    ChunkCoordinate ChunkCoordinate,
    LocalBlockCoordinate LocalCoordinate,
    BlockId BlockId);
