namespace MiniCRUFT.Core;

public sealed class SurvivalConfig
{
    public bool Enabled { get; set; } = true;
    public int MaxHunger { get; set; } = 20;
    public float StartingHunger { get; set; } = 20f;
    public float BaseDrainPerSecond { get; set; } = 0.0125f;
    public float SprintDrainMultiplier { get; set; } = 1.75f;
    public float LiquidDrainMultiplier { get; set; } = 1.2f;
    public int MinHungerToSprint { get; set; } = 4;
    public float StarvationDamageIntervalSeconds { get; set; } = 4f;
    public int StarvationDamage { get; set; } = 1;
    public bool EnableRest { get; set; } = true;
    public float RestMinSunIntensity { get; set; } = 0.12f;
    public float RestThreatRadius { get; set; } = 24f;
    public float RestWakeTimeOfDay { get; set; } = 0.23f;
}
