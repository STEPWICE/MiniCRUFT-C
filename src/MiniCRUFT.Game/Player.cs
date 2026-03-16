using System;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.World;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Game;

public sealed class Player
{
    private readonly PhysicsConfig _physics;

    public Vector3 Position;
    public Vector3 Velocity;
    public float Yaw;
    public float Pitch;
    public bool OnGround;

    private Vector3 _lastSafePosition;
    private bool _hasSafePosition;

    public Player(Vector3 start, PhysicsConfig physics)
    {
        Position = start;
        _physics = physics;
    }

    public void Update(float dt, InputState input, WorldType world, float speed, float mouseSensitivity)
    {
        if (!world.HasChunkAt(MathUtil.FloorToInt(Position.X), MathUtil.FloorToInt(Position.Z)))
        {
            if (_hasSafePosition)
            {
                Position = _lastSafePosition;
                Velocity = Vector3.Zero;
            }
            return;
        }

        Yaw += input.MouseDeltaX * mouseSensitivity;
        Pitch -= input.MouseDeltaY * mouseSensitivity;
        Pitch = MathUtil.Clamp(Pitch, -89f, 89f);

        Vector3 forward = new Vector3(MathF.Cos(MathF.PI * Yaw / 180f), 0, MathF.Sin(MathF.PI * Yaw / 180f));
        Vector3 right = new Vector3(-forward.Z, 0, forward.X);

        Vector3 move = Vector3.Zero;
        if (input.Forward) move += forward;
        if (input.Backward) move -= forward;
        if (input.Right) move += right;
        if (input.Left) move -= right;
        if (move != Vector3.Zero)
        {
            move = Vector3.Normalize(move);
        }

        float moveSpeed = speed * (input.Sprint ? _physics.SprintMultiplier : 1f);
        Velocity.X = move.X * moveSpeed;
        Velocity.Z = move.Z * moveSpeed;

        if (OnGround && input.Jump)
        {
            Velocity.Y = _physics.JumpVelocity;
            OnGround = false;
        }

        Velocity.Y += _physics.Gravity * dt;

        MoveAndCollide(world, Velocity * dt);

        ResolveIfInsideSolid(world);
        if (Position.Y < -2f)
        {
            if (_hasSafePosition)
            {
                Position = _lastSafePosition;
                Velocity = Vector3.Zero;
            }
            else
            {
                Position = new Vector3(Position.X, world.Settings.SeaLevel + 4f, Position.Z);
                Velocity = Vector3.Zero;
            }
        }
        if (OnGround && world.HasChunkAt(MathUtil.FloorToInt(Position.X), MathUtil.FloorToInt(Position.Z)))
        {
            _lastSafePosition = Position;
            _hasSafePosition = true;
        }
    }

    private void MoveAndCollide(WorldType world, Vector3 delta)
    {
        Vector3 pos = Position;

        pos.X += delta.X;
        if (IsColliding(world, pos))
        {
            pos.X -= delta.X;
            Velocity.X = 0;
        }

        pos.Y += delta.Y;
        if (IsColliding(world, pos))
        {
            if (delta.Y < 0)
            {
                OnGround = true;
            }
            pos.Y -= delta.Y;
            Velocity.Y = 0;
        }
        else
        {
            OnGround = false;
        }

        pos.Z += delta.Z;
        if (IsColliding(world, pos))
        {
            pos.Z -= delta.Z;
            Velocity.Z = 0;
        }

        Position = pos;
    }

    private bool IsColliding(WorldType world, Vector3 pos)
    {
        float half = _physics.PlayerWidth / 2f;
        float minX = pos.X - half;
        float maxX = pos.X + half;
        float minY = pos.Y;
        float maxY = pos.Y + _physics.PlayerHeight;
        float minZ = pos.Z - half;
        float maxZ = pos.Z + half;

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

    public bool IntersectsBlock(BlockCoord block)
    {
        float half = _physics.PlayerWidth / 2f;
        float minX = Position.X - half;
        float maxX = Position.X + half;
        float minY = Position.Y;
        float maxY = Position.Y + _physics.PlayerHeight;
        float minZ = Position.Z - half;
        float maxZ = Position.Z + half;

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

    private void ResolveIfInsideSolid(WorldType world)
    {
        if (!IsColliding(world, Position))
        {
            return;
        }

        var attempt = Position;
        for (int i = 0; i < 8; i++)
        {
            attempt.Y += 1f;
            if (!IsColliding(world, attempt))
            {
                Position = attempt;
                Velocity = Vector3.Zero;
                return;
            }
        }

        if (_hasSafePosition)
        {
            Position = _lastSafePosition;
            Velocity = Vector3.Zero;
        }
    }
}
