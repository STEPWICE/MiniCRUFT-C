using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.World;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Game;

internal static class LiquidCurrentSampler
{
    private const float SurfaceHeightScale = 1f / (LiquidBlocks.MaxLevel + 1f);
    private const float DownwardPull = 0.65f;

    public static Vector3 SampleWaterCurrent(WorldType world, Vector3 position, float width, float height)
    {
        if (width <= 0f || height <= 0f)
        {
            return Vector3.Zero;
        }

        float halfWidth = width * 0.5f;
        float minX = position.X - halfWidth;
        float maxX = position.X + halfWidth;
        float minY = position.Y;
        float maxY = position.Y + height;
        float minZ = position.Z - halfWidth;
        float maxZ = position.Z + halfWidth;

        int minBx = MathUtil.FloorToInt(minX);
        int maxBx = MathUtil.FloorToInt(maxX);
        int minBy = MathUtil.FloorToInt(minY);
        int maxBy = MathUtil.FloorToInt(maxY);
        int minBz = MathUtil.FloorToInt(minZ);
        int maxBz = MathUtil.FloorToInt(maxZ);

        Vector3 totalCurrent = Vector3.Zero;
        int samples = 0;

        for (int x = minBx; x <= maxBx; x++)
        {
            for (int y = minBy; y <= maxBy; y++)
            {
                for (int z = minBz; z <= maxBz; z++)
                {
                    if (!world.HasChunkAt(x, z))
                    {
                        continue;
                    }

                    BlockId block = world.GetBlock(x, y, z);
                    if (!LiquidBlocks.IsWater(block))
                    {
                        continue;
                    }

                    Vector3 blockCurrent = SampleBlockCurrent(world, x, y, z, block);
                    if (blockCurrent.LengthSquared() <= float.Epsilon)
                    {
                        continue;
                    }

                    totalCurrent += blockCurrent;
                    samples++;
                }
            }
        }

        if (samples == 0)
        {
            return Vector3.Zero;
        }

        totalCurrent /= samples;
        float magnitude = totalCurrent.Length();
        if (magnitude > 1f)
        {
            totalCurrent /= magnitude;
        }

        return totalCurrent;
    }

    private static Vector3 SampleBlockCurrent(WorldType world, int x, int y, int z, BlockId block)
    {
        float surfaceHeight = GetSurfaceHeight(block);
        Vector3 current = Vector3.Zero;

        AccumulateHorizontalContribution(world, x, y, z, surfaceHeight, 1, 0, ref current);
        AccumulateHorizontalContribution(world, x, y, z, surfaceHeight, -1, 0, ref current);
        AccumulateHorizontalContribution(world, x, y, z, surfaceHeight, 0, 1, ref current);
        AccumulateHorizontalContribution(world, x, y, z, surfaceHeight, 0, -1, ref current);

        if (y > 0 && world.GetBlock(x, y - 1, z) == BlockId.Air)
        {
            current += new Vector3(0f, -surfaceHeight * DownwardPull, 0f);
        }

        return current;
    }

    private static void AccumulateHorizontalContribution(
        WorldType world,
        int x,
        int y,
        int z,
        float surfaceHeight,
        int dx,
        int dz,
        ref Vector3 current)
    {
        int nx = x + dx;
        int nz = z + dz;
        if (!world.HasChunkAt(nx, nz))
        {
            return;
        }

        BlockId neighbor = world.GetBlock(nx, y, nz);
        if (LiquidBlocks.IsWater(neighbor))
        {
            float neighborHeight = GetSurfaceHeight(neighbor);
            float difference = surfaceHeight - neighborHeight;
            if (difference > 0f)
            {
                current += new Vector3(dx * difference, 0f, dz * difference);
            }
        }
    }

    private static float GetSurfaceHeight(BlockId block)
    {
        return 1f - LiquidBlocks.GetLevel(block) * SurfaceHeightScale;
    }
}
