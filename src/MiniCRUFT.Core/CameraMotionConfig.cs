namespace MiniCRUFT.Core;

public sealed class CameraMotionConfig
{
    public bool Enabled { get; set; } = true;
    public float BobAmplitude { get; set; } = 0.03f;
    public float BobSpeed { get; set; } = 7f;
    public float BobLateralFactor { get; set; } = 0.55f;
    public float BobForwardFactor { get; set; } = 0.25f;
    public float InertiaStrength { get; set; } = 14f;
    public float AirborneMultiplier { get; set; } = 0.2f;
    public float LiquidMultiplier { get; set; } = 0.35f;
}
