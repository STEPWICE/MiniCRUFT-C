using System;

namespace MiniCRUFT.World;

public static class TreeGenerator
{
    public static void GenerateOak(Chunk chunk, int x, int y, int z, Random rand)
    {
        GenerateSimpleTree(chunk, x, y, z, rand, BlockId.Wood, BlockId.Leaves, 4, 6, 2);
    }

    public static bool GenerateLargeOak(Chunk chunk, int x, int y, int z, Random rand, int minHeight, int maxHeight, int leafRadius, int canopyDepth)
    {
        if (minHeight < 4)
        {
            minHeight = 4;
        }
        if (maxHeight < minHeight + 1)
        {
            maxHeight = minHeight + 1;
        }
        if (leafRadius < 2)
        {
            leafRadius = 2;
        }
        if (canopyDepth < 2)
        {
            canopyDepth = 2;
        }

        int height = rand.Next(minHeight, maxHeight + 1);
        if (!CanPlaceLargeOak(chunk, x, y, z, height, leafRadius, canopyDepth))
        {
            return false;
        }

        for (int i = 0; i < height; i++)
        {
            int ty = y + i;
            if (ty >= Chunk.SizeY)
            {
                break;
            }
            for (int dx = 0; dx <= 1; dx++)
            {
                for (int dz = 0; dz <= 1; dz++)
                {
                    chunk.SetBlock(x + dx, ty, z + dz, BlockId.Wood);
                }
            }
        }

        int top = y + height;
        int canopyStart = top - canopyDepth;
        for (int ly = canopyStart; ly <= top + 1; ly++)
        {
            int layerFromTop = top - ly;
            int radius = leafRadius - layerFromTop / 2;
            if (ly >= top)
            {
                radius = Math.Max(1, radius - 1);
            }
            radius = Math.Max(1, radius);

            for (int lx = -radius; lx <= radius + 1; lx++)
            {
                for (int lz = -radius; lz <= radius + 1; lz++)
                {
                    int ax = x + lx;
                    int az = z + lz;
                    if (ax < 0 || ax >= Chunk.SizeX || az < 0 || az >= Chunk.SizeZ || ly < 0 || ly >= Chunk.SizeY)
                    {
                        continue;
                    }

                    int dx = lx < 0 ? lx : lx - 1;
                    int dz = lz < 0 ? lz : lz - 1;
                    if (dx * dx + dz * dz > radius * radius)
                    {
                        continue;
                    }

                    if (IsReplaceable(chunk.GetBlock(ax, ly, az)))
                    {
                        chunk.SetBlock(ax, ly, az, BlockId.Leaves);
                    }
                }
            }
        }

        return true;
    }

    public static void GenerateBirch(Chunk chunk, int x, int y, int z, Random rand)
    {
        GenerateSimpleTree(chunk, x, y, z, rand, BlockId.BirchWood, BlockId.BirchLeaves, 5, 7, 2);
    }

    public static void GenerateSpruce(Chunk chunk, int x, int y, int z, Random rand)
    {
        int height = rand.Next(6, 9);
        for (int i = 0; i < height; i++)
        {
            int ty = y + i;
            if (ty >= Chunk.SizeY)
            {
                break;
            }
            chunk.SetBlock(x, ty, z, BlockId.SpruceWood);
        }

        int leafStart = y + height - 4;
        int leafEnd = y + height + 1;
        int radius = 2;
        for (int ly = leafStart; ly <= leafEnd; ly++)
        {
            int r = Math.Max(1, radius - (leafEnd - ly) / 2);
            for (int lx = -r; lx <= r; lx++)
            {
                for (int lz = -r; lz <= r; lz++)
                {
                    int ax = x + lx;
                    int az = z + lz;
                    if (ax < 0 || ax >= Chunk.SizeX || az < 0 || az >= Chunk.SizeZ || ly < 0 || ly >= Chunk.SizeY)
                    {
                        continue;
                    }

                    if (chunk.GetBlock(ax, ly, az) == BlockId.Air)
                    {
                        chunk.SetBlock(ax, ly, az, BlockId.SpruceLeaves);
                    }
                }
            }
        }
    }

