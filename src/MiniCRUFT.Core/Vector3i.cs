using System;
using System.Numerics;

namespace MiniCRUFT.Core;

public readonly struct Vector3i : IEquatable<Vector3i>
{
    public readonly int X;
    public readonly int Y;
    public readonly int Z;

    public Vector3i(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vector3i Zero => new Vector3i(0, 0, 0);

    public static Vector3i operator +(Vector3i a, Vector3i b) => new Vector3i(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3i operator -(Vector3i a, Vector3i b) => new Vector3i(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public Vector3 ToVector3() => new Vector3(X, Y, Z);

    public bool Equals(Vector3i other) => X == other.X && Y == other.Y && Z == other.Z;
    public override bool Equals(object? obj) => obj is Vector3i other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override string ToString() => $"({X}, {Y}, {Z})";
}
