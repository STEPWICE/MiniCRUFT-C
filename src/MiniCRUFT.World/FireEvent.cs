using System.Numerics;

namespace MiniCRUFT.World;

public enum FireEventKind
{
    Ignited,
    Crackle,
    Consumed,
    Extinguished
}

public readonly struct FireEvent
{
    public FireEventKind Kind { get; }
    public Vector3 Position { get; }
    public float Intensity { get; }

    public FireEvent(FireEventKind kind, Vector3 position, float intensity = 1f)
    {
        Kind = kind;
        Position = position;
        Intensity = intensity;
    }
}
