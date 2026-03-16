using System;

namespace MiniCRUFT.World;

public sealed class Chunk
{
    public const int SizeX = 16;
    public const int SizeZ = 16;
    public const int SizeY = 256;

    public readonly BlockId[] Blocks;
    public readonly byte[] SkyLight;
    public readonly byte[] TorchLight;
    public readonly BiomeId[] Biomes;

    public readonly int ChunkX;
    public readonly int ChunkZ;

    public bool IsDirty { get; private set; } = true;
    public bool LightingDirty { get; private set; } = true;
    public bool SaveDirty { get; private set; } = true;

    public object SyncRoot { get; } = new();

    public Chunk(int chunkX, int chunkZ)
    {
        ChunkX = chunkX;
        ChunkZ = chunkZ;
        Blocks = new BlockId[SizeX * SizeY * SizeZ];
        SkyLight = new byte[Blocks.Length];
        TorchLight = new byte[Blocks.Length];
        Biomes = new BiomeId[SizeX * SizeZ];
    }

    public void MarkDirty() => IsDirty = true;
    public void ClearDirty() => IsDirty = false;
    public void MarkLightingDirty() => LightingDirty = true;
    public void ClearLightingDirty() => LightingDirty = false;
    public void MarkSaveDirty() => SaveDirty = true;
    public void ClearSaveDirty() => SaveDirty = false;

    public static int GetIndex(int x, int y, int z)
    {
        return x + (z * SizeX) + (y * SizeX * SizeZ);
    }

    public static int GetBiomeIndex(int x, int z) => x + z * SizeX;

    public BlockId GetBlock(int x, int y, int z)
    {
        if (x < 0 || x >= SizeX || z < 0 || z >= SizeZ || y < 0 || y >= SizeY)
        {
            return BlockId.Air;
        }

        return Blocks[GetIndex(x, y, z)];
    }

    public void SetBlock(int x, int y, int z, BlockId id)
    {
        if (x < 0 || x >= SizeX || z < 0 || z >= SizeZ || y < 0 || y >= SizeY)
        {
            return;
        }

        Blocks[GetIndex(x, y, z)] = id;
        IsDirty = true;
        SaveDirty = true;
    }

    public byte GetSkyLight(int x, int y, int z)
    {
        if (x < 0 || x >= SizeX || z < 0 || z >= SizeZ || y < 0 || y >= SizeY)
        {
            return 0;
        }

        return SkyLight[GetIndex(x, y, z)];
    }

    public byte GetTorchLight(int x, int y, int z)
    {
        if (x < 0 || x >= SizeX || z < 0 || z >= SizeZ || y < 0 || y >= SizeY)
        {
            return 0;
        }

        return TorchLight[GetIndex(x, y, z)];
    }

    public void SetSkyLight(int x, int y, int z, byte value)
    {
        if (x < 0 || x >= SizeX || z < 0 || z >= SizeZ || y < 0 || y >= SizeY)
        {
            return;
        }

        SkyLight[GetIndex(x, y, z)] = value;
    }

    public void SetTorchLight(int x, int y, int z, byte value)
    {
        if (x < 0 || x >= SizeX || z < 0 || z >= SizeZ || y < 0 || y >= SizeY)
        {
            return;
        }

        TorchLight[GetIndex(x, y, z)] = value;
    }

    public void SetBiome(int x, int z, BiomeId biome)
    {
        if (x < 0 || x >= SizeX || z < 0 || z >= SizeZ)
        {
            return;
        }

        Biomes[GetBiomeIndex(x, z)] = biome;
    }

    public BiomeId GetBiome(int x, int z)
    {
        if (x < 0 || x >= SizeX || z < 0 || z >= SizeZ)
        {
            return BiomeId.Plains;
        }

        return Biomes[GetBiomeIndex(x, z)];
    }
}
