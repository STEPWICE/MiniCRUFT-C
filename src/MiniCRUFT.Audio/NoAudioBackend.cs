namespace MiniCRUFT.Audio;

public sealed class NoAudioBackend : IAudioBackend
{
    public bool IsAvailable => false;

    public void Play(string path, float volume)
    {
    }

    public void Dispose()
    {
    }
}
