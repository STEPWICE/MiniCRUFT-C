namespace MiniCRUFT.Core;

public sealed class WeatherConfig
{
    public float ToggleIntervalSeconds { get; set; } = 120f;
    public float ToggleChance { get; set; } = 0.2f;
    public float RainDarkenR { get; set; } = 0.7f;
    public float RainDarkenG { get; set; } = 0.7f;
    public float RainDarkenB { get; set; } = 0.75f;
    public float RainFadeInSeconds { get; set; } = 4f;
    public float RainFadeOutSeconds { get; set; } = 6f;
    public float RainSpawnRate { get; set; } = 72f;
    public int RainMaxParticles { get; set; } = 192;
    public float RainParticleSpeed { get; set; } = 36f;
    public float RainParticleLength { get; set; } = 24f;
    public float RainParticleWidth { get; set; } = 1.5f;
    public Color3 RainStreakTint { get; set; } = new(0.72f, 0.82f, 0.95f);
    public float RainStreakAlpha { get; set; } = 0.28f;
    public float LightningChancePerSecond { get; set; } = 0.015f;
    public float LightningMinRainIntensity { get; set; } = 0.45f;
    public float LightningFlashFadeSeconds { get; set; } = 0.16f;
    public float LightningFlashStrength { get; set; } = 0.9f;
    public float LightningThunderDelaySeconds { get; set; } = 1.25f;
    public float LightningCooldownSeconds { get; set; } = 18f;
}
