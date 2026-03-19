using System.Numerics;

namespace MiniCRUFT.Core;

public readonly struct TntRenderInstance
{
    public Vector3 Position { get; }
    public float FuseRemaining { get; }
    public float FuseDuration { get; }

    public TntRenderInstance(Vector3 position, float fuseRemaining, float fuseDuration)
    {
        Position = position;
        FuseRemaining = fuseRemaining;
        FuseDuration = fuseDuration;
    }
}
