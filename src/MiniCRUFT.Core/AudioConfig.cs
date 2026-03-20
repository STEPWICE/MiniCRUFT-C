namespace MiniCRUFT.Core;

public sealed class AudioConfig
{
    public bool Enabled { get; set; } = true;
    public int MaxActive { get; set; } = 32;
    public float SpatialInnerRadius { get; set; } = 2.5f;
    public float SpatialOuterRadius { get; set; } = 32f;
    public float SpatialPanStrength { get; set; } = 1f;
    public float MasterVolume { get; set; } = 1f;
    public float DigVolume { get; set; } = 0.85f;
    public float PlaceVolume { get; set; } = 0.6f;
    public float AmbientVolume { get; set; } = 0.45f;
    public float MusicVolume { get; set; } = 0.35f;
    public float WeatherVolume { get; set; } = 0.35f;
    public float LiquidVolume { get; set; } = 0.35f;
    public float FireVolume { get; set; } = 0.45f;
    public float StepVolume { get; set; } = 0.55f;
    public float RunVolume { get; set; } = 0.7f;
    public float JumpVolume { get; set; } = 0.6f;
    public float MobVolume { get; set; } = 0.65f;
    public float MobStepVolume { get; set; } = 0.5f;
    public float FuseVolume { get; set; } = 0.5f;
    public float ExplosionVolume { get; set; } = 0.95f;
    public float StepDistance { get; set; } = 1.7f;
    public float RunStepDistance { get; set; } = 1.1f;
    public float AmbientIntervalSeconds { get; set; } = 45f;
    public float MusicIntervalSeconds { get; set; } = 180f;
    public float WeatherIntervalSeconds { get; set; } = 12f;
    public float LiquidIntervalSeconds { get; set; } = 8f;
    public float LiquidRadius { get; set; } = 6f;
    public float SwimStepDistance { get; set; } = 1.3f;
}
