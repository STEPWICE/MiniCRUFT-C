using System.Numerics;

namespace MiniCRUFT.Core;

public readonly struct AtmosphereFrame
{
    public Vector3 SkyTop { get; }
    public Vector3 SkyBottom { get; }
    public Vector4 FogColor { get; }
    public float SunIntensity { get; }
    public float TimeSeconds { get; }
    public float TimeOfDay { get; }
    public float CloudOffset { get; }

    public AtmosphereFrame(Vector3 skyTop, Vector3 skyBottom, Vector4 fogColor, float sunIntensity, float timeSeconds, float timeOfDay, float cloudOffset)
    {
        SkyTop = skyTop;
        SkyBottom = skyBottom;
        FogColor = fogColor;
        SunIntensity = sunIntensity;
        TimeSeconds = timeSeconds;
        TimeOfDay = timeOfDay;
        CloudOffset = cloudOffset;
    }
}
