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
            try
            {
                return new NaudioBackend(config.MaxActive);
            }
            catch (Exception ex)
            {
                Log.Warn($"Audio backend unavailable, falling back to no-op backend: {ex.Message}");
                return new NoAudioBackend();
            }
        }

        Log.Warn("Audio backend unavailable on this platform because the current implementation depends on NAudio; using no-op backend.");
        return new NoAudioBackend();
    }
}