    private static void GenerateSimpleTree(Chunk chunk, int x, int y, int z, Random rand, BlockId wood, BlockId leaves, int minHeight, int maxHeight, int leafRadius)
    {
        int height = rand.Next(minHeight, maxHeight);
        for (int i = 0; i < height; i++)
        {
            int ty = y + i;
            if (ty >= Chunk.SizeY)
            {
                break;
            }
            chunk.SetBlock(x, ty, z, wood);
        }

        int leafStart = y + height - 2;
        int leafEnd = y + height + 1;
        for (int ly = leafStart; ly <= leafEnd; ly++)
        {
            int radius = ly == leafEnd ? 1 : leafRadius;
            for (int lx = -radius; lx <= radius; lx++)
            {
                for (int lz = -radius; lz <= radius; lz++)
                {
                    int ax = x + lx;
                    int az = z + lz;
                    if (ax < 0 || ax >= Chunk.SizeX || az < 0 || az >= Chunk.SizeZ || ly < 0 || ly >= Chunk.SizeY)
                    {
                        continue;
                    }

                    if (chunk.GetBlock(ax, ly, az) == BlockId.Air)
                    {
                        chunk.SetBlock(ax, ly, az, leaves);
                    }
                }
            }
        }
    }

    private static bool CanPlaceLargeOak(Chunk chunk, int x, int y, int z, int height, int leafRadius, int canopyDepth)
    {
        int minX = x - leafRadius;
        int maxX = x + 1 + leafRadius;
        int minZ = z - leafRadius;
        int maxZ = z + 1 + leafRadius;
        int maxY = y + height + 1;

        if (minX < 0 || minZ < 0 || maxX >= Chunk.SizeX || maxZ >= Chunk.SizeZ)
        {
            return false;
        }

        if (y < 1 || maxY >= Chunk.SizeY)
        {
            return false;
        }

        for (int dx = 0; dx <= 1; dx++)
        {
            for (int dz = 0; dz <= 1; dz++)
            {
                var baseBlock = chunk.GetBlock(x + dx, y - 1, z + dz);
                if (baseBlock != BlockId.Grass && baseBlock != BlockId.Dirt)
                {
                    return false;
                }
            }
        }

        for (int i = 0; i < height; i++)
        {
            int ty = y + i;
            for (int dx = 0; dx <= 1; dx++)
            {
                for (int dz = 0; dz <= 1; dz++)
                {
                    if (!IsReplaceable(chunk.GetBlock(x + dx, ty, z + dz)))
                    {
                        return false;
                    }
                }
            }
        }

        int top = y + height;
        int canopyStart = top - canopyDepth;
        for (int ly = canopyStart; ly <= top + 1; ly++)
        {
            int layerFromTop = top - ly;
            int radius = leafRadius - layerFromTop / 2;
            if (ly >= top)
            {
                radius = Math.Max(1, radius - 1);
            }
            radius = Math.Max(1, radius);

            for (int lx = -radius; lx <= radius + 1; lx++)
            {
                for (int lz = -radius; lz <= radius + 1; lz++)
                {
                    int ax = x + lx;
                    int az = z + lz;
                    int dx = lx < 0 ? lx : lx - 1;
                    int dz = lz < 0 ? lz : lz - 1;
                    if (dx * dx + dz * dz > radius * radius)
                    {
                        continue;
                    }

                    if (!IsReplaceable(chunk.GetBlock(ax, ly, az)))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static bool IsReplaceable(BlockId block)
    {
        return block == BlockId.Air ||
               block == BlockId.TallGrass ||
               block == BlockId.Flower ||
               block == BlockId.Leaves ||
               block == BlockId.BirchLeaves ||
               block == BlockId.SpruceLeaves;
    }
}
