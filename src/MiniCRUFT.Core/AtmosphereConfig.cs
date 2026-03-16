namespace MiniCRUFT.Core;

public sealed class AtmosphereConfig
{
    public float CloudHeight { get; set; } = 160f;
    public float CloudSpeed { get; set; } = 0.003f;
    public float CloudTiling { get; set; } = 4.0f;
    public float CloudOpacity { get; set; } = 0.3f;
    public float CloudRadius { get; set; } = 320f;
    public float CloudCellSize { get; set; } = 24f;
    public float SunSize { get; set; } = 40f;
    public float MoonSize { get; set; } = 36f;
    public float WaterOpacity { get; set; } = 0.55f;
    public float WaterUvSpeed { get; set; } = 0.02f;
    public Color3 WaterTint { get; set; } = new(0.08f, 0.18f, 0.28f);

    public Color3 SkyDayTop { get; set; } = new(0.45f, 0.67f, 1.0f);
    public Color3 SkyDayBottom { get; set; } = new(0.78f, 0.9f, 1.0f);
    public Color3 SkyNightTop { get; set; } = new(0.03f, 0.04f, 0.08f);
    public Color3 SkyNightBottom { get; set; } = new(0.02f, 0.03f, 0.06f);
}
