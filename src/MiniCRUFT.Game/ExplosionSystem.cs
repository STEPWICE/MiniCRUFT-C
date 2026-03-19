using System;
using System.Collections.Generic;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.World;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Game;

public static class ExplosionSystem
{
    private const int ExplosionSampleCount = 16;
    private const float ExplosionRayStep = 0.3f;
    private const float ExplosionRayBaseStrength = 0.7f;
    private const float ExplosionRayStrengthRange = 0.6f;
    private const float ExplosionRayDecay = 0.225f;
    private static readonly Vector3[] ExplosionDirections = BuildExplosionDirections();

    public static int DestroyBlocks(
        WorldType world,
        WorldEditor editor,
        Vector3 center,
        float radius,
        float resistanceScale,
        int maxAffectedBlocks,
        BlockCoord? source = null,
        Action<BlockCoord>? primeTnt = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(editor);

        if (radius <= 0f || maxAffectedBlocks <= 0)
        {
            return 0;
        }

        var sourceCoord = source ?? new BlockCoord(
            MathUtil.FloorToInt(center.X),
            MathUtil.FloorToInt(center.Y),
            MathUtil.FloorToInt(center.Z));

        var destroyed = new HashSet<BlockCoord>();
        int affectedBlocks = 0;

        if (source.HasValue && editor.SetBlock(sourceCoord.X, sourceCoord.Y, sourceCoord.Z, BlockId.Air))
        {
            destroyed.Add(sourceCoord);
            affectedBlocks++;
        }

        for (int i = 0; i < ExplosionDirections.Length && affectedBlocks < maxAffectedBlocks; i++)
        {
            Vector3 direction = ExplosionDirections[i];
            float rayStrength = radius * GetRayStrength(sourceCoord, i);
            Vector3 rayPosition = center;

            while (rayStrength > 0f && affectedBlocks < maxAffectedBlocks)
            {
                rayPosition += direction * ExplosionRayStep;
                int x = MathUtil.FloorToInt(rayPosition.X);
                int y = MathUtil.FloorToInt(rayPosition.Y);
                int z = MathUtil.FloorToInt(rayPosition.Z);

                if (y <= 0 || y >= Chunk.SizeY)
                {
                    rayStrength -= ExplosionRayDecay;
                    continue;
                }

                if (!world.HasChunkAt(x, z))
                {
                    rayStrength -= ExplosionRayDecay;
                    continue;
                }

                BlockId id = world.GetBlock(x, y, z);
                if (id == BlockId.Air)
                {
                    rayStrength -= ExplosionRayDecay;
                    continue;
                }

                if (id == BlockId.Bedrock)
                {
                    break;
                }

                if (id == BlockId.Tnt && primeTnt is not null)
                {
                    primeTnt(new BlockCoord(x, y, z));
                    rayStrength -= ExplosionRayDecay;
                    continue;
                }

                rayStrength -= (ExplosionResistance.Get(id) * resistanceScale * 0.3f) + ExplosionRayDecay;
                if (rayStrength <= 0f)
                {
                    continue;
                }

                var block = new BlockCoord(x, y, z);
                if (destroyed.Add(block) && editor.SetBlock(x, y, z, BlockId.Air))
                {
                    affectedBlocks++;
                }
            }
        }

        return affectedBlocks;
    }

    private static Vector3[] BuildExplosionDirections()
    {
        int estimatedCount = ExplosionSampleCount * ExplosionSampleCount * ExplosionSampleCount -
                             (ExplosionSampleCount - 2) * (ExplosionSampleCount - 2) * (ExplosionSampleCount - 2);
        var directions = new Vector3[estimatedCount];
        int count = 0;

        for (int x = 0; x < ExplosionSampleCount; x++)
        {
            float dx = x / (float)(ExplosionSampleCount - 1) * 2f - 1f;
            for (int y = 0; y < ExplosionSampleCount; y++)
            {
                float dy = y / (float)(ExplosionSampleCount - 1) * 2f - 1f;
                for (int z = 0; z < ExplosionSampleCount; z++)
                {
                    if (x > 0 && x < ExplosionSampleCount - 1 &&
                        y > 0 && y < ExplosionSampleCount - 1 &&
                        z > 0 && z < ExplosionSampleCount - 1)
                    {
                        continue;
                    }

                    float dz = z / (float)(ExplosionSampleCount - 1) * 2f - 1f;
                    var direction = new Vector3(dx, dy, dz);
                    if (direction.LengthSquared() <= float.Epsilon)
                    {
                        continue;
                    }

                    directions[count++] = Vector3.Normalize(direction);
                }
            }
        }

        if (count != directions.Length)
        {
            Array.Resize(ref directions, count);
        }

        return directions;
    }

    private static float GetRayStrength(BlockCoord source, int rayIndex)
    {
        unchecked
        {
            uint hash = (uint)source.X * 0x9E3779B1u;
            hash ^= (uint)source.Y * 0x85EBCA6Bu;
            hash ^= (uint)source.Z * 0xC2B2AE35u;
            hash ^= (uint)rayIndex * 0x27D4EB2Du;
            hash ^= hash >> 16;
            hash *= 0x7FEB352Du;
            hash ^= hash >> 15;
            hash *= 0x846CA68Bu;
            hash ^= hash >> 16;

            float normalized = (hash & 0x00FFFFFFu) / 16777215f;
            return ExplosionRayBaseStrength + normalized * ExplosionRayStrengthRange;
        }
    }
}
