namespace MiniCRUFT.Core;

public sealed class DayNightConfig
{
    public float DayLengthSeconds { get; set; } = 900f;
    public float StartTimeOfDay { get; set; } = 0.25f;
    public float MinSunIntensity { get; set; } = 0.05f;
}
