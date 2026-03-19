using System.Numerics;

namespace MiniCRUFT.Core;

public readonly struct AtmosphereFrame
{
    public Vector3 SkyTop { get; }
    public Vector3 SkyBottom { get; }
    public Vector4 FogColor { get; }
    public float FogStart { get; }
    public float FogEnd { get; }
    public float SunIntensity { get; }
    public float MoonIntensity { get; }
    public int MoonPhaseIndex { get; }
    public float RainIntensity { get; }
    public float LightningFlashIntensity { get; }
    public float TimeSeconds { get; }
    public float TimeOfDay { get; }
    public float CloudOffset { get; }

    public AtmosphereFrame(Vector3 skyTop, Vector3 skyBottom, Vector4 fogColor, float fogStart, float fogEnd, float sunIntensity, float moonIntensity, int moonPhaseIndex, float rainIntensity, float lightningFlashIntensity, float timeSeconds, float timeOfDay, float cloudOffset)
    {
        SkyTop = skyTop;
        SkyBottom = skyBottom;
        FogColor = fogColor;
        FogStart = fogStart;
        FogEnd = fogEnd;
        SunIntensity = sunIntensity;
        MoonIntensity = moonIntensity;
        MoonPhaseIndex = moonPhaseIndex;
        RainIntensity = rainIntensity;
        LightningFlashIntensity = lightningFlashIntensity;
        TimeSeconds = timeSeconds;
        TimeOfDay = timeOfDay;
        CloudOffset = cloudOffset;
    }
}
