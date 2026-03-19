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
    public int Health { get; private set; }
    public int MaxHealth { get; }
    public float HurtCooldown { get; private set; }
    public bool InWater { get; private set; }
    public bool InLava { get; private set; }
    public float WaterCoverage { get; private set; }
    public float LavaCoverage { get; private set; }
    public Vector3 EyePosition => Position + new Vector3(0f, _physics.EyeHeight, 0f);

    private Vector3 _lastSafePosition;
    private bool _hasSafePosition;

    public Player(Vector3 start, PhysicsConfig physics)
    {
        Position = start;
        _physics = physics;
        MaxHealth = Math.Max(1, physics.PlayerMaxHealth);
        Health = MaxHealth;
    }

    public void Update(float dt, InputState input, WorldType world, float speed, float mouseSensitivity, float sprintMultiplier = 1f)
    {
        if (HurtCooldown > 0f)
        {
            HurtCooldown = Math.Max(0f, HurtCooldown - dt);
        }

        if (!world.HasChunkAt(MathUtil.FloorToInt(Position.X), MathUtil.FloorToInt(Position.Z)))
        {
            if (_hasSafePosition)
            {
                Position = _lastSafePosition;
                Velocity = Vector3.Zero;
            }
            InWater = false;
            InLava = false;
            WaterCoverage = 0f;
            LavaCoverage = 0f;
            return;
        }

        CharacterLiquidState liquidState = CharacterLiquidSampler.Sample(world, Position, _physics.PlayerWidth, _physics.PlayerHeight);
        float liquidCoverage = liquidState.Coverage;
        bool inLiquid = liquidState.InLiquid;

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

        float moveSpeed = speed * (input.Sprint ? sprintMultiplier : 1f);
        if (inLiquid)
        {
            float swimMultiplier = 1f - (1f - _physics.WaterMoveMultiplier) * liquidCoverage;
            moveSpeed *= swimMultiplier;
        }
        Velocity.X = move.X * moveSpeed;
        Velocity.Z = move.Z * moveSpeed;

        if (inLiquid)
        {
            float gravityMultiplier = 1f - (1f - _physics.WaterGravityMultiplier) * liquidCoverage;
            Velocity.Y += _physics.Gravity * gravityMultiplier * dt;
            Velocity.Y += _physics.WaterBuoyancy * liquidCoverage * dt;
            if (input.Jump)
            {
                Velocity.Y = Math.Max(Velocity.Y, _physics.WaterJumpVelocity);
                OnGround = false;
            }

            Velocity.Y = Math.Max(Velocity.Y, -_physics.WaterMaxFallSpeed);
        }
        else
        {
            if (OnGround && input.Jump)
            {
                Velocity.Y = _physics.JumpVelocity;
                OnGround = false;
            }

            Velocity.Y += _physics.Gravity * dt;
        }

        if (liquidState.InWater)
        {
            Vector3 waterCurrent = LiquidCurrentSampler.SampleWaterCurrent(world, Position, _physics.PlayerWidth, _physics.PlayerHeight);
            if (waterCurrent.LengthSquared() > float.Epsilon)
            {
                Velocity += waterCurrent * _physics.WaterCurrentMultiplier * liquidState.WaterCoverage;
            }
        }

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

        if (world.HasChunkAt(MathUtil.FloorToInt(Position.X), MathUtil.FloorToInt(Position.Z)))
        {
            liquidState = CharacterLiquidSampler.Sample(world, Position, _physics.PlayerWidth, _physics.PlayerHeight);
            InWater = liquidState.InWater;
            InLava = liquidState.InLava;
            WaterCoverage = liquidState.WaterCoverage;
            LavaCoverage = liquidState.LavaCoverage;
        }
        else
        {
            InWater = false;
            InLava = false;
            WaterCoverage = 0f;
            LavaCoverage = 0f;
        }
    }

    public bool TryApplyDamage(int damage, Vector3 knockback)
    {
        return TryApplyDamage(damage, knockback, _physics.HurtCooldownSeconds);
    }

    public bool TryApplyDamage(int damage, Vector3 knockback, float cooldownSeconds)
    {
        if (damage <= 0 || Health <= 0 || HurtCooldown > 0f)
        {
            return false;
        }

        Health = Math.Max(0, Health - damage);
        HurtCooldown = Math.Max(0f, cooldownSeconds);
        Velocity += knockback;
        if (Health == 0)
        {
            Velocity = Vector3.Zero;
            OnGround = false;
        }

        return true;
    }

    public void Respawn(Vector3 position)
    {
        Position = position;
        Velocity = Vector3.Zero;
        OnGround = false;
        Health = MaxHealth;
        HurtCooldown = 0f;
        _lastSafePosition = position;
        _hasSafePosition = true;
        InWater = false;
        InLava = false;
        WaterCoverage = 0f;
        LavaCoverage = 0f;
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
