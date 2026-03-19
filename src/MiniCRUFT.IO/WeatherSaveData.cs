namespace MiniCRUFT.IO;

public readonly struct WeatherSaveData
{
    public bool TargetRaining { get; }
    public float RainIntensity { get; }
    public float Timer { get; }

    public WeatherSaveData(bool targetRaining, float rainIntensity, float timer)
    {
        TargetRaining = targetRaining;
        RainIntensity = rainIntensity;
        Timer = timer;
    }
}
