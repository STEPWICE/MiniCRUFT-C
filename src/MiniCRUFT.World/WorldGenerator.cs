namespace MiniCRUFT.World;

public sealed class WorldGenerator
{
    public WorldGenerator(int seed)
    {
        Seed = seed;
    }

    public int Seed { get; }

    public Chunk GenerateChunk(ChunkCoordinate coordinate)
    {
        var chunk = new Chunk(coordinate);

        for (var localZ = 0; localZ < ChunkConstants.ChunkSizeZ; localZ++)
        {
            for (var localX = 0; localX < ChunkConstants.ChunkSizeX; localX++)
            {
                var worldX = coordinate.OriginX + localX;
                var worldZ = coordinate.OriginZ + localZ;
                var height = GetSurfaceHeight(worldX, worldZ);

                for (var y = 0; y <= height; y++)
                {
                    var blockId = ResolveBlock(y, height);
                    chunk.SetBlock(new LocalBlockCoordinate(localX, y, localZ), blockId);
                }
            }
        }

        chunk.State = ChunkState.ReadyVoxelData;

        return chunk;
    }

    private int GetSurfaceHeight(int worldX, int worldZ)
    {
        var noise = Hash(worldX, worldZ);
        var variation = noise % 5;
        return 48 + variation;
    }

    private BlockId ResolveBlock(int y, int surfaceHeight)
    {
        if (y == surfaceHeight)
        {
            return BlockId.Grass;
        }

        if (y >= surfaceHeight - 3)
        {
            return BlockId.Dirt;
        }

        return BlockId.Stone;
    }

    private int Hash(int worldX, int worldZ)
    {
        unchecked
        {
            var hash = Seed;
            hash = (hash * 397) ^ worldX;
            hash = (hash * 397) ^ worldZ;
            hash ^= hash >> 13;
            hash *= 16777619;
            return hash & int.MaxValue;
        }
    }
}
