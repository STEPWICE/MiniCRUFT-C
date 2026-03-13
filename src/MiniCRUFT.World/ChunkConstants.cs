namespace MiniCRUFT.World;

public static class ChunkConstants
{
    public const int ChunkSizeX = 16;
    public const int ChunkSizeZ = 16;
    public const int ChunkHeight = 128;

    public const int SubchunkSize = 16;
    public const int SubchunkCountPerChunk = ChunkHeight / SubchunkSize;
}
