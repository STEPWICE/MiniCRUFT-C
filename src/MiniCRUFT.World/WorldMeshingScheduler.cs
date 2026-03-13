namespace MiniCRUFT.World;

public sealed class WorldMeshingScheduler
{
    public int ProcessDirtySubchunkSnapshots(WorldHost world, int maxSubchunks = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (maxSubchunks < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSubchunks));
        }

        var processedCount = 0;
        while (processedCount < maxSubchunks && world.TryClaimNextDirtySubchunkSnapshot(out var snapshot))
        {
            if (!world.TryCreateClaimedSubchunkMeshingOutput(snapshot!, SubchunkMeshing.BuildVisibleFaces(snapshot!), out var output))
            {
                continue;
            }

            if (!world.TryCompleteSubchunkMeshing(snapshot!, output!))
            {
                continue;
            }

            processedCount++;
        }

        return processedCount;
    }
}
