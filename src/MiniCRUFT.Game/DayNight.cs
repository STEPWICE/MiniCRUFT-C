using System;
using System.Numerics;
using MiniCRUFT.Core;

namespace MiniCRUFT.Game;

public sealed class DayNightCycle
{
    private readonly DayNightConfig _config;
    public float TimeOfDay { get; private set; }

    public DayNightCycle(DayNightConfig config)
    {
        _config = config;
        TimeOfDay = _config.StartTimeOfDay;
    }

    public void Reset(float? timeOfDay = null)
    {
        TimeOfDay = Math.Clamp(timeOfDay ?? _config.StartTimeOfDay, 0f, 1f);
    }

    public void Update(float dt)
    {
        TimeOfDay += dt / _config.DayLengthSeconds;
        if (TimeOfDay >= 1f)
        {
            TimeOfDay -= 1f;
        }
    }

    public Vector4 GetSkyColor()
    {
        float t = MathF.Abs(TimeOfDay - 0.5f) * 2f;
        var day = new Vector4(0.52f, 0.75f, 1.0f, 1f);
        var night = new Vector4(0.03f, 0.04f, 0.08f, 1f);
        return Vector4.Lerp(day, night, t);
    }

    public float GetSunIntensity()
    {
        float angle = (TimeOfDay - 0.25f) * 2f * MathF.PI;
        float value = MathF.Cos(angle);
        return Math.Clamp(value, _config.MinSunIntensity, 1f);
    }
}

public sealed class WeatherSystem
{
    private readonly WeatherConfig _config;
    private readonly Random _random = new();
    private float _timer;
    public bool IsRaining { get; private set; }

    public WeatherSystem(WeatherConfig config)
    {
        _config = config;
    }

    public void Reset()
    {
        _timer = 0f;
        IsRaining = false;
    }

    public void Update(float dt)
    {
        _timer += dt;
        if (_timer > _config.ToggleIntervalSeconds)
        {
            _timer = 0f;
            if (_random.NextDouble() < _config.ToggleChance)
            {
                IsRaining = !IsRaining;
            }
        }
    }

    public Vector4 Apply(Vector4 sky)
    {
        if (!IsRaining)
        {
            return sky;
        }

        return new Vector4(sky.X * _config.RainDarkenR, sky.Y * _config.RainDarkenG, sky.Z * _config.RainDarkenB, 1f);
    }
}
