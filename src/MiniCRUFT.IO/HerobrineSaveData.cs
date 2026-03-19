using System.Numerics;

namespace MiniCRUFT.IO;

public readonly struct HerobrineSaveData
{
    public int Seed { get; }
    public Vector3 LastManifestPosition { get; }
    public Vector3 LastObservedPlayerPosition { get; }
    public float HauntPressure { get; }
    public float ManifestCooldown { get; }
    public float EventCooldown { get; }
    public float WorldEffectCooldown { get; }
    public float ActiveTimer { get; }
    public int EncounterCount { get; }
    public bool IsManifested { get; }

    public HerobrineSaveData(
        int seed,
        Vector3 lastManifestPosition,
        Vector3 lastObservedPlayerPosition,
        float hauntPressure,
        float manifestCooldown,
        float eventCooldown,
        float worldEffectCooldown,
        float activeTimer,
        int encounterCount,
        bool isManifested)
    {
        Seed = seed;
        LastManifestPosition = lastManifestPosition;
        LastObservedPlayerPosition = lastObservedPlayerPosition;
        HauntPressure = hauntPressure;
        ManifestCooldown = manifestCooldown;
        EventCooldown = eventCooldown;
        WorldEffectCooldown = worldEffectCooldown;
        ActiveTimer = activeTimer;
        EncounterCount = encounterCount;
        IsManifested = isManifested;
    }
}
