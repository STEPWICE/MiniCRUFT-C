namespace MiniCRUFT.World;

public readonly record struct ChunkCoordinate(int X, int Z)
{
    public int OriginX => X * ChunkConstants.ChunkSizeX;
    public int OriginZ => Z * ChunkConstants.ChunkSizeZ;

    public static ChunkCoordinate FromWorldBlock(int worldX, int worldZ)
    {
        return new ChunkCoordinate(
            FloorDiv(worldX, ChunkConstants.ChunkSizeX),
            FloorDiv(worldZ, ChunkConstants.ChunkSizeZ));
    }

    internal static int FloorDiv(int value, int divisor)
    {
        var quotient = value / divisor;
        var remainder = value % divisor;

        return remainder != 0 && ((remainder < 0) != (divisor < 0))
            ? quotient - 1
            : quotient;
    }

    internal static int PositiveModulo(int value, int divisor)
    {
        var remainder = value % divisor;
        return remainder < 0 ? remainder + divisor : remainder;
    }

    public override string ToString() => $"({X}, {Z})";
}
