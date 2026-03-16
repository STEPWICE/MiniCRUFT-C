using System;
using MiniCRUFT.Core;

namespace MiniCRUFT.Audio;

public static class AudioBackendFactory
{
    public static IAudioBackend Create(AudioConfig config)
    {
        if (!config.Enabled)
        {
            return new NoAudioBackend();
        }

        if (OperatingSystem.IsWindows())
        {
            return new NaudioBackend(config.MaxActive);
        }

        return new NoAudioBackend();
    }
}
