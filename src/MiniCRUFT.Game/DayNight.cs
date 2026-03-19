using System;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.IO;

namespace MiniCRUFT.Game;

public sealed class DayNightCycle
{
    private readonly DayNightConfig _config;
    public float TimeOfDay { get; private set; }
    public long DayCount { get; private set; }

    public DayNightCycle(DayNightConfig config)
    {
        _config = config;
        TimeOfDay = _config.StartTimeOfDay;
    }

    public void Reset(float? timeOfDay = null)
    {
        SetState(timeOfDay ?? _config.StartTimeOfDay, 0);
    }

    public void SetState(float timeOfDay, long dayCount)
    {
        TimeOfDay = Math.Clamp(timeOfDay, 0f, 1f);
        DayCount = Math.Max(0, dayCount);
    }

    public void Update(float dt)
    {
        if (dt <= 0f)
        {
            return;
        }

        float dayLengthSeconds = Math.Max(1f, _config.DayLengthSeconds);
        TimeOfDay += dt / dayLengthSeconds;
        while (TimeOfDay >= 1f)
        {
            TimeOfDay -= 1f;
            DayCount++;
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
        value = MathF.Max(0f, value);
        value = MathF.Pow(value, Math.Max(0.01f, _config.SunIntensityCurve));
        return Math.Clamp(value, _config.MinSunIntensity, 1f);
    }

    public int GetMoonPhaseIndex()
    {
        int cycleDays = Math.Max(1, _config.MoonPhaseCycleDays);
        long dayInCycle = DayCount % cycleDays;
        if (dayInCycle < 0)
        {
            dayInCycle += cycleDays;
        }

        return (int)(dayInCycle * 8L / cycleDays) & 7;
    }
}

public sealed class WeatherSystem
{
    private readonly WeatherConfig _config;
    private readonly Random _random = new();
    private float _timer;
    private bool _targetRaining;
    private float _rainIntensity;
    private float _lightningFlash;
    private float _thunderTimer;
    private float _lightningCooldown;
    private bool _thunderReady;
    public bool IsRaining { get; private set; }
    public float RainIntensity => _rainIntensity;
    public float LightningFlashIntensity => _lightningFlash * _config.LightningFlashStrength;

    public WeatherSystem(WeatherConfig config)
    {
        _config = config;
    }

    public void Reset()
    {
        _timer = 0f;
        _targetRaining = false;
        _rainIntensity = 0f;
        _lightningFlash = 0f;
        _thunderTimer = 0f;
        _lightningCooldown = 0f;
        _thunderReady = false;
        IsRaining = false;
    }

    public void Update(float dt, float sunIntensity = 1f)
    {
        if (dt <= 0f)
        {
            return;
        }

        _timer += dt;
        float toggleInterval = Math.Max(0.1f, _config.ToggleIntervalSeconds);
        while (_timer >= toggleInterval)
        {
            _timer -= toggleInterval;
            if (_random.NextDouble() < _config.ToggleChance)
            {
                _targetRaining = !_targetRaining;
            }
        }

        float target = _targetRaining ? 1f : 0f;
        float fadeSeconds = target > _rainIntensity
            ? Math.Max(0.1f, _config.RainFadeInSeconds)
            : Math.Max(0.1f, _config.RainFadeOutSeconds);
        float step = dt / fadeSeconds;
        if (_rainIntensity < target)
        {
            _rainIntensity = Math.Min(target, _rainIntensity + step);
        }
        else if (_rainIntensity > target)
        {
            _rainIntensity = Math.Max(target, _rainIntensity - step);
        }

        IsRaining = _rainIntensity > 0.01f;
        UpdateLightning(dt, sunIntensity);
    }

    public WeatherSaveData BuildSaveData()
    {
        return new WeatherSaveData(_targetRaining, _rainIntensity, _timer);
    }

    public void Load(WeatherSaveData data)
    {
        _targetRaining = data.TargetRaining;
        _rainIntensity = Math.Clamp(data.RainIntensity, 0f, 1f);
        _timer = Math.Max(0f, data.Timer);
        IsRaining = _rainIntensity > 0.01f;
    }

    public bool TryConsumeThunderEvent()
    {
        if (!_thunderReady)
        {
            return false;
        }

        _thunderReady = false;
        return true;
    }

    public Vector3 ApplySky(Vector3 sky)
    {
        if (_rainIntensity <= 0f)
        {
            return sky;
        }

        return ApplyWeatherTint(sky, 0.32f);
    }

    public Vector3 ApplyFog(Vector3 fog)
    {
        if (_rainIntensity <= 0f)
        {
            return fog;
        }

        return ApplyWeatherTint(fog, 0.46f);
    }

    private Vector3 ApplyWeatherTint(Vector3 color, float desaturateStrength)
    {
        float rain = _rainIntensity;
        float luminance = Vector3.Dot(color, new Vector3(0.2126f, 0.7152f, 0.0722f));
        var gray = new Vector3(luminance, luminance, luminance);
        color = Vector3.Lerp(color, gray, rain * desaturateStrength);

        var dark = new Vector3(_config.RainDarkenR, _config.RainDarkenG, _config.RainDarkenB);
        color = Vector3.Lerp(color, color * dark, rain);
        return color;
    }

    private void UpdateLightning(float dt, float sunIntensity)
    {
        if (_lightningFlash > 0f)
        {
            float flashFade = Math.Max(0.05f, _config.LightningFlashFadeSeconds);
            _lightningFlash = Math.Max(0f, _lightningFlash - dt / flashFade);
        }

        if (_lightningCooldown > 0f)
        {
            _lightningCooldown = Math.Max(0f, _lightningCooldown - dt);
        }

        if (_thunderTimer > 0f)
        {
            _thunderTimer -= dt;
            if (_thunderTimer <= 0f)
            {
                _thunderReady = true;
            }
        }

        if (_lightningCooldown > 0f)
        {
            return;
        }

        float minRain = Math.Clamp(_config.LightningMinRainIntensity, 0f, 1f);
        if (_rainIntensity < minRain)
        {
            return;
        }

        float chance = Math.Clamp(_config.LightningChancePerSecond, 0f, 1f);
        float nightFactor = 1f - Math.Clamp(sunIntensity, 0f, 1f);
        chance *= dt;
        chance *= 0.35f + nightFactor * 0.65f;
        chance *= 0.25f + _rainIntensity * 0.75f;

        if (_random.NextDouble() <= chance)
        {
            TriggerLightning();
        }
    }

    private void TriggerLightning()
    {
        _lightningFlash = 1f;
        _thunderTimer = Math.Max(0.05f, _config.LightningThunderDelaySeconds);
        _lightningCooldown = Math.Max(1f, _config.LightningCooldownSeconds);
        _thunderReady = false;
    }
}
