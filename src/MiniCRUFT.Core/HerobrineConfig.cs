namespace MiniCRUFT.Core;

public sealed class HerobrineConfig
{
    public bool Enabled { get; set; } = false;
    public string Mode { get; set; } = HerobrineModeCatalog.Classic;
    public float MinManifestIntervalSeconds { get; set; } = 90f;
    public float MaxManifestIntervalSeconds { get; set; } = 240f;
    public float ManifestDurationSeconds { get; set; } = 6f;
    public float EventCooldownSeconds { get; set; } = 18f;
    public float WorldEffectCooldownSeconds { get; set; } = 45f;
    public float MinManifestDistance { get; set; } = 16f;
    public float MaxManifestDistance { get; set; } = 48f;
    public float BehindPlayerChance { get; set; } = 0.45f;
    public float NightBias { get; set; } = 0.75f;
    public float CaveBias { get; set; } = 0.55f;
    public float DirectLookDespawnSeconds { get; set; } = 0.7f;
    public float HiddenTimeoutSeconds { get; set; } = 4f;
    public float WorldEffectIntensity { get; set; } = 0.35f;
}
