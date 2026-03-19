using System.Numerics;
using MiniCRUFT.Core;

namespace MiniCRUFT.Game;

public enum MobEventKind
{
    Ambient,
    Step,
    Hurt,
    Death,
    Attack,
    Explosion
}

public readonly struct MobEvent
{
    public MobEventKind Kind { get; }
    public MobType Type { get; }
    public Vector3 Position { get; }
    public float Intensity { get; }
    public bool Elite { get; }
    public EliteMobVariant EliteVariant { get; }

    public MobEvent(MobEventKind kind, MobType type, Vector3 position, float intensity = 1f, bool elite = false, EliteMobVariant eliteVariant = EliteMobVariant.None)
    {
        Kind = kind;
        Type = type;
        Position = position;
        Intensity = intensity;
        Elite = elite;
        EliteVariant = eliteVariant;
    }
}
