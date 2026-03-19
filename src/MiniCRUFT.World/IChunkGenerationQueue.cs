using MiniCRUFT.Core;
using System.Diagnostics.CodeAnalysis;

namespace MiniCRUFT.World;

public interface IChunkGenerationQueue
{
    void Enqueue(ChunkCoord coord);

    bool TryDequeueCompletedChunk([NotNullWhen(true)] out Chunk? chunk);
}
