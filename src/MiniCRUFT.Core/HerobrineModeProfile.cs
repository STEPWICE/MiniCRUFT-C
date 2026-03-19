namespace MiniCRUFT.Core;

public readonly struct HerobrineModeProfile
{
    public string Name { get; }
    public string Description { get; }
    public float PressureFloor { get; }
    public float PressureRiseMultiplier { get; }
    public float PressureFallMultiplier { get; }
    public float ManifestIntervalMultiplier { get; }
    public float EventCooldownMultiplier { get; }
    public float WorldEffectCooldownMultiplier { get; }
    public float WorldEffectIntensityMultiplier { get; }
    public float BehindPlayerChanceBonus { get; }
    public float DirectLookDespawnMultiplier { get; }
    public float HiddenTimeoutMultiplier { get; }
    public float ManifestDurationMultiplier { get; }

    public HerobrineModeProfile(
        string name,
        string description,
        float pressureFloor,
        float pressureRiseMultiplier,
        float pressureFallMultiplier,
        float manifestIntervalMultiplier,
        float eventCooldownMultiplier,
        float worldEffectCooldownMultiplier,
        float worldEffectIntensityMultiplier,
        float behindPlayerChanceBonus,
        float directLookDespawnMultiplier,
        float hiddenTimeoutMultiplier,
        float manifestDurationMultiplier)
    {
        Name = name;
        Description = description;
        PressureFloor = pressureFloor;
        PressureRiseMultiplier = pressureRiseMultiplier;
        PressureFallMultiplier = pressureFallMultiplier;
        ManifestIntervalMultiplier = manifestIntervalMultiplier;
        EventCooldownMultiplier = eventCooldownMultiplier;
        WorldEffectCooldownMultiplier = worldEffectCooldownMultiplier;
        WorldEffectIntensityMultiplier = worldEffectIntensityMultiplier;
        BehindPlayerChanceBonus = behindPlayerChanceBonus;
        DirectLookDespawnMultiplier = directLookDespawnMultiplier;
        HiddenTimeoutMultiplier = hiddenTimeoutMultiplier;
        ManifestDurationMultiplier = manifestDurationMultiplier;
    }
}
