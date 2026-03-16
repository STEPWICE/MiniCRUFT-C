using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.IO;

public interface IChunkStorage
{
    void SaveChunk(Chunk chunk);
    Chunk? LoadChunk(ChunkCoord coord);
}
