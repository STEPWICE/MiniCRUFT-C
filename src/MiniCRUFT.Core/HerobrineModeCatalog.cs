using System;
using System.Collections.Generic;

namespace MiniCRUFT.Core;

public static class HerobrineModeCatalog
{
    public const string Classic = "Classic";
    public const string Stalker = "Stalker";
    public const string Haunt = "Haunt";
    public const string Nightmare = "Nightmare";

    private static readonly HerobrineModeProfile[] _profiles =
    [
        new(
            Classic,
            "Balanced stalking and ambience.",
            pressureFloor: 0.08f,
            pressureRiseMultiplier: 1f,
            pressureFallMultiplier: 1f,
            manifestIntervalMultiplier: 1f,
            eventCooldownMultiplier: 1f,
            worldEffectCooldownMultiplier: 1f,
            worldEffectIntensityMultiplier: 1f,
            behindPlayerChanceBonus: 0f,
            directLookDespawnMultiplier: 1f,
            hiddenTimeoutMultiplier: 1f,
            manifestDurationMultiplier: 1f),
        new(
            Stalker,
            "Appears behind you more often.",
            pressureFloor: 0.06f,
            pressureRiseMultiplier: 1.1f,
            pressureFallMultiplier: 0.95f,
            manifestIntervalMultiplier: 0.82f,
            eventCooldownMultiplier: 0.9f,
            worldEffectCooldownMultiplier: 0.9f,
            worldEffectIntensityMultiplier: 0.95f,
            behindPlayerChanceBonus: 0.18f,
            directLookDespawnMultiplier: 0.9f,
            hiddenTimeoutMultiplier: 1.05f,
            manifestDurationMultiplier: 1.1f),
        new(
            Haunt,
            "More whispers and world effects.",
            pressureFloor: 0.05f,
            pressureRiseMultiplier: 1.25f,
            pressureFallMultiplier: 0.9f,
            manifestIntervalMultiplier: 0.72f,
            eventCooldownMultiplier: 0.7f,
            worldEffectCooldownMultiplier: 0.6f,
            worldEffectIntensityMultiplier: 1.25f,
            behindPlayerChanceBonus: 0.1f,
            directLookDespawnMultiplier: 0.85f,
            hiddenTimeoutMultiplier: 0.95f,
            manifestDurationMultiplier: 1.2f),
        new(
            Nightmare,
            "Fast pressure and frequent visits.",
            pressureFloor: 0.12f,
            pressureRiseMultiplier: 1.45f,
            pressureFallMultiplier: 0.8f,
            manifestIntervalMultiplier: 0.55f,
            eventCooldownMultiplier: 0.55f,
            worldEffectCooldownMultiplier: 0.45f,
            worldEffectIntensityMultiplier: 1.5f,
            behindPlayerChanceBonus: 0.32f,
            directLookDespawnMultiplier: 0.75f,
            hiddenTimeoutMultiplier: 0.8f,
            manifestDurationMultiplier: 1.35f)
    ];

    public static IReadOnlyList<HerobrineModeProfile> Profiles => _profiles;

    public static HerobrineModeProfile Get(string? mode)
    {
        return _profiles[FindIndex(mode)];
    }

    public static string Normalize(string? mode)
    {
        return _profiles[FindIndex(mode)].Name;
    }

    public static string Next(string? mode)
    {
        int index = FindIndex(mode);
        int nextIndex = (index + 1) % _profiles.Length;
        return _profiles[nextIndex].Name;
    }

    public static string Previous(string? mode)
    {
        int index = FindIndex(mode);
        int previousIndex = index - 1;
        if (previousIndex < 0)
        {
            previousIndex = _profiles.Length - 1;
        }

        return _profiles[previousIndex].Name;
    }

    private static int FindIndex(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            return 0;
        }

        for (int i = 0; i < _profiles.Length; i++)
        {
            if (string.Equals(_profiles[i].Name, mode, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return 0;
    }
}
