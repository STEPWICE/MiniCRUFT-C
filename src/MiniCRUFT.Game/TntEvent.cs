using System.Numerics;

namespace MiniCRUFT.Game;

public enum TntEventKind
{
    Primed,
    Explosion
}

public readonly struct TntEvent
{
    public TntEventKind Kind { get; }
    public Vector3 Position { get; }
    public float Intensity { get; }
    public int AffectedBlocks { get; }

    public TntEvent(TntEventKind kind, Vector3 position, float intensity = 1f, int affectedBlocks = 0)
    {
        Kind = kind;
        Position = position;
        Intensity = intensity;
        AffectedBlocks = affectedBlocks;
    }
}
