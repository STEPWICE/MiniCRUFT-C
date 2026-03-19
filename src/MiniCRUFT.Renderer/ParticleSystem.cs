using System;
using System.Collections.Generic;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.World;
using Veldrid;

namespace MiniCRUFT.Renderer;

public sealed class ParticleSystem : IDisposable
{
    private const float DefaultMaxScreenSize = 40f;
    private const float SmokeMaxScreenSize = 80f;
    private const float FlameMaxScreenSize = 48f;
    private const float SmokeGravityScale = 0.18f;
    private const float FlameGravityScale = 0.06f;

    private static readonly Vector4 SmokeTintBase = new(0.72f, 0.72f, 0.72f, 0.82f);
    private static readonly Vector4 FlameTintBase = new(1f, 0.72f, 0.18f, 1f);

    private readonly TextureAtlas _atlas;
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteRegion _smokeRegion;
    private readonly SpriteRegion _flame0Region;
    private readonly SpriteRegion _flame1Region;
    private readonly ParticleConfig _config;
    private readonly Random _random = new();
    private readonly List<Particle> _particles = new();
    private readonly Dictionary<BlockId, SpriteRegion> _regionCache = new();

    public ParticleSystem(GraphicsDevice device, TextureAtlas atlas, ParticleConfig config)
    {
        _atlas = atlas;
        _config = config;
        _spriteBatch = new SpriteBatch(device, atlas.Texture, atlas.Sampler);
        _smokeRegion = ToSpriteRegion(atlas.GetRegion("beta_smoke"));
        _flame0Region = ToSpriteRegion(atlas.GetRegion("fire_0"));
        _flame1Region = ToSpriteRegion(atlas.GetRegion("fire_1"));
    }

