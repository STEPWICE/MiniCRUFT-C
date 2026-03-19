using System.Collections.Generic;
using MiniCRUFT.Core;

namespace MiniCRUFT.Game;

internal enum ChunkRequestState
{
    Requested,
    InFlight,
    Loaded
}

internal sealed class ChunkRequestTracker
{
    private readonly Dictionary<ChunkCoord, ChunkRequestState> _states = new();

    public bool IsPending(ChunkCoord coord)
    {
        return _states.TryGetValue(coord, out var state) && state != ChunkRequestState.Loaded;
    }

    public bool TryMarkRequested(ChunkCoord coord)
    {
        if (_states.ContainsKey(coord))
        {
            return false;
        }

        _states[coord] = ChunkRequestState.Requested;
        return true;
    }

    public bool TryMarkInFlight(ChunkCoord coord)
    {
        if (!_states.TryGetValue(coord, out var state) || state != ChunkRequestState.Requested)
        {
            return false;
        }

        _states[coord] = ChunkRequestState.InFlight;
        return true;
    }

    public bool MarkObservedLoaded(ChunkCoord coord)
    {
        if (_states.TryGetValue(coord, out var state) && state == ChunkRequestState.Loaded)
        {
            return false;
        }

        _states[coord] = ChunkRequestState.Loaded;
        return true;
    }

    public bool TryAcceptGenerated(ChunkCoord coord)
    {
        if (!_states.TryGetValue(coord, out var state))
        {
            return false;
        }

        if (state == ChunkRequestState.Loaded)
        {
            return false;
        }

        _states[coord] = ChunkRequestState.Loaded;
        return true;
    }

    public void Forget(ChunkCoord coord)
    {
        _states.Remove(coord);
    }
}
