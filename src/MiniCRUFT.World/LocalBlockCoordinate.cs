namespace MiniCRUFT.World;

public readonly record struct LocalBlockCoordinate
{
    public LocalBlockCoordinate(int x, int y, int z)
    {
        if ((uint)x >= ChunkConstants.ChunkSizeX)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        if ((uint)y >= ChunkConstants.ChunkHeight)
        {
            throw new ArgumentOutOfRangeException(nameof(y));
        }

        if ((uint)z >= ChunkConstants.ChunkSizeZ)
        {
            throw new ArgumentOutOfRangeException(nameof(z));
        }

        X = x;
        Y = y;
        Z = z;
    }

    public int X { get; }
    public int Y { get; }
    public int Z { get; }

    public int SubchunkIndex => Y / ChunkConstants.SubchunkSize;
    public int SubchunkLocalY => Y % ChunkConstants.SubchunkSize;

    public static LocalBlockCoordinate FromWorldBlock(int worldX, int worldY, int worldZ)
    {
        return new LocalBlockCoordinate(
            ChunkCoordinate.PositiveModulo(worldX, ChunkConstants.ChunkSizeX),
            worldY,
            ChunkCoordinate.PositiveModulo(worldZ, ChunkConstants.ChunkSizeZ));
    }

    public override string ToString() => $"({X}, {Y}, {Z})";
}
