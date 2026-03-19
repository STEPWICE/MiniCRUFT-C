using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.Game;
using MiniCRUFT.IO;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class DayNightWeatherTests
{
    [Fact]
    public void DayNightCycle_TracksDayCountAndMoonPhase()
    {
        var config = new DayNightConfig
        {
            DayLengthSeconds = 10f,
            StartTimeOfDay = 0.95f,
            MinSunIntensity = 0.05f,
            SunIntensityCurve = 1.1f,
            MoonPhaseCycleDays = 8
        };

        var cycle = new DayNightCycle(config);
        cycle.Reset(0.95f);
        cycle.Update(1f);

        Assert.Equal(1L, cycle.DayCount);
        Assert.InRange(cycle.TimeOfDay, 0.04f, 0.06f);
        Assert.Equal(1, cycle.GetMoonPhaseIndex());
        Assert.InRange(cycle.GetSunIntensity(), 0.05f, 1f);
    }

    [Fact]
    public void DayNightCycle_HandlesLargeTimeSteps()
    {
        var config = new DayNightConfig
        {
            DayLengthSeconds = 10f,
            StartTimeOfDay = 0.1f
        };

        var cycle = new DayNightCycle(config);
        cycle.Reset(0.1f);
        cycle.Update(31f);

        Assert.Equal(3L, cycle.DayCount);
        Assert.InRange(cycle.TimeOfDay, 0.1f, 0.2f);
    }

    [Fact]
    public void WeatherSystem_TogglesAndAppliesRainTint()
    {
        var config = new WeatherConfig
        {
            ToggleIntervalSeconds = 1f,
            ToggleChance = 1f,
            RainDarkenR = 0.65f,
            RainDarkenG = 0.7f,
            RainDarkenB = 0.75f,
            RainFadeInSeconds = 1f,
            RainFadeOutSeconds = 1f
        };

        var weather = new WeatherSystem(config);
        weather.Reset();

        weather.Update(1.1f);
        Assert.True(weather.IsRaining);
        Assert.Equal(1f, weather.RainIntensity, 3);

        var sky = new Vector3(0.7f, 0.8f, 1f);
        var rainySky = weather.ApplySky(sky);
        Assert.True(rainySky.X < sky.X);
        Assert.True(rainySky.Y < sky.Y);
        Assert.True(rainySky.Z < sky.Z);

        weather.Update(1.1f);
        Assert.False(weather.IsRaining);
        Assert.Equal(0f, weather.RainIntensity, 3);

        var fog = weather.ApplyFog(sky);
        Assert.Equal(sky, fog);
    }

    [Fact]
    public void WeatherSystem_CreatesLightningFlashAndThunderDuringRain()
    {
        var config = new WeatherConfig
        {
            ToggleChance = 0f,
            LightningChancePerSecond = 1f,
            LightningMinRainIntensity = 0f,
            LightningFlashFadeSeconds = 2f,
            LightningFlashStrength = 1f,
            LightningThunderDelaySeconds = 0.1f,
            LightningCooldownSeconds = 1f
        };

        var weather = new WeatherSystem(config);
        weather.Load(new WeatherSaveData(true, 1f, 0f));

        weather.Update(1f, 0f);

        Assert.True(weather.LightningFlashIntensity > 0f);
        weather.Update(0.2f, 0f);
        Assert.True(weather.TryConsumeThunderEvent());
        Assert.False(weather.TryConsumeThunderEvent());

        float flashBefore = weather.LightningFlashIntensity;
        weather.Update(0.5f, 0f);

        Assert.True(weather.LightningFlashIntensity < flashBefore);
    }

    [Fact]
    public void WorldSave_RoundTripsDayNightState()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_daynight_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var expected = new DayNightSaveData(0.42f, 17);
            WorldSave.SaveDayNight(root, expected);

            var loaded = WorldSave.LoadDayNight(root, new DayNightSaveData(0.25f, 0));

            Assert.Equal(0.42f, loaded.TimeOfDay, 3);
            Assert.Equal(17L, loaded.DayCount);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void WorldSave_RoundTripsWeatherState()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_weather_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var expected = new WeatherSaveData(true, 0.64f, 3.25f);
            WorldSave.SaveWeather(root, expected);

            var loaded = WorldSave.LoadWeather(root, new WeatherSaveData(false, 0f, 0f));

            Assert.True(loaded.TargetRaining);
            Assert.Equal(0.64f, loaded.RainIntensity, 3);
            Assert.Equal(3.25f, loaded.Timer, 3);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
