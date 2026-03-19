namespace MiniCRUFT.Core;

public sealed class AtmosphereConfig
{
    public float CloudHeight { get; set; } = 190f;
    public float CloudSpeed { get; set; } = 0.003f;
    public float CloudTiling { get; set; } = 2.0f;
    public float CloudOpacity { get; set; } = 0.3f;
    public float CloudRadius { get; set; } = 360f;
    public float CloudCellSize { get; set; } = 48f;
    public float CloudAlphaCutoff { get; set; } = 0.68f;
    public float CloudBlockSize { get; set; } = 4f;
    public float SunSize { get; set; } = 64f;
    public float MoonSize { get; set; } = 48f;
    public float WaterOpacity { get; set; } = 0.6f;
    public float WaterUvSpeed { get; set; } = 0.02f;
    public Color3 WaterTint { get; set; } = new(0.08f, 0.2f, 0.32f);
    public Color3 WaterShoreTint { get; set; } = new(0.25f, 0.45f, 0.55f);
    public float WaterShoreStrength { get; set; } = 0.55f;
    public float LavaOpacity { get; set; } = 0.95f;
    public float LavaUvSpeed { get; set; } = 0.01f;
    public Color3 LavaTint { get; set; } = new(1.0f, 0.42f, 0.08f);

    public Color3 FogDayColor { get; set; } = new(0.54f, 0.64f, 0.78f);
    public Color3 FogNightColor { get; set; } = new(0.04f, 0.05f, 0.08f);
    public Color3 FogRainColor { get; set; } = new(0.35f, 0.39f, 0.46f);
    public float FogNightMultiplier { get; set; } = 0.84f;
    public float FogRainMultiplier { get; set; } = 0.88f;

    public Color3 SkyDayTop { get; set; } = new(0.45f, 0.67f, 1.0f);
    public Color3 SkyDayBottom { get; set; } = new(0.74f, 0.84f, 0.98f);
    public Color3 SkyNightTop { get; set; } = new(0.03f, 0.04f, 0.08f);
    public Color3 SkyNightBottom { get; set; } = new(0.02f, 0.03f, 0.06f);
}
