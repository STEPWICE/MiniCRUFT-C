using MiniCRUFT.Core;

namespace MiniCRUFT.IO;

public readonly struct TntSaveData
{
    public BlockCoord Position { get; }
    public float FuseRemaining { get; }
    public float FuseDuration { get; }

    public TntSaveData(BlockCoord position, float fuseRemaining)
        : this(position, fuseRemaining, fuseRemaining)
    {
    }

    public TntSaveData(BlockCoord position, float fuseRemaining, float fuseDuration)
    {
        Position = position;
        FuseRemaining = fuseRemaining;
        FuseDuration = fuseDuration;
    }
}
