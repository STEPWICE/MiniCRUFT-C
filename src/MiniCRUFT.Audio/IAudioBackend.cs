using System;

namespace MiniCRUFT.Audio;

public interface IAudioBackend : IDisposable
{
    bool IsAvailable { get; }
    void Play(string path, float volume, float pan = 0f);
}
