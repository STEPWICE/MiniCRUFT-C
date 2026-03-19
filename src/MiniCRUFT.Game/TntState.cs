using MiniCRUFT.Core;

namespace MiniCRUFT.Game;

internal readonly struct TntState
{
    public BlockCoord Position { get; }
    public float FuseRemaining { get; }
    public float FuseDuration { get; }

    public TntState(BlockCoord position, float fuseRemaining, float fuseDuration)
    {
        Position = position;
        FuseRemaining = fuseRemaining;
        FuseDuration = fuseDuration;
    }

    public TntState WithFuse(float fuseRemaining)
    {
        return new TntState(Position, fuseRemaining, FuseDuration);
    }
}
