using System;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.World;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Game;

public readonly struct CharacterCollider
{
    public float Width { get; }
    public float Height { get; }

    public CharacterCollider(float width, float height)
    {
        Width = width;
        Height = height;
    }
}

public static class CharacterPhysics
{
    public static bool IntersectsBlock(Vector3 position, CharacterCollider collider, BlockCoord block)
    {
        float half = collider.Width * 0.5f;
        float minX = position.X - half;
        float maxX = position.X + half;
        float minY = position.Y;
        float maxY = position.Y + collider.Height;
        float minZ = position.Z - half;
        float maxZ = position.Z + half;

        float bx0 = block.X;
        float by0 = block.Y;
        float bz0 = block.Z;
        float bx1 = block.X + 1f;
        float by1 = block.Y + 1f;
        float bz1 = block.Z + 1f;

        return maxX > bx0 && minX < bx1 &&
               maxY > by0 && minY < by1 &&
               maxZ > bz0 && minZ < bz1;
    }

    public static bool IsColliding(WorldType world, Vector3 position, CharacterCollider collider)
    {
        float half = collider.Width * 0.5f;
        float minX = position.X - half;
        float maxX = position.X + half;
        float minY = position.Y;
        float maxY = position.Y + collider.Height;
        float minZ = position.Z - half;
        float maxZ = position.Z + half;

        int minBx = MathUtil.FloorToInt(minX);
        int maxBx = MathUtil.FloorToInt(maxX);
        int minBy = MathUtil.FloorToInt(minY);
        int maxBy = MathUtil.FloorToInt(maxY);
        int minBz = MathUtil.FloorToInt(minZ);
        int maxBz = MathUtil.FloorToInt(maxZ);

        if (minBy < 0)
        {
            return true;
        }

        for (int x = minBx; x <= maxBx; x++)
        {
            for (int y = minBy; y <= maxBy; y++)
            {
                for (int z = minBz; z <= maxBz; z++)
                {
                    if (!world.HasChunkAt(x, z))
                    {
                        return true;
                    }

                    var block = world.GetBlock(x, y, z);
                    if (block == BlockId.Air)
                    {
                        continue;
                    }

                    var def = BlockRegistry.Get(block);
                    if (def.IsSolid)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public static void Move(WorldType world, ref Vector3 position, ref Vector3 velocity, CharacterCollider collider, float dt, out bool onGround)
    {
        MoveInternal(world, ref position, ref velocity, collider, dt, 0f, wasOnGround: false, out onGround);
    }

    public static void MoveWithStep(WorldType world, ref Vector3 position, ref Vector3 velocity, CharacterCollider collider, float dt, float stepHeight, bool wasOnGround, out bool onGround)
    {
        MoveInternal(world, ref position, ref velocity, collider, dt, stepHeight, wasOnGround, out onGround);
    }

    public static bool TryResolveInsideSolid(WorldType world, ref Vector3 position, ref Vector3 velocity, CharacterCollider collider, int maxSteps = 8, float stepSize = 1f)
    {
        if (!IsColliding(world, position, collider))
        {
            return true;
        }

        var attempt = position;
        for (int i = 0; i < maxSteps; i++)
        {
            attempt.Y += stepSize;
            if (!IsColliding(world, attempt, collider))
            {
                position = attempt;
                velocity = Vector3.Zero;
                return true;
            }
        }

        return false;
    }

    private static void MoveInternal(WorldType world, ref Vector3 position, ref Vector3 velocity, CharacterCollider collider, float dt, float stepHeight, bool wasOnGround, out bool onGround)
    {
        Vector3 start = position;
        Vector3 delta = velocity * dt;
        Vector3 moved = position;
        bool collidedHorizontally = false;

        moved.X += delta.X;
        if (IsColliding(world, moved, collider))
        {
            moved.X = start.X;
            velocity.X = 0f;
            collidedHorizontally = true;
        }

        moved.Z += delta.Z;
        if (IsColliding(world, moved, collider))
        {
            moved.Z = start.Z;
            velocity.Z = 0f;
            collidedHorizontally = true;
        }

        if (stepHeight > 0f && wasOnGround && collidedHorizontally)
        {
            var step = moved;
            step.Y += stepHeight;
            if (!IsColliding(world, step, collider))
            {
                var stepX = step;
                stepX.X += delta.X;
                if (IsColliding(world, stepX, collider))
                {
                    stepX.X = step.X;
                    velocity.X = 0f;
                }

                var stepXZ = stepX;
                stepXZ.Z += delta.Z;
                if (IsColliding(world, stepXZ, collider))
                {
                    stepXZ.Z = stepX.Z;
                    velocity.Z = 0f;
                }

                if (!IsColliding(world, stepXZ, collider))
                {
                    moved = stepXZ;
                    collidedHorizontally = false;
                }
            }
        }

        onGround = wasOnGround;
        var afterHorizontal = moved;
        afterHorizontal.Y += delta.Y;
        if (IsColliding(world, afterHorizontal, collider))
        {
            if (delta.Y < 0f)
            {
                onGround = true;
            }

            afterHorizontal.Y = moved.Y;
            velocity.Y = 0f;
        }
        else
        {
            onGround = false;
        }

        if (collidedHorizontally && delta.Y == 0f)
        {
            onGround = wasOnGround;
        }

        position = afterHorizontal;
    }
}
