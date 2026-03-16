using System;

namespace MiniCRUFT.Core;

public readonly struct BlockCoord : IEquatable<BlockCoord>
{
    public readonly int X;
    public readonly int Y;
    public readonly int Z;

    public BlockCoord(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public bool Equals(BlockCoord other) => X == other.X && Y == other.Y && Z == other.Z;
    public override bool Equals(object? obj) => obj is BlockCoord other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override string ToString() => $"({X}, {Y}, {Z})";
}
