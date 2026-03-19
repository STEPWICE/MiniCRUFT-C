using System;
using System.Collections.Generic;
using System.Numerics;
using MiniCRUFT.Core;
using Veldrid;

namespace MiniCRUFT.Renderer;

public sealed class WeatherOverlayRenderer : IDisposable
{
    private readonly WeatherConfig _config;
    private readonly Texture _texture;
    private readonly Sampler _sampler;
    private readonly SpriteBatch _batch;
    private readonly SpriteRegion _region;
    private readonly List<RainStreak> _streaks = new();
    private readonly Random _random = new();
    private float _spawnAccumulator;

    public WeatherOverlayRenderer(GraphicsDevice device, WeatherConfig config)
    {
        _config = config;
        _texture = device.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
            1, 1, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm_SRgb, TextureUsage.Sampled));
        device.UpdateTexture(_texture, new byte[] { 255, 255, 255, 255 }, 0, 0, 0, 1, 1, 1, 0, 0);
        _sampler = device.ResourceFactory.CreateSampler(new SamplerDescription
        {
            AddressModeU = SamplerAddressMode.Clamp,
            AddressModeV = SamplerAddressMode.Clamp,
            AddressModeW = SamplerAddressMode.Clamp,
            Filter = SamplerFilter.MinPoint_MagPoint_MipPoint,
            MaximumAnisotropy = 1
        });
        _batch = new SpriteBatch(device, _texture, _sampler);
        _region = new SpriteRegion(Vector2.Zero, Vector2.One, Vector2.One);
    }

    public void Update(float dt, float rainIntensity, int width, int height)
    {
        if (dt <= 0f || width <= 0 || height <= 0)
        {
            return;
        }

        float intensity = Math.Clamp(rainIntensity, 0f, 1f);
        if (_streaks.Count > 0)
        {
            for (int i = _streaks.Count - 1; i >= 0; i--)
            {
                var streak = _streaks[i];
                streak.Age += dt;
                streak.Position += streak.Velocity * dt;
                if (streak.Age >= streak.Lifetime ||
                    streak.Position.Y > height + streak.Length ||
                    streak.Position.X < -streak.Length ||
                    streak.Position.X > width + streak.Length)
                {
                    _streaks.RemoveAt(i);
                    continue;
                }

                _streaks[i] = streak;
            }
        }

        if (intensity <= 0f || _config.RainSpawnRate <= 0f || _config.RainMaxParticles == 0)
        {
            return;
        }

        _spawnAccumulator += intensity * _config.RainSpawnRate * dt;
        int spawnCount = (int)_spawnAccumulator;
        if (spawnCount <= 0)
        {
            return;
        }

        _spawnAccumulator -= spawnCount;
        int allowed = Math.Max(0, _config.RainMaxParticles - _streaks.Count);
        spawnCount = Math.Min(spawnCount, allowed);
        for (int i = 0; i < spawnCount; i++)
        {
            _streaks.Add(SpawnStreak(width, height));
        }
    }

    public void Draw(CommandList commandList, int width, int height, AtmosphereFrame atmosphere)
    {
        float flashIntensity = Math.Clamp(atmosphere.LightningFlashIntensity, 0f, 1f);
        if (_streaks.Count == 0 && flashIntensity <= 0f)
        {
            return;
        }

        _batch.Begin(width, height);
        if (flashIntensity > 0f)
        {
            var flashColor = new Vector4(1f, 1f, 1f, Math.Clamp(flashIntensity * 0.45f, 0f, 0.75f));
            _batch.Draw(_region, Vector2.Zero, new Vector2(width, height), flashColor);
        }

        float rainAlpha = Math.Clamp(Math.Max(atmosphere.RainIntensity, 0.08f), 0f, 1f);
        var streakTintLinear = ColorSpace.ToLinear(_config.RainStreakTint.ToVector3());
        var color = new Vector4(streakTintLinear * (0.6f + rainAlpha * 0.4f), _config.RainStreakAlpha * rainAlpha);

        for (int i = 0; i < _streaks.Count; i++)
        {
            var streak = _streaks[i];
            float alpha = color.W * Math.Clamp(1f - (streak.Age / streak.Lifetime), 0.15f, 1f);
            var streakColor = new Vector4(color.X, color.Y, color.Z, alpha);
            _batch.Draw(_region, streak.Position, new Vector2(streak.Thickness, streak.Length), streakColor);
        }

        _batch.Flush(commandList);
    }

    public void Dispose()
    {
        _batch.Dispose();
        _sampler.Dispose();
        _texture.Dispose();
    }

    private RainStreak SpawnStreak(int width, int height)
    {
        float length = _config.RainParticleLength * (0.8f + NextFloat() * 0.4f);
        float speed = _config.RainParticleSpeed * (0.85f + NextFloat() * 0.35f);
        float thickness = _config.RainParticleWidth * (0.8f + NextFloat() * 0.4f);
        float x = NextFloat() * width;
        float y = -length - NextFloat() * height * 0.35f;
        float lifetime = Math.Max(0.2f, ((height + length) / Math.Max(speed, 1f)) * (0.85f + NextFloat() * 0.3f));
        var position = new Vector2(x, y);
        var velocity = new Vector2(NextFloat() * 4f - 2f, speed);
        return new RainStreak(position, velocity, length, thickness, lifetime);
    }

    private float NextFloat()
    {
        return (float)_random.NextDouble();
    }

    private struct RainStreak
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Length;
        public float Thickness;
        public float Lifetime;
        public float Age;

        public RainStreak(Vector2 position, Vector2 velocity, float length, float thickness, float lifetime)
        {
            Position = position;
            Velocity = velocity;
            Length = length;
            Thickness = thickness;
            Lifetime = lifetime;
            Age = 0f;
        }
    }
}
