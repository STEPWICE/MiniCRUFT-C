using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.Renderer;

public interface IChunkRenderQueue
{
    void EnqueueChunk(Chunk chunk, ChunkNeighborhood neighbors, bool highPriority = false);

    void RefreshChunk(Chunk chunk, ChunkNeighborhood neighbors);

    void RemoveChunkMesh(ChunkCoord coord);
}
