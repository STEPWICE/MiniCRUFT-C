namespace MiniCRUFT.Core;

public sealed class WeatherConfig
{
    public float ToggleIntervalSeconds { get; set; } = 120f;
    public float ToggleChance { get; set; } = 0.2f;
    public float RainDarkenR { get; set; } = 0.7f;
    public float RainDarkenG { get; set; } = 0.7f;
    public float RainDarkenB { get; set; } = 0.75f;
}
