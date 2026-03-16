using MiniCRUFT.Core;
using MiniCRUFT.World;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Renderer;

public readonly struct ChunkNeighborhood
{
    public Chunk Center { get; }
    public Chunk? North { get; }
    public Chunk? South { get; }
    public Chunk? East { get; }
    public Chunk? West { get; }

    public ChunkNeighborhood(Chunk center, Chunk? north, Chunk? south, Chunk? east, Chunk? west)
    {
        Center = center;
        North = north;
        South = south;
        East = east;
        West = west;
    }

    public static ChunkNeighborhood FromWorld(WorldType world, Chunk center)
    {
        var north = world.GetChunk(new ChunkCoord(center.ChunkX, center.ChunkZ - 1));
        var south = world.GetChunk(new ChunkCoord(center.ChunkX, center.ChunkZ + 1));
        var east = world.GetChunk(new ChunkCoord(center.ChunkX + 1, center.ChunkZ));
        var west = world.GetChunk(new ChunkCoord(center.ChunkX - 1, center.ChunkZ));

        return new ChunkNeighborhood(center, north, south, east, west);
    }

    public BlockId GetBlock(int x, int y, int z)
    {
        if (y < 0 || y >= Chunk.SizeY)
        {
            return BlockId.Air;
        }

        if (x < 0)
        {
            return West?.GetBlock(x + Chunk.SizeX, y, z) ?? BlockId.Air;
        }
        if (x >= Chunk.SizeX)
        {
            return East?.GetBlock(x - Chunk.SizeX, y, z) ?? BlockId.Air;
        }
        if (z < 0)
        {
            return North?.GetBlock(x, y, z + Chunk.SizeZ) ?? BlockId.Air;
        }
        if (z >= Chunk.SizeZ)
        {
            return South?.GetBlock(x, y, z - Chunk.SizeZ) ?? BlockId.Air;
        }

        return Center.GetBlock(x, y, z);
    }

    public bool TryGetBlock(int x, int y, int z, out BlockId block)
    {
        if (y < 0 || y >= Chunk.SizeY)
        {
            block = BlockId.Air;
            return true;
        }

        if (x < 0)
        {
            if (West == null)
            {
                block = BlockId.Air;
                return false;
            }
            block = West.GetBlock(x + Chunk.SizeX, y, z);
            return true;
        }
        if (x >= Chunk.SizeX)
        {
            if (East == null)
            {
                block = BlockId.Air;
                return false;
            }
            block = East.GetBlock(x - Chunk.SizeX, y, z);
            return true;
        }
        if (z < 0)
        {
            if (North == null)
            {
                block = BlockId.Air;
                return false;
            }
            block = North.GetBlock(x, y, z + Chunk.SizeZ);
            return true;
        }
        if (z >= Chunk.SizeZ)
        {
            if (South == null)
            {
                block = BlockId.Air;
                return false;
            }
            block = South.GetBlock(x, y, z - Chunk.SizeZ);
            return true;
        }

        block = Center.GetBlock(x, y, z);
        return true;
    }

    public byte GetSkyLight(int x, int y, int z)
    {
        if (y < 0 || y >= Chunk.SizeY)
        {
            return 0;
        }

        if (x < 0)
        {
            return West?.GetSkyLight(x + Chunk.SizeX, y, z) ?? (byte)0;
        }
        if (x >= Chunk.SizeX)
        {
            return East?.GetSkyLight(x - Chunk.SizeX, y, z) ?? (byte)0;
        }
        if (z < 0)
        {
            return North?.GetSkyLight(x, y, z + Chunk.SizeZ) ?? (byte)0;
        }
        if (z >= Chunk.SizeZ)
        {
            return South?.GetSkyLight(x, y, z - Chunk.SizeZ) ?? (byte)0;
        }

        return Center.GetSkyLight(x, y, z);
    }

    public byte GetTorchLight(int x, int y, int z)
    {
        if (y < 0 || y >= Chunk.SizeY)
        {
            return 0;
        }

        if (x < 0)
        {
            return West?.GetTorchLight(x + Chunk.SizeX, y, z) ?? (byte)0;
        }
        if (x >= Chunk.SizeX)
        {
            return East?.GetTorchLight(x - Chunk.SizeX, y, z) ?? (byte)0;
        }
        if (z < 0)
        {
            return North?.GetTorchLight(x, y, z + Chunk.SizeZ) ?? (byte)0;
        }
        if (z >= Chunk.SizeZ)
        {
            return South?.GetTorchLight(x, y, z - Chunk.SizeZ) ?? (byte)0;
        }

        return Center.GetTorchLight(x, y, z);
    }

    public BiomeId GetBiome(int x, int z)
    {
        if (x < 0)
        {
            return West?.GetBiome(x + Chunk.SizeX, z) ?? BiomeId.Plains;
        }
        if (x >= Chunk.SizeX)
        {
            return East?.GetBiome(x - Chunk.SizeX, z) ?? BiomeId.Plains;
        }
        if (z < 0)
        {
            return North?.GetBiome(x, z + Chunk.SizeZ) ?? BiomeId.Plains;
        }
        if (z >= Chunk.SizeZ)
        {
            return South?.GetBiome(x, z - Chunk.SizeZ) ?? BiomeId.Plains;
        }

        return Center.GetBiome(x, z);
    }
}