    public void Update(float dt)
    {
        if (!_config.Enabled || dt <= 0f || _particles.Count == 0)
        {
            return;
        }

        float drag = MathF.Pow(_config.Drag, dt * 60f);
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var particle = _particles[i];
            particle.Age += dt;
            if (particle.Age >= particle.Lifetime)
            {
                _particles.RemoveAt(i);
                continue;
            }

            particle.Velocity.Y += _config.Gravity * particle.GravityScale * dt;
            particle.Position += particle.Velocity * dt;
            particle.Velocity *= drag;
            _particles[i] = particle;
        }
    }

    public void Draw(CommandList commandList, Camera camera, int width, int height)
    {
        if (!_config.Enabled || _particles.Count == 0)
        {
            return;
        }

        _spriteBatch.Begin(width, height);
        var viewProj = camera.View * camera.Projection;
        float focalLength = height / (2f * MathF.Tan(MathF.PI * camera.Fov / 360f));

        for (int i = 0; i < _particles.Count; i++)
        {
            var particle = _particles[i];
            var clip = Vector4.Transform(new Vector4(particle.Position, 1f), viewProj);
            if (clip.W <= 0.001f)
            {
                continue;
            }

            float invW = 1f / clip.W;
            float ndcX = clip.X * invW;
            float ndcY = clip.Y * invW;
            if (ndcX < -1.2f || ndcX > 1.2f || ndcY < -1.2f || ndcY > 1.2f)
            {
                continue;
            }

            float depth = MathF.Max(clip.W, 0.1f);
            float sizePx = Math.Clamp(particle.Size * focalLength / depth, 1f, particle.MaxScreenSize);
            float alpha = 1f - Math.Clamp(particle.Age / particle.Lifetime, 0f, 1f);
            alpha *= alpha;

            var region = GetRegion(particle);
            var color = new Vector4(
                particle.Tint.X * particle.Brightness,
                particle.Tint.Y * particle.Brightness,
                particle.Tint.Z * particle.Brightness,
                alpha * particle.Tint.W);
            var position = new Vector2(
                (ndcX * 0.5f + 0.5f) * width - sizePx * 0.5f,
                (1f - (ndcY * 0.5f + 0.5f)) * height - sizePx * 0.5f);
            var size = new Vector2(sizePx, sizePx);
            _spriteBatch.Draw(region, position, size, color);
        }

        _spriteBatch.Flush(commandList);
    }

    public void EmitExplosionParticles(Vector3 position, int affectedBlocks, float intensity = 1f)
    {
        if (!_config.Enabled)
        {
            return;
        }

        float scale = Math.Clamp(intensity, 0.5f, 2f);
        float blockScale = Math.Clamp(affectedBlocks / 64f, 0f, 1.5f);

        int smokeCount = Math.Clamp((int)MathF.Ceiling((18f + affectedBlocks * 0.5f) * scale), 12, 96);
        int flameCount = Math.Clamp((int)MathF.Ceiling((6f + affectedBlocks * 0.18f) * scale), 4, 32);
        EnsureCapacity(smokeCount + flameCount);

        EmitExplosionSmoke(position, smokeCount, scale, blockScale);
        EmitExplosionFlames(position, flameCount, scale, blockScale);
    }

    public void EmitFireParticles(FireEventKind kind, Vector3 position, float intensity = 1f)
    {
        if (!_config.Enabled)
        {
            return;
        }

        float scale = Math.Clamp(intensity, 0.35f, 2f);
        switch (kind)
        {
            case FireEventKind.Ignited:
                EmitFireSmokeBurst(position, ScaleCount(3, scale), 0.18f, 0.82f, 0.58f, 0.14f, 0.95f, 0.78f, 0.18f, 0.46f);
                EmitFireSparkBurst(position, ScaleCount(2, scale), 0.1f, 1.2f, 0.2f, 0.08f, 1.15f, 0.98f, 0.9f, 0.9f);
                break;
            case FireEventKind.Crackle:
                EmitFireSmokeBurst(position, ScaleCount(2, scale), 0.14f, 0.64f, 0.42f, 0.11f, 0.9f, 0.58f, 0.14f, 0.38f);
                EmitFireSparkBurst(position, 1, 0.08f, 1.05f, 0.17f, 0.06f, 1.15f, 0.92f, 0.7f, 0.75f);
                break;
            case FireEventKind.Consumed:
                EmitFireSmokeBurst(position, ScaleCount(4, scale), 0.2f, 0.7f, 0.72f, 0.15f, 0.95f, 0.66f, 0.2f, 0.42f);
                EmitFireSparkBurst(position, ScaleCount(1, scale), 0.09f, 1.1f, 0.19f, 0.07f, 1.18f, 0.96f, 0.78f, 0.8f);
                break;
            case FireEventKind.Extinguished:
                EmitFireSmokeBurst(position, ScaleCount(4, scale), 0.22f, 0.52f, 0.86f, 0.16f, 0.92f, 0.5f, 0.12f, 0.34f);
                break;
        }
    }

    public void EmitBlockBreak(BlockId block, Vector3 position, Vector3 motion)
    {
        EmitBurst(block, position, motion, _config.BlockBreakCount, _config.BlockBreakLifetime, _config.BlockBreakSize, _config.BlockBreakSpeed, _config.StepUpwardBias, 0.35f, 0.12f);
    }

    public void EmitBlockPlace(BlockId block, Vector3 position, Vector3 motion)
    {
        EmitBurst(block, position, motion, _config.BlockPlaceCount, _config.BlockPlaceLifetime, _config.BlockPlaceSize, _config.BlockPlaceSpeed, _config.StepUpwardBias * 0.5f, 0.18f, 0.08f);
    }

    public void EmitStep(BlockId block, Vector3 position, Vector3 motion, bool sprinting)
    {
        int count = sprinting ? _config.StepCount + 1 : _config.StepCount;
        float speed = sprinting ? _config.StepSpeed * 1.25f : _config.StepSpeed;
        float lifetime = sprinting ? _config.StepLifetime * 0.9f : _config.StepLifetime;
        float size = sprinting ? _config.StepSize * 1.05f : _config.StepSize;
        float upwardBias = sprinting ? _config.StepUpwardBias : _config.StepUpwardBias * 0.75f;
        EmitBurst(block, position, motion, count, lifetime, size, speed, upwardBias, 0.12f, 0.15f);
    }

    public void EmitJump(BlockId block, Vector3 position, Vector3 motion)
    {
        EmitBurst(block, position, motion, _config.JumpCount, _config.JumpLifetime, _config.JumpSize, _config.JumpSpeed, _config.JumpUpwardBias, 0.16f, 0.1f);
    }

    public void EmitMobAttack(MobType type, Vector3 position, Vector3 motion, bool elite = false, EliteMobVariant eliteVariant = EliteMobVariant.None, float intensity = 1f)
    {
        BlockId block = GetMobAttackBlock(type);
        float scale = Math.Max(0f, intensity) * GetEliteParticleMultiplier(elite, eliteVariant);
        int count = ScaleCount(_config.MobAttackCount, scale);
        EmitBurst(
            block,
            position,
            motion,
            count,
            _config.MobAttackLifetime,
            _config.MobAttackSize,
            _config.MobAttackSpeed,
            _config.MobAttackUpwardBias,
            _config.MobAttackSpread,
            _config.MobAttackMotionInfluence,
            0.94f);

        if (!elite)
        {
            return;
        }

        BlockId flash = GetEliteAttackFlashBlock(eliteVariant);
        if (flash == block)
        {
            return;
        }

        int flashCount = Math.Max(1, count / 3);
        EmitBurst(
            flash,
            position,
            motion * 0.55f,
            flashCount,
            _config.MobAttackLifetime * 0.82f,
            _config.MobAttackSize * 1.08f,
            _config.MobAttackSpeed * 1.1f,
            _config.MobAttackUpwardBias,
            _config.MobAttackSpread * 0.8f,
            _config.MobAttackMotionInfluence,
            1.08f);
    }

    public void EmitMobHurt(MobType type, Vector3 position, Vector3 motion, bool elite = false, EliteMobVariant eliteVariant = EliteMobVariant.None, float intensity = 1f)
    {
        BlockId block = GetMobHurtBlock(type);
        float scale = Math.Max(0f, intensity) * GetEliteParticleMultiplier(elite, eliteVariant);
        int count = ScaleCount(_config.MobHurtCount, scale);
        EmitBurst(
            block,
            position,
            motion,
            count,
            _config.MobHurtLifetime,
            _config.MobHurtSize,
            _config.MobHurtSpeed,
            _config.MobHurtUpwardBias,
            _config.MobHurtSpread,
            _config.MobHurtMotionInfluence,
            0.86f);
    }

    public void EmitMobDeath(MobType type, Vector3 position, Vector3 motion, bool elite = false, EliteMobVariant eliteVariant = EliteMobVariant.None, float intensity = 1f)
    {
        BlockId block = GetMobDeathBlock(type);
        float scale = Math.Max(0f, intensity) * GetEliteParticleMultiplier(elite, eliteVariant);
        int count = ScaleCount(_config.MobDeathCount, scale);
        EmitBurst(
            block,
            position,
            motion,
            count,
            _config.MobDeathLifetime,
            _config.MobDeathSize,
            _config.MobDeathSpeed,
            _config.MobDeathUpwardBias,
            _config.MobDeathSpread,
            _config.MobDeathMotionInfluence,
            0.96f);

        if (!elite)
        {
            return;
        }

        int trophyCount = Math.Max(1, count / 3);
        EmitBurst(
            BlockId.MobTrophy,
            position,
            motion * 0.65f,
            trophyCount,
            _config.MobDeathLifetime * 0.85f,
            _config.MobDeathSize * 1.05f,
            _config.MobDeathSpeed * 1.05f,
            _config.MobDeathUpwardBias,
            _config.MobDeathSpread * 0.85f,
            _config.MobDeathMotionInfluence,
            1.08f);
    }

    private void EmitBurst(
        BlockId block,
        Vector3 position,
        Vector3 motion,
        int count,
        float lifetime,
        float size,
        float speed,
        float upwardBias,
        float spread,
        float motionInfluence,
        float baseBrightness = 0.8f,
        ParticleVisual visual = ParticleVisual.Block,
        Vector4 tint = default,
        float gravityScale = 1f,
        float maxScreenSize = DefaultMaxScreenSize)
    {
        if (!_config.Enabled || count <= 0 || (visual == ParticleVisual.Block && block == BlockId.Air))
        {
            return;
        }

        EnsureCapacity(count);
        Vector4 particleTint = tint == default ? Vector4.One : tint;

        for (int i = 0; i < count; i++)
        {
            Vector3 spawn = position + RandomOffset(spread);
            Vector3 velocity = motion * motionInfluence + RandomBurstVelocity(speed, upwardBias);
            float particleLifetime = lifetime * (0.8f + NextFloat() * 0.4f);
            float particleSize = size * (0.85f + NextFloat() * 0.3f);
            float brightness = baseBrightness + NextFloat() * 0.25f;
            _particles.Add(new Particle(visual, block, spawn, velocity, particleLifetime, particleSize, brightness, particleTint, gravityScale, maxScreenSize));
        }
    }

    private void EmitExplosionSmoke(Vector3 position, int count, float intensity, float blockScale)
    {
        float spread = 0.48f + blockScale * 0.36f;
        float speed = 1.05f + intensity * 0.4f;
        float lifetime = 1.15f + blockScale * 0.55f;
        float size = 0.32f + blockScale * 0.18f;
        float upwardBias = 0.95f + intensity * 0.16f;

        for (int i = 0; i < count; i++)
        {
            Vector3 spawn = position + RandomOffset(spread);
            spawn.Y += 0.08f + NextFloat() * 0.32f;

            Vector3 velocity = RandomBurstVelocity(speed, upwardBias);
            velocity.X *= 0.62f;
            velocity.Z *= 0.62f;

            float particleLifetime = lifetime * (0.72f + NextFloat() * 0.5f);
            float particleSize = size * (0.95f + NextFloat() * 0.55f);
            float brightness = 0.5f + NextFloat() * 0.18f;
            float smokeShade = 0.7f + NextFloat() * 0.12f;
            var tint = new Vector4(
                SmokeTintBase.X * smokeShade,
                SmokeTintBase.Y * smokeShade,
                SmokeTintBase.Z * smokeShade,
                SmokeTintBase.W);
            var visual = ParticleVisual.Smoke;

            _particles.Add(new Particle(visual, BlockId.Air, spawn, velocity, particleLifetime, particleSize, brightness, tint, SmokeGravityScale, SmokeMaxScreenSize));
        }
    }

    private void EmitExplosionFlames(Vector3 position, int count, float intensity, float blockScale)
    {
        float spread = 0.22f + blockScale * 0.2f;
        float speed = 1.45f + intensity * 0.45f;
        float lifetime = 0.42f + blockScale * 0.12f;
        float size = 0.15f + blockScale * 0.04f;
        float upwardBias = 1.15f + intensity * 0.18f;

        for (int i = 0; i < count; i++)
        {
            Vector3 spawn = position + RandomOffset(spread);
            spawn.Y += 0.03f + NextFloat() * 0.22f;

            Vector3 velocity = RandomBurstVelocity(speed, upwardBias);
            velocity.X *= 0.45f;
            velocity.Z *= 0.45f;

            float particleLifetime = lifetime * (0.82f + NextFloat() * 0.36f);
            float particleSize = size * (0.9f + NextFloat() * 0.35f);
            float brightness = 0.92f + NextFloat() * 0.18f;
            float flameShade = 0.74f + NextFloat() * 0.16f;
            float alpha = 0.9f + NextFloat() * 0.1f;
            var tint = new Vector4(
                FlameTintBase.X,
                FlameTintBase.Y * flameShade,
                FlameTintBase.Z + NextFloat() * 0.08f,
                FlameTintBase.W * alpha);
            var visual = (i & 1) == 0 ? ParticleVisual.Flame0 : ParticleVisual.Flame1;

            _particles.Add(new Particle(visual, BlockId.Air, spawn, velocity, particleLifetime, particleSize, brightness, tint, FlameGravityScale, FlameMaxScreenSize));
        }
    }

    private void EmitFireSmokeBurst(Vector3 position, int count, float spread, float speed, float lifetime, float size, float upwardBias, float smokeShade, float alpha, float brightness)
    {
        if (count <= 0)
        {
            return;
        }

        EnsureCapacity(count);
        float shade = Math.Clamp(smokeShade, 0f, 1f);
        var tint = new Vector4(
            SmokeTintBase.X * shade,
            SmokeTintBase.Y * shade,
            SmokeTintBase.Z * shade,
            SmokeTintBase.W * Math.Clamp(alpha, 0f, 1f));

        for (int i = 0; i < count; i++)
        {
            Vector3 spawn = position + RandomOffset(spread);
            spawn.Y += 0.04f + NextFloat() * 0.18f;

            Vector3 velocity = RandomBurstVelocity(speed, upwardBias);
            velocity.X *= 0.58f;
            velocity.Z *= 0.58f;

            float particleLifetime = lifetime * (0.82f + NextFloat() * 0.32f);
            float particleSize = size * (0.84f + NextFloat() * 0.34f);
            float particleBrightness = brightness + NextFloat() * 0.14f;

            _particles.Add(new Particle(ParticleVisual.Smoke, BlockId.Air, spawn, velocity, particleLifetime, particleSize, particleBrightness, tint, SmokeGravityScale, SmokeMaxScreenSize));
        }
    }

    private void EmitFireSparkBurst(Vector3 position, int count, float spread, float speed, float lifetime, float size, float upwardBias, float brightness, float flameShade, float alpha)
    {
        if (count <= 0)
        {
            return;
        }

        EnsureCapacity(count);
        float shade = Math.Clamp(flameShade, 0f, 1f);
        var tint = new Vector4(
            FlameTintBase.X,
            FlameTintBase.Y * shade,
            FlameTintBase.Z,
            FlameTintBase.W * Math.Clamp(alpha, 0f, 1f));

        for (int i = 0; i < count; i++)
        {
            Vector3 spawn = position + RandomOffset(spread);
            spawn.Y += 0.02f + NextFloat() * 0.16f;

            Vector3 velocity = RandomBurstVelocity(speed, upwardBias);
            velocity.X *= 0.45f;
            velocity.Z *= 0.45f;

            float particleLifetime = lifetime * (0.8f + NextFloat() * 0.3f);
            float particleSize = size * (0.85f + NextFloat() * 0.3f);
            float particleBrightness = brightness + NextFloat() * 0.16f;
            var visual = (i & 1) == 0 ? ParticleVisual.Flame0 : ParticleVisual.Flame1;

            _particles.Add(new Particle(visual, BlockId.Air, spawn, velocity, particleLifetime, particleSize, particleBrightness, tint, FlameGravityScale, FlameMaxScreenSize));
        }
    }

    private int ScaleCount(int count, float scale)
    {
        if (count <= 0)
        {
            return 0;
        }

        float clampedScale = Math.Max(0f, scale);
        return Math.Max(1, (int)MathF.Ceiling(count * clampedScale));
    }

    private void EnsureCapacity(int additionalCount)
    {
        int overflow = _particles.Count + additionalCount - _config.MaxParticles;
        if (overflow > 0)
        {
            int remove = Math.Min(overflow, _particles.Count);
            if (remove > 0)
            {
                _particles.RemoveRange(0, remove);
            }
        }
    }

    private Vector3 RandomOffset(float spread)
    {
        return new Vector3(
            (NextFloat() - 0.5f) * spread,
            NextFloat() * spread * 0.6f,
            (NextFloat() - 0.5f) * spread);
    }

    private Vector3 RandomBurstVelocity(float speed, float upwardBias)
    {
        var direction = new Vector3(NextFloat() * 2f - 1f, NextFloat() * 2f - 1f, NextFloat() * 2f - 1f);
        direction.Y = MathF.Abs(direction.Y) + upwardBias;
        if (direction.LengthSquared() < 0.0001f)
        {
            direction = Vector3.UnitY;
        }
        else
        {
            direction = Vector3.Normalize(direction);
        }

        return direction * (speed * (0.65f + NextFloat() * 0.7f));
    }

    private SpriteRegion GetRegion(Particle particle)
    {
        return particle.Visual switch
        {
            ParticleVisual.Smoke => _smokeRegion,
            ParticleVisual.Flame0 => _flame0Region,
            ParticleVisual.Flame1 => _flame1Region,
            _ => GetBlockRegion(particle.Block)
        };
    }

    private SpriteRegion GetBlockRegion(BlockId block)
    {
        if (_regionCache.TryGetValue(block, out var region))
        {
            return region;
        }

        var definition = BlockRegistry.Get(block);
        var atlasRegion = _atlas.GetRegion(definition.TextureTop);
        region = new SpriteRegion(atlasRegion.Min, atlasRegion.Max, new Vector2(16f, 16f));
        _regionCache[block] = region;
        return region;
    }

    private static SpriteRegion ToSpriteRegion(AtlasRegion region)
    {
        return new SpriteRegion(region.Min, region.Max, new Vector2(16f, 16f));
    }

    private static BlockId GetMobAttackBlock(MobType type)
    {
        return type switch
        {
            MobType.Zombie => BlockId.RottenFlesh,
            MobType.Creeper => BlockId.Gunpowder,
            MobType.Cow => BlockId.Leather,
            MobType.Sheep => BlockId.String,
            MobType.Chicken => BlockId.Feather,
            MobType.Herobrine => BlockId.Torch,
            _ => BlockId.Stone
        };
    }

    private static BlockId GetMobHurtBlock(MobType type)
    {
        return type switch
        {
            MobType.Zombie => BlockId.RottenFlesh,
            MobType.Creeper => BlockId.Gunpowder,
            MobType.Cow => BlockId.Leather,
            MobType.Sheep => BlockId.String,
            MobType.Chicken => BlockId.Feather,
            MobType.Herobrine => BlockId.Torch,
            _ => BlockId.Stone
        };
    }

    private static BlockId GetMobDeathBlock(MobType type)
    {
        return type switch
        {
            MobType.Zombie => BlockId.RottenFlesh,
            MobType.Creeper => BlockId.Gunpowder,
            MobType.Cow => BlockId.RawBeef,
            MobType.Sheep => BlockId.RawMutton,
            MobType.Chicken => BlockId.RawChicken,
            MobType.Herobrine => BlockId.Diamond,
            _ => BlockId.Stone
        };
    }

    private static BlockId GetEliteAttackFlashBlock(EliteMobVariant eliteVariant)
    {
        return eliteVariant switch
        {
            EliteMobVariant.Brute => BlockId.IronIngot,
            EliteMobVariant.Hunter => BlockId.GoldIngot,
            EliteMobVariant.Warden => BlockId.MobTrophy,
            _ => BlockId.GoldIngot
        };
    }

    private float GetEliteParticleMultiplier(bool elite, EliteMobVariant eliteVariant)
    {
        if (!elite)
        {
            return 1f;
        }

        float variantMultiplier = eliteVariant switch
        {
            EliteMobVariant.Brute => 0.98f,
            EliteMobVariant.Hunter => 1.05f,
            EliteMobVariant.Warden => 1.18f,
            _ => 1f
        };

        return Math.Max(0.5f, _config.EliteMobParticleMultiplier * variantMultiplier);
    }

    private float NextFloat()
    {
        return (float)_random.NextDouble();
    }

    public void Dispose()
    {
        _spriteBatch.Dispose();
    }

    private enum ParticleVisual
    {
        Block = 0,
        Smoke = 1,
        Flame0 = 2,
        Flame1 = 3
    }

    private struct Particle
    {
        public ParticleVisual Visual;
        public BlockId Block;
        public Vector3 Position;
        public Vector3 Velocity;
        public float Age;
        public float Lifetime;
        public float Size;
        public float Brightness;
        public Vector4 Tint;
        public float GravityScale;
        public float MaxScreenSize;

        public Particle(ParticleVisual visual, BlockId block, Vector3 position, Vector3 velocity, float lifetime, float size, float brightness, Vector4 tint, float gravityScale, float maxScreenSize)
        {
            Visual = visual;
            Block = block;
            Position = position;
            Velocity = velocity;
            Age = 0f;
            Lifetime = lifetime;
            Size = size;
            Brightness = brightness;
            Tint = tint == default ? Vector4.One : tint;
            GravityScale = gravityScale;
            MaxScreenSize = maxScreenSize;
        }
    }
}
