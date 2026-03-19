namespace MiniCRUFT.Core;

public sealed class FluidConfig
{
    public const int AbsoluteMaxLevel = 7;
    public bool Enabled { get; set; } = true;
    public int MaxUpdatesPerFrame { get; set; } = 256;
    public int WaterMaxSpreadLevel { get; set; } = AbsoluteMaxLevel;
    public int LavaMaxSpreadLevel { get; set; } = 4;
    public int LavaUpdatesPerFrame { get; set; } = 48;
    public int LavaUpdateIntervalFrames { get; set; } = 3;
    public bool InfiniteSources { get; set; } = true;
    public bool LavaInfiniteSources { get; set; } = false;
    public bool ReplaceNonSolid { get; set; } = true;
    public bool LavaHardensOnWaterContact { get; set; } = true;
}
