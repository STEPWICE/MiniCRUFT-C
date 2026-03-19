using System;
using System.Numerics;

namespace MiniCRUFT.Core;

public readonly record struct SpatialAudioMix(float Volume, float Pan);

public static class SpatialAudio
{
    public static SpatialAudioMix Calculate(Vector3 listenerPosition, Vector3 sourcePosition, Vector3 listenerRight, AudioConfig config, float baseVolume)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (baseVolume <= 0f)
        {
            return new SpatialAudioMix(0f, 0f);
        }

        float innerRadius = Math.Max(0.5f, config.SpatialInnerRadius);
        float outerRadius = Math.Max(innerRadius + 1f, config.SpatialOuterRadius);

        Vector3 offset = sourcePosition - listenerPosition;
        float distance = offset.Length();

        float volume = Math.Clamp(baseVolume, 0f, 1f);
        if (distance > innerRadius)
        {
            float falloff = 1f - Math.Clamp((distance - innerRadius) / (outerRadius - innerRadius), 0f, 1f);
            volume *= falloff;
        }

        if (volume <= 0f)
        {
            return new SpatialAudioMix(0f, 0f);
        }

        float pan = 0f;
        if (listenerRight.LengthSquared() > float.Epsilon && distance > float.Epsilon)
        {
            Vector3 right = Vector3.Normalize(listenerRight);
            pan = Math.Clamp(Vector3.Dot(Vector3.Normalize(offset), right) * config.SpatialPanStrength, -1f, 1f);
        }

        return new SpatialAudioMix(volume, pan);
    }
}
