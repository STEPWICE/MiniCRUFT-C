using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiniCRUFT.Core;

public sealed class GameConfig
{
    public int ChunkRadius { get; set; } = 8;
    public bool VSync { get; set; } = true;
    public bool Fullscreen { get; set; } = false;
    public int WindowWidth { get; set; } = 1280;
    public int WindowHeight { get; set; } = 720;
    public int Seed { get; set; } = 1337;
    public string AssetsPath { get; set; } = "assets";
    public string WorldPath { get; set; } = "world";
    public string Language { get; set; } = "ru";
    public float FieldOfView { get; set; } = 75.0f;
    public float MouseSensitivity { get; set; } = 0.12f;
    public float PlayerSpeed { get; set; } = 6.0f;
    public bool ShowDebug { get; set; } = false;
    public bool ResetWorldOnLaunch { get; set; } = false;
    public int ChunkPreloadExtra { get; set; } = 1;
    public PhysicsConfig Physics { get; set; } = new();
    public DayNightConfig DayNight { get; set; } = new();
    public WeatherConfig Weather { get; set; } = new();
    public AudioConfig Audio { get; set; } = new();
    [JsonPropertyName("Save")]
    public SaveConfig SaveSettings { get; set; } = new();
    public SpawnConfig Spawn { get; set; } = new();
    public RenderConfig Render { get; set; } = new();
    public AtmosphereConfig Atmosphere { get; set; } = new();
    public UiConfig Ui { get; set; } = new();
    public WorldGenConfig WorldGen { get; set; } = new();

    public static GameConfig LoadOrCreate(string path)
    {
        if (!File.Exists(path))
        {
            var config = new GameConfig();
            config.Normalize();
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
            return config;
        }

        var text = File.ReadAllText(path);
        var loaded = JsonSerializer.Deserialize<GameConfig>(text);
        if (loaded == null)
        {
            return new GameConfig();
        }

        loaded.Normalize();
        return loaded;
    }

    public static void Save(string path, GameConfig config)
    {
        config.Normalize();
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public void Normalize()
    {
        Spawn ??= new SpawnConfig();
        Physics ??= new PhysicsConfig();
        DayNight ??= new DayNightConfig();
        Weather ??= new WeatherConfig();
        Audio ??= new AudioConfig();
        SaveSettings ??= new SaveConfig();
        Render ??= new RenderConfig();
        Atmosphere ??= new AtmosphereConfig();
        Ui ??= new UiConfig();
        WorldGen ??= new WorldGenConfig();
        Render.Fog ??= new FogConfig();
        Render.Foliage ??= new FoliageConfig();
        Render.Lod ??= new LodConfig();

        if (Spawn.SearchRadius < 64)
        {
            Spawn.SearchRadius = 64;
        }

        if (Spawn.MaxAttempts < 1)
        {
            Spawn.MaxAttempts = 1;
        }

        if (Spawn.MinHeightAboveSea < 1)
        {
            Spawn.MinHeightAboveSea = 1;
        }

        if (Render.Anisotropy < 1)
        {
            Render.Anisotropy = 1;
        }

        if (Render.MaxMeshUploadsPerFrame < 1)
        {
            Render.MaxMeshUploadsPerFrame = 1;
        }

        if (Render.FogEnd < Render.FogStart + 10f)
        {
            Render.FogEnd = Render.FogStart + 10f;
        }

        if (ChunkPreloadExtra < 0)
        {
            ChunkPreloadExtra = 0;
        }

        if (Physics.PlayerWidth < 0.2f)
        {
            Physics.PlayerWidth = 0.2f;
        }

        if (Physics.PlayerHeight < 1.0f)
        {
            Physics.PlayerHeight = 1.0f;
        }

        if (Physics.EyeHeight < 0.5f)
        {
            Physics.EyeHeight = 0.5f;
        }

        if (Physics.EyeHeight > Physics.PlayerHeight)
        {
            Physics.EyeHeight = Physics.PlayerHeight;
        }

        if (Physics.SprintMultiplier < 1f)
        {
            Physics.SprintMultiplier = 1f;
        }

        if (DayNight.DayLengthSeconds < 60f)
        {
            DayNight.DayLengthSeconds = 60f;
        }

        DayNight.StartTimeOfDay = Math.Clamp(DayNight.StartTimeOfDay, 0f, 1f);
        DayNight.MinSunIntensity = Math.Clamp(DayNight.MinSunIntensity, 0f, 1f);

        if (Weather.ToggleIntervalSeconds < 10f)
        {
            Weather.ToggleIntervalSeconds = 10f;
        }

        Weather.ToggleChance = Math.Clamp(Weather.ToggleChance, 0f, 1f);

        if (Audio.MaxActive < 1)
        {
            Audio.MaxActive = 1;
        }

        if (SaveSettings.AutoSaveIntervalSeconds < 5f)
        {
            SaveSettings.AutoSaveIntervalSeconds = 5f;
        }

        if (SaveSettings.SaveWorkers < 1)
        {
            SaveSettings.SaveWorkers = 1;
        }

        if (SaveSettings.UnloadExtraRadius < 0)
        {
            SaveSettings.UnloadExtraRadius = 0;
        }

        if (WorldGen.LargeTreeMinHeight < 4)
        {
            WorldGen.LargeTreeMinHeight = 4;
        }

        if (WorldGen.LargeTreeMaxHeight < WorldGen.LargeTreeMinHeight + 1)
        {
            WorldGen.LargeTreeMaxHeight = WorldGen.LargeTreeMinHeight + 1;
        }

        if (WorldGen.LargeTreeChance < 0f)
        {
            WorldGen.LargeTreeChance = 0f;
        }
        else if (WorldGen.LargeTreeChance > 1f)
        {
            WorldGen.LargeTreeChance = 1f;
        }

        if (WorldGen.TreeMaxSlope < 1f)
        {
            WorldGen.TreeMaxSlope = 1f;
        }

        if (Render.Lod.EndDistance < Render.Lod.StartDistance + Render.Lod.Step)
        {
            Render.Lod.EndDistance = Render.Lod.StartDistance + Render.Lod.Step;
        }
    }
}
