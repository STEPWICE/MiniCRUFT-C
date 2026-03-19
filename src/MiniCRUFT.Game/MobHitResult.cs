using System.Numerics;
using MiniCRUFT.Core;

namespace MiniCRUFT.Game;

public readonly struct MobHitResult
{
    public bool Hit { get; }
    public int Index { get; }
    public MobType Type { get; }
    public Vector3 Position { get; }
    public float Distance { get; }

    public MobHitResult(bool hit, int index, MobType type, Vector3 position, float distance)
    {
        Hit = hit;
        Index = index;
        Type = type;
        Position = position;
        Distance = distance;
    }
}
