using System;
using System.Collections.Generic;
using MiniCRUFT.Core;
using NAudio.Vorbis;
using NAudio.Wave;
using System.Threading;

namespace MiniCRUFT.Audio;

public sealed class NaudioBackend : IAudioBackend
{
    private readonly List<ActiveSound> _active = new();
    private readonly object _sync = new();
    private readonly int _maxActive;
    private long _nextSequence;

    public NaudioBackend(int maxActive)
    {
        _maxActive = Math.Max(1, maxActive);
    }

    public bool IsAvailable => true;

    public void Play(string path, float volume, float pan = 0f)
    {
        if (volume <= 0f)
        {
            return;
        }

        ActiveSound? sound = null;
        ActiveSound? evicted = null;

        try
        {
            IWavePlayer output = new WaveOutEvent();
            WaveStream reader = path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)
                ? new VorbisWaveReader(path)
                : new AudioFileReader(path);

            float clampedVolume = Math.Clamp(volume, 0f, 1f);
            float clampedPan = Math.Clamp(pan, -1f, 1f);
            var channel = new WaveChannel32(reader)
            {
                Volume = clampedVolume,
                Pan = clampedPan
            };
            output.Init(channel);
            sound = new ActiveSound(output, reader, channel, clampedVolume, clampedPan, Interlocked.Increment(ref _nextSequence));
            output.PlaybackStopped += (_, __) => Release(sound);

            lock (_sync)
            {
                PruneStoppedLocked();
                if (_active.Count >= _maxActive)
                {
                    evicted = RemoveLowestPriorityLocked();
                }

                _active.Add(sound);
            }

            output.Play();
        }
        catch (Exception ex)
        {
            Log.Warn($"Audio playback failed for '{path}': {ex.Message}");
            Release(sound);
        }
        finally
        {
            evicted?.Dispose();
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            foreach (var sound in _active)
            {
                sound.Dispose();
            }
            _active.Clear();
        }
    }

    private void Release(ActiveSound? sound)
    {
        if (sound == null)
        {
            return;
        }

        lock (_sync)
        {
            _active.Remove(sound);
        }

        sound.Dispose();
    }

    private void PruneStoppedLocked()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (_active[i].IsStopped)
            {
                var sound = _active[i];
                _active.RemoveAt(i);
                sound.Dispose();
            }
        }
    }

    private ActiveSound? RemoveLowestPriorityLocked()
    {
        if (_active.Count == 0)
        {
            return null;
        }

        int candidateIndex = 0;
        for (int i = 1; i < _active.Count; i++)
        {
            if (HasLowerPriority(_active[i], _active[candidateIndex]))
            {
                candidateIndex = i;
            }
        }

        var candidate = _active[candidateIndex];
        _active.RemoveAt(candidateIndex);
        return candidate;
    }

    private static bool HasLowerPriority(ActiveSound candidate, ActiveSound current)
    {
        if (candidate.Volume < current.Volume)
        {
            return true;
        }

        if (candidate.Volume > current.Volume)
        {
            return false;
        }

        return candidate.Sequence < current.Sequence;
    }

    private sealed class ActiveSound : IDisposable
    {
        private int _disposed;

        public IWavePlayer Output { get; }
        public WaveStream Reader { get; }
        public WaveChannel32 Channel { get; }
        public float Volume { get; }
        public float Pan { get; }
        public long Sequence { get; }

        public ActiveSound(IWavePlayer output, WaveStream reader, WaveChannel32 channel, float volume, float pan, long sequence)
        {
            Output = output;
            Reader = reader;
            Channel = channel;
            Volume = volume;
            Pan = pan;
            Sequence = sequence;
        }

        public bool IsStopped => Output is WaveOutEvent wave && wave.PlaybackState == PlaybackState.Stopped;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            try
            {
                Output.Dispose();
            }
            catch
            {
            }

            try
            {
                Channel.Dispose();
            }
            catch
            {
            }

            try
            {
                Reader.Dispose();
            }
            catch
            {
            }
        }
    }
}
