using System;
using System.Collections.Generic;
using NAudio.Vorbis;
using NAudio.Wave;

namespace MiniCRUFT.Audio;

public sealed class NaudioBackend : IAudioBackend
{
    private readonly List<IWavePlayer> _active = new();
    private readonly object _sync = new();
    private readonly int _maxActive;

    public NaudioBackend(int maxActive)
    {
        _maxActive = Math.Max(1, maxActive);
    }

    public bool IsAvailable => true;

    public void Play(string path, float volume)
    {
        lock (_sync)
        {
            if (_active.Count >= _maxActive)
            {
                return;
            }

            try
            {
                IWavePlayer output = new WaveOutEvent();
                WaveStream reader = path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)
                    ? new VorbisWaveReader(path)
                    : new AudioFileReader(path);

                var channel = new WaveChannel32(reader) { Volume = volume };
                output.Init(channel);
                output.PlaybackStopped += (_, __) =>
                {
                    lock (_sync)
                    {
                        output.Dispose();
                        channel.Dispose();
                        reader.Dispose();
                        _active.Remove(output);
                    }
                };

                _active.Add(output);
                output.Play();
            }
            catch
            {
                // ignore backend exceptions, leave audio disabled for this clip
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            foreach (var output in _active)
            {
                output.Dispose();
            }
            _active.Clear();
        }
    }
}
