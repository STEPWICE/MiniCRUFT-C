using System.Diagnostics.CodeAnalysis;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.IO;

public interface IChunkLoadQueue
{
    void Enqueue(ChunkCoord coord);

    bool TryDequeueCompletedChunk([NotNullWhen(true)] out Chunk? chunk);

    bool TryDequeueMissingChunk(out ChunkCoord coord);
}
