using System;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.World;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Game;

public readonly struct RaycastResult
{
    public readonly bool Hit;
    public readonly BlockCoord Block;
    public readonly BlockCoord Adjacent;
    public readonly float Distance;

    public RaycastResult(bool hit, BlockCoord block, BlockCoord adjacent, float distance)
    {
        Hit = hit;
        Block = block;
        Adjacent = adjacent;
        Distance = distance;
    }
}

public static class WorldRaycaster
{
    public static RaycastResult Raycast(WorldType world, Vector3 origin, Vector3 direction, float maxDistance)
    {
        if (direction.LengthSquared() < 1e-6f)
        {
            return new RaycastResult(false, new BlockCoord(), new BlockCoord(), float.PositiveInfinity);
        }

        direction = Vector3.Normalize(direction);

        int x = MathUtil.FloorToInt(origin.X);
        int y = MathUtil.FloorToInt(origin.Y);
        int z = MathUtil.FloorToInt(origin.Z);

        int stepX = direction.X >= 0 ? 1 : -1;
        int stepY = direction.Y >= 0 ? 1 : -1;
        int stepZ = direction.Z >= 0 ? 1 : -1;

        float tMaxX = IntBound(origin.X, direction.X);
        float tMaxY = IntBound(origin.Y, direction.Y);
        float tMaxZ = IntBound(origin.Z, direction.Z);

        float tDeltaX = direction.X == 0 ? float.PositiveInfinity : MathF.Abs(1f / direction.X);
        float tDeltaY = direction.Y == 0 ? float.PositiveInfinity : MathF.Abs(1f / direction.Y);
        float tDeltaZ = direction.Z == 0 ? float.PositiveInfinity : MathF.Abs(1f / direction.Z);

        int lastAxis = -1;

        float dist = 0f;
        while (dist <= maxDistance)
        {
            if (world.GetBlock(x, y, z) != BlockId.Air)
            {
                var hit = new BlockCoord(x, y, z);
                BlockCoord adjacent = hit;
                if (lastAxis == 0)
                {
                    adjacent = new BlockCoord(x - stepX, y, z);
                }
                else if (lastAxis == 1)
                {
                    adjacent = new BlockCoord(x, y - stepY, z);
                }
                else if (lastAxis == 2)
                {
                    adjacent = new BlockCoord(x, y, z - stepZ);
                }
                return new RaycastResult(true, hit, adjacent, dist);
            }

            if (tMaxX < tMaxY)
            {
                if (tMaxX < tMaxZ)
                {
                    dist = tMaxX;
                    lastAxis = 0;
                    x += stepX;
                    tMaxX += tDeltaX;
                }
                else
                {
                    dist = tMaxZ;
                    lastAxis = 2;
                    z += stepZ;
                    tMaxZ += tDeltaZ;
                }
            }
            else
            {
                if (tMaxY < tMaxZ)
                {
                    dist = tMaxY;
                    lastAxis = 1;
                    y += stepY;
                    tMaxY += tDeltaY;
                }
                else
                {
                    dist = tMaxZ;
                    lastAxis = 2;
                    z += stepZ;
                    tMaxZ += tDeltaZ;
                }
            }
        }

        return new RaycastResult(false, new BlockCoord(), new BlockCoord(), float.PositiveInfinity);
    }

    private static float IntBound(float s, float ds)
    {
        if (ds == 0)
        {
            return float.PositiveInfinity;
        }

        if (ds > 0)
        {
            float sFrac = s - MathF.Floor(s);
            return (1f - sFrac) / ds;
        }
        float sFracNeg = s - MathF.Floor(s);
        return sFracNeg / -ds;
    }
}
