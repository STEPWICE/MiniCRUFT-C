namespace MiniCRUFT.Core;

public sealed class WorldGenConfig
{
    public int BaseHeight { get; set; } = 54;
    public int SeaLevel { get; set; } = 62;
    public float ContinentalAmplitude { get; set; } = 60f;
    public float PeakAmplitude { get; set; } = 42f;
    public float ErosionAmplitude { get; set; } = 22f;
    public float RidgeAmplitude { get; set; } = 28f;
    public float RiverThreshold { get; set; } = 0.045f;
    public float CaveThreshold { get; set; } = 0.63f;
    public float TreeChance { get; set; } = 0.06f;
    public float LargeTreeChance { get; set; } = 0.12f;
    public int LargeTreeMinHeight { get; set; } = 8;
    public int LargeTreeMaxHeight { get; set; } = 12;
    public int LargeTreeLeafRadius { get; set; } = 4;
    public int LargeTreeCanopyDepth { get; set; } = 4;
    public float TreeMaxSlope { get; set; } = 7f;
    public string? ForcedBiome { get; set; } = null;
    public float CliffSlopeThreshold { get; set; } = 10f;
    public float CliffSmoothStrength { get; set; } = 0.35f;
    public float RidgeClamp { get; set; } = 0.85f;
    public int BeachSize { get; set; } = 3;
}
