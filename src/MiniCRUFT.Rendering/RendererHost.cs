using MiniCRUFT.World;

namespace MiniCRUFT.Rendering;

public enum RendererSubchunkUploadState
{
    PendingUpload,
    Uploaded
}

public sealed class RendererHost
{
    private const int DefaultMaxUploadsPerCall = 16;

    private readonly Dictionary<(ChunkCoordinate ChunkCoordinate, int SubchunkIndex), RendererSubchunkUploadEntry> _subchunkUploads = new();
    private readonly Queue<PendingUploadQueueEntry> _pendingUploadQueue = new();

    public int PendingSubchunkUploadCount => _subchunkUploads.Values.Count(entry => entry.State == RendererSubchunkUploadState.PendingUpload);

    public int UploadedSubchunkMeshCount => _subchunkUploads.Values.Count(entry => entry.State == RendererSubchunkUploadState.Uploaded);

    public void Initialize()
    {
    }

    public int PickupBuiltSubchunkMeshingOutputs(WorldHost world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var consumedCount = 0;
        while (world.TryDrainNextBuiltSubchunkMeshingOutput(out var output))
        {
            var consumedOutput = output!;
            var key = (consumedOutput.ChunkCoordinate, consumedOutput.SubchunkIndex);
            var nextPendingVersion = _subchunkUploads.TryGetValue(key, out var existingEntry)
                ? existingEntry.PendingVersion + 1
                : 1;

            _subchunkUploads[key] = new RendererSubchunkUploadEntry(
                consumedOutput,
                RendererSubchunkUploadState.PendingUpload,
                nextPendingVersion);
            _pendingUploadQueue.Enqueue(new PendingUploadQueueEntry(key.ChunkCoordinate, key.SubchunkIndex, nextPendingVersion));
            consumedCount++;
        }

        return consumedCount;
    }

    public int ProcessPendingUploads(int maxUploads = DefaultMaxUploadsPerCall)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxUploads);

        if (maxUploads == 0)
        {
            return 0;
        }

        var processedCount = 0;
        while (processedCount < maxUploads && _pendingUploadQueue.Count > 0)
        {
            var nextPending = _pendingUploadQueue.Dequeue();
            var key = (nextPending.ChunkCoordinate, nextPending.SubchunkIndex);
            if (!_subchunkUploads.TryGetValue(key, out var currentEntry))
            {
                continue;
            }

            if (currentEntry.State != RendererSubchunkUploadState.PendingUpload || currentEntry.PendingVersion != nextPending.PendingVersion)
            {
                continue;
            }

            _subchunkUploads[key] = currentEntry with { State = RendererSubchunkUploadState.Uploaded };
            processedCount++;
        }

        return processedCount;
    }

    public bool TryGetSubchunkUploadState(ChunkCoordinate chunkCoordinate, int subchunkIndex, out RendererSubchunkUploadState state)
    {
        if (_subchunkUploads.TryGetValue((chunkCoordinate, subchunkIndex), out var entry))
        {
            state = entry.State;
            return true;
        }

        state = default;
        return false;
    }

    public bool TryGetUploadedSubchunkMesh(ChunkCoordinate chunkCoordinate, int subchunkIndex, out SubchunkMeshingOutput? output)
    {
        if (_subchunkUploads.TryGetValue((chunkCoordinate, subchunkIndex), out var entry)
            && entry.State == RendererSubchunkUploadState.Uploaded)
        {
            output = entry.Output;
            return true;
        }

        output = null;
        return false;
    }

    public void RenderFrame()
    {
    }

    public void Shutdown()
    {
        _pendingUploadQueue.Clear();
        _subchunkUploads.Clear();
    }

    private sealed record RendererSubchunkUploadEntry(
        SubchunkMeshingOutput Output,
        RendererSubchunkUploadState State,
        int PendingVersion);

    private readonly record struct PendingUploadQueueEntry(
        ChunkCoordinate ChunkCoordinate,
        int SubchunkIndex,
        int PendingVersion);
}
