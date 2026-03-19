using System;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.Renderer;
using MiniCRUFT.World;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Game;

public sealed class PlayerFeedbackSystem
{
    private readonly SoundSystem _soundSystem;
    private readonly IParticleEmitter _particleEmitter;
    private readonly AudioConfig _audioConfig;

    private bool _initialized;
    private bool _wasOnGround;
    private bool _wasInWater;
    private bool _wasInLava;
    private Vector3 _lastPosition;
    private float _stepDistance;

    public PlayerFeedbackSystem(SoundSystem soundSystem, IParticleEmitter particleEmitter, AudioConfig audioConfig)
    {
        _soundSystem = soundSystem;
        _particleEmitter = particleEmitter;
        _audioConfig = audioConfig;
    }

    public void Reset(Player player)
    {
        _initialized = true;
        _wasOnGround = player.OnGround;
        _wasInWater = player.InWater;
        _wasInLava = player.InLava;
        _lastPosition = player.Position;
        _stepDistance = 0f;
    }

    public void Update(float dt, Player player, InputState input, WorldType world, bool sprintEnabled = true)
    {
        if (dt <= 0f)
        {
            return;
        }

        if (!_initialized)
        {
            Reset(player);
            return;
        }

        Vector3 currentPosition = player.Position;
        Vector3 motion = new(player.Velocity.X, 0f, player.Velocity.Z);
        bool movementIntent = input.Forward || input.Backward || input.Left || input.Right;
        bool sprinting = movementIntent && input.Sprint && sprintEnabled;
        BlockId groundBlock = SampleGroundBlock(world, player);
        bool solidGround = IsSolidGround(groundBlock);
        Vector3 feetPosition = new(currentPosition.X, currentPosition.Y + 0.05f, currentPosition.Z);
        bool inWater = player.InWater;
        bool inLava = player.InLava;
        bool inLiquid = inWater || inLava;

        if (inLiquid)
        {
            LiquidKind liquidKind = inWater ? LiquidKind.Water : LiquidKind.Lava;
            float coverage = Math.Clamp(inWater ? player.WaterCoverage : player.LavaCoverage, 0.25f, 1f);

            if ((!_wasInWater && inWater) || (!_wasInLava && inLava))
            {
                _soundSystem.PlayLiquidSplash(liquidKind, feetPosition, coverage);
                _stepDistance = 0f;
            }

            if (movementIntent)
            {
                _stepDistance += HorizontalDistance(currentPosition, _lastPosition);
                float threshold = Math.Max(0.1f, _audioConfig.SwimStepDistance);
                while (_stepDistance >= threshold)
                {
                    _stepDistance -= threshold;
                    _soundSystem.PlayLiquidSwim(liquidKind, feetPosition, coverage);
                }
            }
            else
            {
                _stepDistance = 0f;
            }
        }
        else
        {
            if (_wasOnGround && !player.OnGround && player.Velocity.Y > 0f && solidGround)
            {
                _soundSystem.PlayJump(groundBlock, feetPosition);
                _particleEmitter.EmitJumpParticles(groundBlock, feetPosition, motion);
                _stepDistance = 0f;
            }

            if (player.OnGround && movementIntent && solidGround)
            {
                _stepDistance += HorizontalDistance(currentPosition, _lastPosition);
                float threshold = sprinting ? _audioConfig.RunStepDistance : _audioConfig.StepDistance;
                while (_stepDistance >= threshold)
                {
                    _stepDistance -= threshold;
                    if (sprinting)
                    {
                        _soundSystem.PlayRun(groundBlock, feetPosition);
                    }
                    else
                    {
                        _soundSystem.PlayStep(groundBlock, feetPosition);
                    }

                    _particleEmitter.EmitStepParticles(groundBlock, feetPosition, motion, sprinting);
                }
            }
            else
            {
                _stepDistance = 0f;
            }
        }

        _wasOnGround = player.OnGround;
        _wasInWater = inWater;
        _wasInLava = inLava;
        _lastPosition = currentPosition;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    private static BlockId SampleGroundBlock(WorldType world, Player player)
    {
        int x = MathUtil.FloorToInt(player.Position.X);
        int y = MathUtil.FloorToInt(player.Position.Y - 0.01f);
        int z = MathUtil.FloorToInt(player.Position.Z);

        if (!world.HasChunkAt(x, z))
        {
            return BlockId.Air;
        }

        var block = world.GetBlock(x, y, z);
        if (block == BlockId.Air && y > 0)
        {
            block = world.GetBlock(x, y - 1, z);
        }

        return block;
    }

    private static bool IsSolidGround(BlockId block)
    {
        if (block == BlockId.Air || LiquidBlocks.IsLiquid(block))
        {
            return false;
        }

        return BlockRegistry.Get(block).IsSolid;
    }
}
