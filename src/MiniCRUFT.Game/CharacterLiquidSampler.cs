using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.World;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Game;

public static class CharacterLiquidSampler
{
    public static CharacterLiquidState Sample(WorldType world, Vector3 position, float width, float height)
    {
        if (width <= 0f || height <= 0f)
        {
            return default;
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

        float playerVolume = Math.Max(width * width * height, 0.0001f);
        float waterVolume = 0f;
        float lavaVolume = 0f;

        for (int x = minBx; x <= maxBx; x++)
        {
            for (int y = minBy; y <= maxBy; y++)
            {
                float blockMinY = y;
                float blockMaxY = y + 1f;
                float overlapY = Math.Min(maxY, blockMaxY) - Math.Max(minY, blockMinY);
                if (overlapY <= 0f)
                {
                    continue;
                }

                for (int z = minBz; z <= maxBz; z++)
                {
                    if (!world.HasChunkAt(x, z))
                    {
                        continue;
                    }

                    BlockId block = world.GetBlock(x, y, z);
                    if (!LiquidBlocks.IsLiquid(block))
                    {
                        continue;
                    }

                    float overlapX = Math.Min(maxX, x + 1f) - Math.Max(minX, x);
                    if (overlapX <= 0f)
                    {
                        continue;
                    }

                    float overlapZ = Math.Min(maxZ, z + 1f) - Math.Max(minZ, z);
                    if (overlapZ <= 0f)
                    {
                        continue;
                    }

                    float volume = overlapX * overlapY * overlapZ;
                    if (volume <= 0f)
                    {
                        continue;
                    }

                    if (LiquidBlocks.IsWater(block))
                    {
                        waterVolume += volume;
                    }
                    else if (LiquidBlocks.IsLava(block))
                    {
                        lavaVolume += volume;
                    }
                }
            }
        }

        return new CharacterLiquidState(
            waterVolume > 0f,
            lavaVolume > 0f,
            waterVolume / playerVolume,
            lavaVolume / playerVolume);
    }
}
