using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiniCRUFT.Core;

public sealed class GameConfig
{
    private const int MaxParticleCount = 4096;
    private const int MaxFireUpdatesPerFrame = 256;
    private const int MaxFireEventQueue = 4096;
    private const int MaxFireExplosionIgnitedBlocks = 256;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public int ChunkRadius { get; set; } = 8;
    public bool VSync { get; set; } = true;
    public bool Fullscreen { get; set; } = false;
    public int WindowX { get; set; } = 100;
    public int WindowY { get; set; } = 100;
    public int WindowWidth { get; set; } = 1280;
    public int WindowHeight { get; set; } = 720;
    public int Seed { get; set; } = 1337;
    public string AssetsPath { get; set; } = "assets";
    public string WorldPath { get; set; } = "world";
    public string Language { get; set; } = "ru";
    public bool StrictBetaMode { get; set; } = false;
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
    public MobConfig Mob { get; set; } = new();
    public TntConfig Tnt { get; set; } = new();
    public FallingConfig Falling { get; set; } = new();
    public FluidConfig Fluid { get; set; } = new();
    public FireConfig Fire { get; set; } = new();
    public RenderConfig Render { get; set; } = new();
    public FirstPersonConfig FirstPerson { get; set; } = new();
    public CameraMotionConfig CameraMotion { get; set; } = new();
    public AtmosphereConfig Atmosphere { get; set; } = new();
    public ParticleConfig Particles { get; set; } = new();
    public UiConfig Ui { get; set; } = new();
    public HerobrineConfig Herobrine { get; set; } = new();
    public SurvivalConfig Survival { get; set; } = new();
    public ToolConfig Tools { get; set; } = new();
    public WorldGenConfig WorldGen { get; set; } = new();

    public static GameConfig LoadOrCreate(string path)
    {
        if (!File.Exists(path))
        {
            var config = CreateDefaultConfig(strictBetaMode: true);
            config.Normalize();
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(path, json);
            return config;
        }

        try
        {
            var text = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<GameConfig>(text, JsonOptions);
            if (loaded == null)
            {
                Log.Warn($"Config file {path} is empty or unsupported, using defaults.");
                loaded = CreateDefaultConfig(strictBetaMode: true);
            }

            loaded.Normalize();
            return loaded;
        }
        catch (JsonException ex)
        {
            Log.Warn($"Config file {path} is invalid JSON, using defaults: {ex.Message}");
        }
        catch (NotSupportedException ex)
        {
            Log.Warn($"Config file {path} uses an unsupported value type, using defaults: {ex.Message}");
        }
        catch (IOException ex)
        {
            Log.Warn($"Failed to read config file {path}, using defaults: {ex.Message}");
        }

        var fallback = CreateDefaultConfig(strictBetaMode: true);
        fallback.Normalize();
        return fallback;
    }

    public static void Save(string path, GameConfig config)
    {
        config.Normalize();
        var json = JsonSerializer.Serialize(config, JsonOptions);
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
        FirstPerson ??= new FirstPersonConfig();
        CameraMotion ??= new CameraMotionConfig();
        Atmosphere ??= new AtmosphereConfig();
        Ui ??= new UiConfig();
        Herobrine ??= new HerobrineConfig();
        Survival ??= new SurvivalConfig();
        Tools ??= new ToolConfig();
        WorldGen ??= new WorldGenConfig();
        Mob ??= new MobConfig();
        Tnt ??= new TntConfig();
        Falling ??= new FallingConfig();
        Fluid ??= new FluidConfig();
        Fire ??= new FireConfig();
        Render.FaceShading ??= new FaceShadingConfig();
        Render.Palette ??= new PaletteConfig();
        Render.Fog ??= new FogConfig();
        Render.Foliage ??= new FoliageConfig();
        Render.Lod ??= new LodConfig();
        Particles ??= new ParticleConfig();
        WorldGen.StrictBetaMode = StrictBetaMode;

        if (StrictBetaMode)
        {
            StrictBetaProfile.Apply(this);
        }

        if (StrictBetaMode)
        {
            Survival.Enabled = false;
        }
        if (Survival.MaxHunger < 1)
        {
            Survival.MaxHunger = 1;
        }

        Survival.StartingHunger = Math.Clamp(Survival.StartingHunger, 0f, Survival.MaxHunger);
        if (Survival.BaseDrainPerSecond < 0f)
        {
            Survival.BaseDrainPerSecond = 0f;
        }

        if (Survival.SprintDrainMultiplier < 1f)
        {
            Survival.SprintDrainMultiplier = 1f;
        }

        if (Survival.LiquidDrainMultiplier < 1f)
        {
            Survival.LiquidDrainMultiplier = 1f;
        }

        if (Survival.MinHungerToSprint < 0)
        {
            Survival.MinHungerToSprint = 0;
        }
        else if (Survival.MinHungerToSprint > Survival.MaxHunger)
        {
            Survival.MinHungerToSprint = Survival.MaxHunger;
        }

        if (Survival.StarvationDamageIntervalSeconds < 0.1f)
        {
            Survival.StarvationDamageIntervalSeconds = 0.1f;
        }

        if (Survival.StarvationDamage < 1)
        {
            Survival.StarvationDamage = 1;
        }

        Survival.RestMinSunIntensity = Math.Clamp(Survival.RestMinSunIntensity, 0f, 1f);
        if (Survival.RestThreatRadius < 0f)
        {
            Survival.RestThreatRadius = 0f;
        }

        Survival.RestWakeTimeOfDay = Math.Clamp(Survival.RestWakeTimeOfDay, 0f, 1f);

        if (WorldGen.ChunkGenerationWorkers < 1)
        {
            WorldGen.ChunkGenerationWorkers = WorldGenConfig.DefaultChunkGenerationWorkers;
        }

        if (Tools.WoodMaxDurability < 1)
        {
            Tools.WoodMaxDurability = 1;
        }
        else if (Tools.WoodMaxDurability > 100000)
        {
            Tools.WoodMaxDurability = 100000;
        }

        if (Tools.StoneMaxDurability < 1)
        {
            Tools.StoneMaxDurability = 1;
        }
        else if (Tools.StoneMaxDurability > 100000)
        {
            Tools.StoneMaxDurability = 100000;
        }

        if (Tools.IronMaxDurability < 1)
        {
            Tools.IronMaxDurability = 1;
        }
        else if (Tools.IronMaxDurability > 100000)
        {
            Tools.IronMaxDurability = 100000;
        }

        if (Tools.DiamondMaxDurability < 1)
        {
            Tools.DiamondMaxDurability = 1;
        }
        else if (Tools.DiamondMaxDurability > 100000)
        {
            Tools.DiamondMaxDurability = 100000;
        }

        if (Tools.ToolWearPerAction < 1)
        {
            Tools.ToolWearPerAction = 1;
        }
        else if (Tools.ToolWearPerAction > 64)
        {
            Tools.ToolWearPerAction = 64;
        }

        Tools.BlockBreakTimeScale = Math.Clamp(Tools.BlockBreakTimeScale, 0.05f, 5f);
        Tools.MinBreakSeconds = Math.Clamp(Tools.MinBreakSeconds, 0.01f, 2f);
        Tools.WrongToolBreakPenalty = Math.Clamp(Tools.WrongToolBreakPenalty, 1f, 8f);
        Tools.WoodMiningSpeedMultiplier = Math.Clamp(Tools.WoodMiningSpeedMultiplier, 0.1f, 16f);
        Tools.StoneMiningSpeedMultiplier = Math.Clamp(Tools.StoneMiningSpeedMultiplier, 0.1f, 16f);
        Tools.IronMiningSpeedMultiplier = Math.Clamp(Tools.IronMiningSpeedMultiplier, 0.1f, 16f);
        Tools.DiamondMiningSpeedMultiplier = Math.Clamp(Tools.DiamondMiningSpeedMultiplier, 0.1f, 16f);

        if (Tools.WoodRepairDurability < 1)
        {
            Tools.WoodRepairDurability = 1;
        }
        else if (Tools.WoodRepairDurability > 100000)
        {
            Tools.WoodRepairDurability = 100000;
        }

        if (Tools.StoneRepairDurability < 1)
        {
            Tools.StoneRepairDurability = 1;
        }
        else if (Tools.StoneRepairDurability > 100000)
        {
            Tools.StoneRepairDurability = 100000;
        }

        if (Tools.IronRepairDurability < 1)
        {
            Tools.IronRepairDurability = 1;
        }
        else if (Tools.IronRepairDurability > 100000)
        {
            Tools.IronRepairDurability = 100000;
        }

        if (Tools.DiamondRepairDurability < 1)
        {
            Tools.DiamondRepairDurability = 1;
        }
        else if (Tools.DiamondRepairDurability > 100000)
        {
            Tools.DiamondRepairDurability = 100000;
        }

        WorldGen.StructureChance = Math.Clamp(WorldGen.StructureChance, 0f, 1f);
        WorldGen.CampChance = Math.Clamp(WorldGen.CampChance, 0f, 1f);
        WorldGen.WatchtowerChance = Math.Clamp(WorldGen.WatchtowerChance, 0f, 1f);
        WorldGen.BuriedCacheChance = Math.Clamp(WorldGen.BuriedCacheChance, 0f, 1f);
        WorldGen.CaveCacheChance = Math.Clamp(WorldGen.CaveCacheChance, 0f, 1f);
        WorldGen.RuinChance = Math.Clamp(WorldGen.RuinChance, 0f, 1f);
        WorldGen.MineShaftChance = Math.Clamp(WorldGen.MineShaftChance, 0f, 1f);
        if (WorldGen.StructureMargin < 0)
        {
            WorldGen.StructureMargin = 0;
        }
        else if (WorldGen.StructureMargin > 16)
        {
            WorldGen.StructureMargin = 16;
        }

        if (Spawn.MaxAttempts < 1)
        {
            Spawn.MaxAttempts = 1;
        }

        if (Spawn.MinHeightAboveSea < 1)
        {
            Spawn.MinHeightAboveSea = 1;
        }

        if (Spawn.SearchRadius < 64)
        {
            Spawn.SearchRadius = 64;
        }

        if (Render.Anisotropy < 1)
        {
            Render.Anisotropy = 1;
        }
        else if (Render.Anisotropy > 16)
        {
            Render.Anisotropy = 16;
        }

        Render.FaceShading.Top = Math.Clamp(Render.FaceShading.Top, 0f, 2f);
        Render.FaceShading.Side = Math.Clamp(Render.FaceShading.Side, 0f, 2f);
        Render.FaceShading.Bottom = Math.Clamp(Render.FaceShading.Bottom, 0f, 2f);
        Render.FaceShading.Strength = Math.Clamp(Render.FaceShading.Strength, 0f, 1f);

        Render.Palette.SandStrength = Math.Clamp(Render.Palette.SandStrength, 0f, 1f);
        Render.Palette.StoneStrength = Math.Clamp(Render.Palette.StoneStrength, 0f, 1f);

        if (Render.MaxMeshUploadsPerFrame < 1)
        {
            Render.MaxMeshUploadsPerFrame = 1;
        }

        if (Render.ChunkMeshWorkers < 1)
        {
            Render.ChunkMeshWorkers = RenderConfig.DefaultChunkMeshWorkers;
        }

        if (Render.FogEnd < Render.FogStart + 10f)
        {
            Render.FogEnd = Render.FogStart + 10f;
        }

        if (WindowWidth < 1)
        {
            WindowWidth = 1;
        }

        if (WindowHeight < 1)
        {
            WindowHeight = 1;
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
        Physics.WaterMoveMultiplier = Math.Clamp(Physics.WaterMoveMultiplier, 0.1f, 1f);
        Physics.WaterGravityMultiplier = Math.Clamp(Physics.WaterGravityMultiplier, 0f, 1f);
        if (Physics.WaterBuoyancy < 0f)
        {
            Physics.WaterBuoyancy = 0f;
        }
        if (Physics.WaterJumpVelocity < 0f)
        {
            Physics.WaterJumpVelocity = 0f;
        }
        if (Physics.WaterMaxFallSpeed < 0f)
        {
            Physics.WaterMaxFallSpeed = 0f;
        }
        Physics.WaterCurrentMultiplier = Math.Clamp(Physics.WaterCurrentMultiplier, 0f, 5f);

        if (DayNight.DayLengthSeconds < 60f)
        {
            DayNight.DayLengthSeconds = 60f;
        }

        DayNight.StartTimeOfDay = Math.Clamp(DayNight.StartTimeOfDay, 0f, 1f);
        DayNight.MinSunIntensity = Math.Clamp(DayNight.MinSunIntensity, 0f, 1f);
        DayNight.SunIntensityCurve = Math.Clamp(DayNight.SunIntensityCurve, 0.1f, 8f);
        if (DayNight.MoonPhaseCycleDays < 1)
        {
            DayNight.MoonPhaseCycleDays = 1;
        }

        Atmosphere.CloudAlphaCutoff = Math.Clamp(Atmosphere.CloudAlphaCutoff, 0f, 1f);
        if (Atmosphere.CloudBlockSize < 1f)
        {
            Atmosphere.CloudBlockSize = 1f;
        }
        if (Atmosphere.SunSize < 1f)
        {
            Atmosphere.SunSize = 1f;
        }
        if (Atmosphere.MoonSize < 1f)
        {
            Atmosphere.MoonSize = 1f;
        }

        Atmosphere.WaterShoreStrength = Math.Clamp(Atmosphere.WaterShoreStrength, 0f, 1f);
        Atmosphere.LavaOpacity = Math.Clamp(Atmosphere.LavaOpacity, 0f, 1f);
        if (Atmosphere.LavaUvSpeed < 0f)
        {
            Atmosphere.LavaUvSpeed = 0f;
        }
        Atmosphere.FogNightMultiplier = Math.Clamp(Atmosphere.FogNightMultiplier, 0.25f, 2f);
        Atmosphere.FogRainMultiplier = Math.Clamp(Atmosphere.FogRainMultiplier, 0.25f, 2f);

        if (Weather.ToggleIntervalSeconds < 10f)
        {
            Weather.ToggleIntervalSeconds = 10f;
        }

        Weather.ToggleChance = Math.Clamp(Weather.ToggleChance, 0f, 1f);
        if (Weather.RainFadeInSeconds < 0.1f)
        {
            Weather.RainFadeInSeconds = 0.1f;
        }
        if (Weather.RainFadeOutSeconds < 0.1f)
        {
            Weather.RainFadeOutSeconds = 0.1f;
        }
        if (Weather.RainSpawnRate < 0f)
        {
            Weather.RainSpawnRate = 0f;
        }
        else if (Weather.RainSpawnRate > 1000f)
        {
            Weather.RainSpawnRate = 1000f;
        }
        if (Weather.RainMaxParticles < 0)
        {
            Weather.RainMaxParticles = 0;
        }
        else if (Weather.RainMaxParticles > 4096)
        {
            Weather.RainMaxParticles = 4096;
        }
        if (Weather.RainParticleSpeed < 0f)
        {
            Weather.RainParticleSpeed = 0f;
        }
        else if (Weather.RainParticleSpeed > 256f)
        {
            Weather.RainParticleSpeed = 256f;
        }
        if (Weather.RainParticleLength < 0.1f)
        {
            Weather.RainParticleLength = 0.1f;
        }
        else if (Weather.RainParticleLength > 256f)
        {
            Weather.RainParticleLength = 256f;
        }
        if (Weather.RainParticleWidth < 0.1f)
        {
            Weather.RainParticleWidth = 0.1f;
        }
        else if (Weather.RainParticleWidth > 16f)
        {
            Weather.RainParticleWidth = 16f;
        }
        Weather.RainStreakAlpha = Math.Clamp(Weather.RainStreakAlpha, 0f, 1f);
        Weather.LightningChancePerSecond = Math.Clamp(Weather.LightningChancePerSecond, 0f, 1f);
        Weather.LightningMinRainIntensity = Math.Clamp(Weather.LightningMinRainIntensity, 0f, 1f);
        if (Weather.LightningFlashFadeSeconds < 0.05f)
        {
            Weather.LightningFlashFadeSeconds = 0.05f;
        }
        else if (Weather.LightningFlashFadeSeconds > 5f)
        {
            Weather.LightningFlashFadeSeconds = 5f;
        }
        Weather.LightningFlashStrength = Math.Clamp(Weather.LightningFlashStrength, 0f, 1f);
        if (Weather.LightningThunderDelaySeconds < 0.05f)
        {
            Weather.LightningThunderDelaySeconds = 0.05f;
        }
        else if (Weather.LightningThunderDelaySeconds > 10f)
        {
            Weather.LightningThunderDelaySeconds = 10f;
        }
        if (Weather.LightningCooldownSeconds < 1f)
        {
            Weather.LightningCooldownSeconds = 1f;
        }
        else if (Weather.LightningCooldownSeconds > 120f)
        {
            Weather.LightningCooldownSeconds = 120f;
        }

        if (Audio.MaxActive < 1)
        {
            Audio.MaxActive = 1;
        }
        else if (Audio.MaxActive > 64)
        {
            Audio.MaxActive = 64;
        }

        if (Audio.SpatialInnerRadius < 0.5f)
        {
            Audio.SpatialInnerRadius = 0.5f;
        }
        else if (Audio.SpatialInnerRadius > 128f)
        {
            Audio.SpatialInnerRadius = 128f;
        }

        if (Audio.SpatialOuterRadius < Audio.SpatialInnerRadius + 1f)
        {
            Audio.SpatialOuterRadius = Audio.SpatialInnerRadius + 1f;
        }
        else if (Audio.SpatialOuterRadius > 256f)
        {
            Audio.SpatialOuterRadius = 256f;
        }

        Audio.SpatialPanStrength = Math.Clamp(Audio.SpatialPanStrength, 0f, 1f);

        Audio.DigVolume = Math.Clamp(Audio.DigVolume, 0f, 1f);
        Audio.PlaceVolume = Math.Clamp(Audio.PlaceVolume, 0f, 1f);
        Audio.AmbientVolume = Math.Clamp(Audio.AmbientVolume, 0f, 1f);
        Audio.MusicVolume = Math.Clamp(Audio.MusicVolume, 0f, 1f);
        Audio.WeatherVolume = Math.Clamp(Audio.WeatherVolume, 0f, 1f);
        Audio.LiquidVolume = Math.Clamp(Audio.LiquidVolume, 0f, 1f);
        Audio.FireVolume = Math.Clamp(Audio.FireVolume, 0f, 1f);
        if (Audio.SwimStepDistance < 0.1f)
        {
            Audio.SwimStepDistance = 0.1f;
        }
        Audio.StepVolume = Math.Clamp(Audio.StepVolume, 0f, 1f);
        Audio.RunVolume = Math.Clamp(Audio.RunVolume, 0f, 1f);
        Audio.JumpVolume = Math.Clamp(Audio.JumpVolume, 0f, 1f);
        Audio.MobVolume = Math.Clamp(Audio.MobVolume, 0f, 1f);
        Audio.MobStepVolume = Math.Clamp(Audio.MobStepVolume, 0f, 1f);
        Audio.FuseVolume = Math.Clamp(Audio.FuseVolume, 0f, 1f);
        Audio.ExplosionVolume = Math.Clamp(Audio.ExplosionVolume, 0f, 1f);

        if (Audio.StepDistance < 0.2f)
        {
            Audio.StepDistance = 0.2f;
        }

        if (Audio.RunStepDistance < 0.2f)
        {
            Audio.RunStepDistance = 0.2f;
        }

        if (Audio.RunStepDistance > Audio.StepDistance)
        {
            Audio.RunStepDistance = Audio.StepDistance;
        }

        if (Audio.AmbientIntervalSeconds < 5f) Audio.AmbientIntervalSeconds = 5f;
        if (Audio.MusicIntervalSeconds < 10f) Audio.MusicIntervalSeconds = 10f;
        if (Audio.WeatherIntervalSeconds < 1f) Audio.WeatherIntervalSeconds = 1f;
        if (Audio.LiquidIntervalSeconds < 1f) Audio.LiquidIntervalSeconds = 1f;
        if (Audio.LiquidRadius < 1f) Audio.LiquidRadius = 1f;

        if (SaveSettings.AutoSaveIntervalSeconds < 5f)
        {
            SaveSettings.AutoSaveIntervalSeconds = 5f;
        }

        if (SaveSettings.SaveWorkers < 1)
        {
            SaveSettings.SaveWorkers = 1;
        }

        if (SaveSettings.LoadWorkers < 1)
        {
            SaveSettings.LoadWorkers = 1;
        }

        if (SaveSettings.UnloadExtraRadius < 0)
        {
            SaveSettings.UnloadExtraRadius = 0;
        }

        if (SaveSettings.MaxBlockChangesPerFrame < 1)
        {
            SaveSettings.MaxBlockChangesPerFrame = 1;
        }
        else if (SaveSettings.MaxBlockChangesPerFrame > 65536)
        {
            SaveSettings.MaxBlockChangesPerFrame = 65536;
        }

        if (Physics.PlayerMaxHealth < 1)
        {
            Physics.PlayerMaxHealth = 1;
        }

        if (Physics.HurtCooldownSeconds < 0f)
        {
            Physics.HurtCooldownSeconds = 0f;
        }

        if (Falling.MaxUpdatesPerFrame < 1)
        {
            Falling.MaxUpdatesPerFrame = 1;
        }

        if (Fluid.MaxUpdatesPerFrame < 1)
        {
            Fluid.MaxUpdatesPerFrame = 1;
        }

        if (Fluid.WaterMaxSpreadLevel < 1)
        {
            Fluid.WaterMaxSpreadLevel = 1;
        }
        else if (Fluid.WaterMaxSpreadLevel > FluidConfig.AbsoluteMaxLevel)
        {
            Fluid.WaterMaxSpreadLevel = FluidConfig.AbsoluteMaxLevel;
        }

        if (Fluid.LavaMaxSpreadLevel < 1)
        {
            Fluid.LavaMaxSpreadLevel = 1;
        }
        else if (Fluid.LavaMaxSpreadLevel > Fluid.WaterMaxSpreadLevel)
        {
            Fluid.LavaMaxSpreadLevel = Fluid.WaterMaxSpreadLevel;
        }

        if (Fluid.LavaUpdatesPerFrame < 1)
        {
            Fluid.LavaUpdatesPerFrame = 1;
        }

        if (Fluid.LavaUpdateIntervalFrames < 1)
        {
            Fluid.LavaUpdateIntervalFrames = 1;
        }

        if (Fire.MaxUpdatesPerFrame < 1)
        {
            Fire.MaxUpdatesPerFrame = 1;
        }
        else if (Fire.MaxUpdatesPerFrame > MaxFireUpdatesPerFrame)
        {
            Fire.MaxUpdatesPerFrame = MaxFireUpdatesPerFrame;
        }

        if (Fire.MaxEventQueue < 1)
        {
            Fire.MaxEventQueue = 1;
        }
        else if (Fire.MaxEventQueue > MaxFireEventQueue)
        {
            Fire.MaxEventQueue = MaxFireEventQueue;
        }

        if (Fire.MaxExplosionIgnitedBlocks < 1)
        {
            Fire.MaxExplosionIgnitedBlocks = 1;
        }
        else if (Fire.MaxExplosionIgnitedBlocks > MaxFireExplosionIgnitedBlocks)
        {
            Fire.MaxExplosionIgnitedBlocks = MaxFireExplosionIgnitedBlocks;
        }

        Fire.MaxAgeSeconds = Math.Clamp(Fire.MaxAgeSeconds, 0.5f, 60f);
        Fire.SpreadIntervalSeconds = Math.Clamp(Fire.SpreadIntervalSeconds, 0.05f, 5f);
        Fire.SpreadChance = Math.Clamp(Fire.SpreadChance, 0f, 1f);
        Fire.BurnStartSeconds = Math.Clamp(Fire.BurnStartSeconds, 0f, Fire.MaxAgeSeconds);
        Fire.BurnChance = Math.Clamp(Fire.BurnChance, 0f, 1f);
        Fire.ExplosionIgniteRadius = Math.Clamp(Fire.ExplosionIgniteRadius, 1f, 16f);
        Fire.ExplosionIgniteChance = Math.Clamp(Fire.ExplosionIgniteChance, 0f, 1f);
        Fire.RainExtinguishMultiplier = Math.Clamp(Fire.RainExtinguishMultiplier, 0f, 10f);

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

        if (WorldGen.RiverThreshold < 0f)
        {
            WorldGen.RiverThreshold = 0f;
        }
        else if (WorldGen.RiverThreshold > 1f)
        {
            WorldGen.RiverThreshold = 1f;
        }

        if (WorldGen.RiverWidth < 0.5f)
        {
            WorldGen.RiverWidth = 0.5f;
        }

        if (WorldGen.RiverDepth < 1)
        {
            WorldGen.RiverDepth = 1;
        }

        if (WorldGen.RiverScale <= 0f)
        {
            WorldGen.RiverScale = 1f;
        }

        if (WorldGen.RiverWarpStrength < 0f)
        {
            WorldGen.RiverWarpStrength = 0f;
        }

        if (WorldGen.RiverInfluenceWidth <= 0.5f)
        {
            WorldGen.RiverInfluenceWidth = 0.5f;
        }

        WorldGen.RiverBankInfluenceMin = Math.Clamp(WorldGen.RiverBankInfluenceMin, 0f, 1f);
        WorldGen.RiverWaterInfluenceMin = Math.Clamp(WorldGen.RiverWaterInfluenceMin, 0f, 1f);

        if (WorldGen.RiverWaterInfluenceMin < WorldGen.RiverBankInfluenceMin)
        {
            WorldGen.RiverWaterInfluenceMin = WorldGen.RiverBankInfluenceMin;
        }

        if (WorldGen.BiomeScale <= 0f)
        {
            WorldGen.BiomeScale = 1f;
        }

        if (WorldGen.BiomeWarpStrength < 0f)
        {
            WorldGen.BiomeWarpStrength = 0f;
        }

        if (WorldGen.BiomeContrast < 0.5f)
        {
            WorldGen.BiomeContrast = 0.5f;
        }
        else if (WorldGen.BiomeContrast > 2.0f)
        {
            WorldGen.BiomeContrast = 2.0f;
        }

        if (WorldGen.MountainBiomeThreshold < 0.5f)
        {
            WorldGen.MountainBiomeThreshold = 0.5f;
        }
        else if (WorldGen.MountainBiomeThreshold > 0.95f)
        {
            WorldGen.MountainBiomeThreshold = 0.95f;
        }

        if (WorldGen.WaterSlopeThreshold < 0f)
        {
            WorldGen.WaterSlopeThreshold = 0f;
        }

        if (WorldGen.WaterMinDepth < 0)
        {
            WorldGen.WaterMinDepth = 0;
        }

        if (WorldGen.AquiferWaterLevelOffset < 0)
        {
            WorldGen.AquiferWaterLevelOffset = 0;
        }

        if (WorldGen.AquiferLavaLevelOffset < WorldGen.AquiferWaterLevelOffset + 1)
        {
            WorldGen.AquiferLavaLevelOffset = WorldGen.AquiferWaterLevelOffset + 1;
        }

        if (WorldGen.AquiferWaterScale <= 0f)
        {
            WorldGen.AquiferWaterScale = 1f;
        }

        if (WorldGen.AquiferLavaScale <= 0f)
        {
            WorldGen.AquiferLavaScale = 1f;
        }

        WorldGen.AquiferWaterThreshold = Math.Clamp(WorldGen.AquiferWaterThreshold, 0f, 1f);
        WorldGen.AquiferLavaThreshold = Math.Clamp(WorldGen.AquiferLavaThreshold, 0f, 1f);
        if (WorldGen.AquiferLavaThreshold < WorldGen.AquiferWaterThreshold)
        {
            WorldGen.AquiferLavaThreshold = WorldGen.AquiferWaterThreshold;
        }

        if (Particles.MaxParticles < 16)
        {
            Particles.MaxParticles = 16;
        }
        else if (Particles.MaxParticles > MaxParticleCount)
        {
            Particles.MaxParticles = MaxParticleCount;
        }

        if (Particles.Gravity > 0f)
        {
            Particles.Gravity = 0f;
        }
        else if (Particles.Gravity < -50f)
        {
            Particles.Gravity = -50f;
        }

        Particles.Drag = Math.Clamp(Particles.Drag, 0f, 1f);

        if (Particles.BlockBreakCount < 1)
        {
            Particles.BlockBreakCount = 1;
        }

        if (Particles.BlockPlaceCount < 1)
        {
            Particles.BlockPlaceCount = 1;
        }

        if (Particles.StepCount < 1)
        {
            Particles.StepCount = 1;
        }

        if (Particles.JumpCount < 1)
        {
            Particles.JumpCount = 1;
        }

        if (Particles.BlockBreakLifetime < 0.05f)
        {
            Particles.BlockBreakLifetime = 0.05f;
        }

        if (Particles.BlockPlaceLifetime < 0.05f)
        {
            Particles.BlockPlaceLifetime = 0.05f;
        }

        if (Particles.StepLifetime < 0.05f)
        {
            Particles.StepLifetime = 0.05f;
        }

        if (Particles.JumpLifetime < 0.05f)
        {
            Particles.JumpLifetime = 0.05f;
        }

        if (Particles.BlockBreakSize < 0.01f)
        {
            Particles.BlockBreakSize = 0.01f;
        }

        if (Particles.BlockPlaceSize < 0.01f)
        {
            Particles.BlockPlaceSize = 0.01f;
        }

        if (Particles.StepSize < 0.01f)
        {
            Particles.StepSize = 0.01f;
        }

        if (Particles.JumpSize < 0.01f)
        {
            Particles.JumpSize = 0.01f;
        }

        if (Particles.BlockBreakSpeed < 0.01f)
        {
            Particles.BlockBreakSpeed = 0.01f;
        }

        if (Particles.BlockPlaceSpeed < 0.01f)
        {
            Particles.BlockPlaceSpeed = 0.01f;
        }

        if (Particles.StepSpeed < 0.01f)
        {
            Particles.StepSpeed = 0.01f;
        }

        if (Particles.JumpSpeed < 0.01f)
        {
            Particles.JumpSpeed = 0.01f;
        }

        Particles.StepUpwardBias = Math.Clamp(Particles.StepUpwardBias, 0f, 1f);
        Particles.JumpUpwardBias = Math.Clamp(Particles.JumpUpwardBias, 0f, 1f);
        if (Particles.MobHurtCount < 1)
        {
            Particles.MobHurtCount = 1;
        }

        if (Particles.MobDeathCount < 1)
        {
            Particles.MobDeathCount = 1;
        }

        if (Particles.MobHurtLifetime < 0.05f)
        {
            Particles.MobHurtLifetime = 0.05f;
        }

        if (Particles.MobDeathLifetime < 0.05f)
        {
            Particles.MobDeathLifetime = 0.05f;
        }

        if (Particles.MobHurtSize < 0.01f)
        {
            Particles.MobHurtSize = 0.01f;
        }

        if (Particles.MobDeathSize < 0.01f)
        {
            Particles.MobDeathSize = 0.01f;
        }

        if (Particles.MobHurtSpeed < 0.01f)
        {
            Particles.MobHurtSpeed = 0.01f;
        }

        if (Particles.MobDeathSpeed < 0.01f)
        {
            Particles.MobDeathSpeed = 0.01f;
        }

        if (Particles.MobHurtSpread < 0.01f)
        {
            Particles.MobHurtSpread = 0.01f;
        }

        if (Particles.MobDeathSpread < 0.01f)
        {
            Particles.MobDeathSpread = 0.01f;
        }

        Particles.MobHurtUpwardBias = Math.Clamp(Particles.MobHurtUpwardBias, 0f, 1f);
        Particles.MobDeathUpwardBias = Math.Clamp(Particles.MobDeathUpwardBias, 0f, 1f);
        Particles.MobHurtMotionInfluence = Math.Clamp(Particles.MobHurtMotionInfluence, 0f, 1f);
        Particles.MobDeathMotionInfluence = Math.Clamp(Particles.MobDeathMotionInfluence, 0f, 1f);
        if (Particles.MobAttackCount < 1)
        {
            Particles.MobAttackCount = 1;
        }

        if (Particles.MobAttackLifetime < 0.05f)
        {
            Particles.MobAttackLifetime = 0.05f;
        }

        if (Particles.MobAttackSize < 0.01f)
        {
            Particles.MobAttackSize = 0.01f;
        }

        if (Particles.MobAttackSpeed < 0.01f)
        {
            Particles.MobAttackSpeed = 0.01f;
        }

        if (Particles.MobAttackSpread < 0.01f)
        {
            Particles.MobAttackSpread = 0.01f;
        }

        Particles.MobAttackUpwardBias = Math.Clamp(Particles.MobAttackUpwardBias, 0f, 1f);
        Particles.MobAttackMotionInfluence = Math.Clamp(Particles.MobAttackMotionInfluence, 0f, 1f);
        Particles.EliteMobParticleMultiplier = Math.Clamp(Particles.EliteMobParticleMultiplier, 0.5f, 4f);

        if (WorldGen.ForestHeightScale <= 0f)
        {
            WorldGen.ForestHeightScale = 1f;
        }

        if (WorldGen.PlainsHeightScale <= 0f)
        {
            WorldGen.PlainsHeightScale = 1f;
        }

        if (WorldGen.DesertHeightScale <= 0f)
        {
            WorldGen.DesertHeightScale = 1f;
        }

        if (WorldGen.MountainsHeightScale <= 0f)
        {
            WorldGen.MountainsHeightScale = 1f;
        }

        if (WorldGen.TaigaHeightScale <= 0f)
        {
            WorldGen.TaigaHeightScale = 1f;
        }

        if (WorldGen.TundraHeightScale <= 0f)
        {
            WorldGen.TundraHeightScale = 1f;
        }

        if (WorldGen.SwampHeightScale <= 0f)
        {
            WorldGen.SwampHeightScale = 1f;
        }

        if (WorldGen.SavannaHeightScale <= 0f)
        {
            WorldGen.SavannaHeightScale = 1f;
        }

        if (WorldGen.ShrublandHeightScale <= 0f)
        {
            WorldGen.ShrublandHeightScale = 1f;
        }

        if (WorldGen.MountainStoneHeightOffset < 0)
        {
            WorldGen.MountainStoneHeightOffset = 0;
        }

        if (WorldGen.MountainStoneSlope < 1f)
        {
            WorldGen.MountainStoneSlope = 1f;
        }

        if (WorldGen.SnowLine < 0)
        {
            WorldGen.SnowLine = 0;
        }

        if (WorldGen.DetailScale <= 0f)
        {
            WorldGen.DetailScale = 1f;
        }

        if (WorldGen.DesertDuneScale <= 0f)
        {
            WorldGen.DesertDuneScale = 1f;
        }

        if (WorldGen.DesertDuneAmplitude < 0f)
        {
            WorldGen.DesertDuneAmplitude = 0f;
        }

        if (WorldGen.TrailScale <= 0f)
        {
            WorldGen.TrailScale = 1f;
        }

        WorldGen.TrailThreshold = Math.Clamp(WorldGen.TrailThreshold, 0f, 1f);
        WorldGen.TrailDirtChance = Math.Clamp(WorldGen.TrailDirtChance, 0f, 1f);
        WorldGen.TrailClearChance = Math.Clamp(WorldGen.TrailClearChance, 0f, 1f);

        if (WorldGen.TrailSecondaryScale <= 0f)
        {
            WorldGen.TrailSecondaryScale = 1f;
        }

        WorldGen.TrailSecondaryThreshold = Math.Clamp(WorldGen.TrailSecondaryThreshold, 0f, 1f);

        if (WorldGen.TreeClusterScale <= 0f)
        {
            WorldGen.TreeClusterScale = 1f;
        }

        WorldGen.TreeClusterStrength = Math.Clamp(WorldGen.TreeClusterStrength, 0f, 1f);

        WorldGen.ForestTreeChance = Math.Clamp(WorldGen.ForestTreeChance, 0f, 1f);
        WorldGen.PlainsTreeChance = Math.Clamp(WorldGen.PlainsTreeChance, 0f, 1f);
        WorldGen.TaigaTreeChance = Math.Clamp(WorldGen.TaigaTreeChance, 0f, 1f);
        WorldGen.SavannaTreeChance = Math.Clamp(WorldGen.SavannaTreeChance, 0f, 1f);
        WorldGen.ShrublandTreeChance = Math.Clamp(WorldGen.ShrublandTreeChance, 0f, 1f);
        WorldGen.SwampTreeChance = Math.Clamp(WorldGen.SwampTreeChance, 0f, 1f);
        WorldGen.MountainsTreeChance = Math.Clamp(WorldGen.MountainsTreeChance, 0f, 1f);
        WorldGen.PlainsTallGrassChance = Math.Clamp(WorldGen.PlainsTallGrassChance, 0f, 1f);
        WorldGen.SavannaTallGrassChance = Math.Clamp(WorldGen.SavannaTallGrassChance, 0f, 1f);
        WorldGen.ShrublandTallGrassChance = Math.Clamp(WorldGen.ShrublandTallGrassChance, 0f, 1f);
        WorldGen.ForestFlowerChance = Math.Clamp(WorldGen.ForestFlowerChance, 0f, 1f);
        WorldGen.SwampFlowerChance = Math.Clamp(WorldGen.SwampFlowerChance, 0f, 1f);
        WorldGen.DesertCactusChance = Math.Clamp(WorldGen.DesertCactusChance, 0f, 1f);
        WorldGen.DesertDeadBushChance = Math.Clamp(WorldGen.DesertDeadBushChance, 0f, 1f);
        WorldGen.SugarCaneChance = Math.Clamp(WorldGen.SugarCaneChance, 0f, 1f);

        if (WorldGen.TreeMaxSlope < 1f)
        {
            WorldGen.TreeMaxSlope = 1f;
        }

        WorldGen.PondChance = Math.Clamp(WorldGen.PondChance, 0f, 1f);
        if (WorldGen.PondRadiusMin < 1) WorldGen.PondRadiusMin = 1;
        if (WorldGen.PondRadiusMax < WorldGen.PondRadiusMin) WorldGen.PondRadiusMax = WorldGen.PondRadiusMin;
        if (WorldGen.PondDepth < 1) WorldGen.PondDepth = 1;

        WorldGen.BoulderChance = Math.Clamp(WorldGen.BoulderChance, 0f, 1f);
        if (WorldGen.BoulderRadiusMin < 1) WorldGen.BoulderRadiusMin = 1;
        if (WorldGen.BoulderRadiusMax < WorldGen.BoulderRadiusMin) WorldGen.BoulderRadiusMax = WorldGen.BoulderRadiusMin;
        WorldGen.BoulderStoneChance = Math.Clamp(WorldGen.BoulderStoneChance, 0f, 1f);

        WorldGen.FallenLogChance = Math.Clamp(WorldGen.FallenLogChance, 0f, 1f);
        if (WorldGen.FallenLogMinLength < 1) WorldGen.FallenLogMinLength = 1;
        if (WorldGen.FallenLogMaxLength < WorldGen.FallenLogMinLength) WorldGen.FallenLogMaxLength = WorldGen.FallenLogMinLength;

        WorldGen.FlowerPatchChance = Math.Clamp(WorldGen.FlowerPatchChance, 0f, 1f);
        if (WorldGen.FlowerPatchRadiusMin < 1) WorldGen.FlowerPatchRadiusMin = 1;
        if (WorldGen.FlowerPatchRadiusMax < WorldGen.FlowerPatchRadiusMin) WorldGen.FlowerPatchRadiusMax = WorldGen.FlowerPatchRadiusMin;

        WorldGen.GravelPatchChance = Math.Clamp(WorldGen.GravelPatchChance, 0f, 1f);
        if (WorldGen.GravelPatchRadiusMin < 1) WorldGen.GravelPatchRadiusMin = 1;
        if (WorldGen.GravelPatchRadiusMax < WorldGen.GravelPatchRadiusMin) WorldGen.GravelPatchRadiusMax = WorldGen.GravelPatchRadiusMin;

        if (Render.Lod.EndDistance < Render.Lod.StartDistance + Render.Lod.Step)
        {
            Render.Lod.EndDistance = Render.Lod.StartDistance + Render.Lod.Step;
        }

        if (Ui.HudScale <= 0f)
        {
            Ui.HudScale = 1f;
        }

        if (Ui.InventoryScale <= 0f)
        {
            Ui.InventoryScale = 1f;
        }

        if (Ui.InventorySlotSize < 8f)
        {
            Ui.InventorySlotSize = 8f;
        }

        if (Ui.InventorySlotPadding < 0f)
        {
            Ui.InventorySlotPadding = 0f;
        }

        if (Ui.InventoryPanelPadding < 0f)
        {
            Ui.InventoryPanelPadding = 0f;
        }

        if (Ui.InventoryColumns < 1)
        {
            Ui.InventoryColumns = 1;
        }

        if (Ui.InventoryRows < 1)
        {
            Ui.InventoryRows = 1;
        }

        if (Ui.ReferenceHeight <= 0f)
        {
            Ui.ReferenceHeight = 1080f;
        }

        if (Ui.ItemNameYOffset < 0f)
        {
            Ui.ItemNameYOffset = 0f;
        }

        if (Ui.ItemNameDisplaySeconds < 0f)
        {
            Ui.ItemNameDisplaySeconds = 0f;
        }

        if (Ui.ItemIconScale <= 0f)
        {
            Ui.ItemIconScale = 1f;
        }

        if (FirstPerson.HandWidth <= 0f)
        {
            FirstPerson.HandWidth = 0.01f;
        }

        if (FirstPerson.HandHeight <= 0f)
        {
            FirstPerson.HandHeight = 0.01f;
        }

        if (FirstPerson.HandDepth <= 0f)
        {
            FirstPerson.HandDepth = 0.01f;
        }

        if (FirstPerson.HandMotionScale <= 0f)
        {
            FirstPerson.HandMotionScale = 0.01f;
        }

        if (FirstPerson.HandSwingScale <= 0f)
        {
            FirstPerson.HandSwingScale = 0.01f;
        }

        if (FirstPerson.ItemScale <= 0f)
        {
            FirstPerson.ItemScale = 0.01f;
        }

        if (FirstPerson.TransparentScale <= 0f)
        {
            FirstPerson.TransparentScale = 0.01f;
        }

        if (FirstPerson.ItemMotionScale <= 0f)
        {
            FirstPerson.ItemMotionScale = 0.01f;
        }

        if (FirstPerson.ItemSwingScale <= 0f)
        {
            FirstPerson.ItemSwingScale = 0.01f;
        }

        if (FirstPerson.SwingCurvePower <= 0f)
        {
            FirstPerson.SwingCurvePower = 0.01f;
        }

        if (FirstPerson.SwingDurationSeconds <= 0f)
        {
            FirstPerson.SwingDurationSeconds = 0.01f;
        }

        if (FirstPerson.MovementBobAmplitude < 0f)
        {
            FirstPerson.MovementBobAmplitude = 0f;
        }

        if (FirstPerson.MovementBobSpeed < 0f)
        {
            FirstPerson.MovementBobSpeed = 0f;
        }

        if (FirstPerson.IdleBobAmplitude < 0f)
        {
            FirstPerson.IdleBobAmplitude = 0f;
        }

        if (FirstPerson.IdleBobSpeed < 0f)
        {
            FirstPerson.IdleBobSpeed = 0f;
        }

        if (FirstPerson.MotionRotationDegrees < 0f)
        {
            FirstPerson.MotionRotationDegrees = 0f;
        }

        if (FirstPerson.CrossScale <= 0f)
        {
            FirstPerson.CrossScale = 0.01f;
        }

        if (FirstPerson.TorchScale <= 0f)
        {
            FirstPerson.TorchScale = 0.01f;
        }

        if (FirstPerson.CardThickness <= 0f)
        {
            FirstPerson.CardThickness = 0.01f;
        }

        if (CameraMotion.BobAmplitude < 0f)
        {
            CameraMotion.BobAmplitude = 0f;
        }

        if (CameraMotion.BobSpeed < 0f)
        {
            CameraMotion.BobSpeed = 0f;
        }

        if (CameraMotion.BobLateralFactor < 0f)
        {
            CameraMotion.BobLateralFactor = 0f;
        }

        if (CameraMotion.BobForwardFactor < 0f)
        {
            CameraMotion.BobForwardFactor = 0f;
        }

        if (CameraMotion.InertiaStrength < 0f)
        {
            CameraMotion.InertiaStrength = 0f;
        }

        CameraMotion.AirborneMultiplier = Math.Clamp(CameraMotion.AirborneMultiplier, 0f, 1f);
        CameraMotion.LiquidMultiplier = Math.Clamp(CameraMotion.LiquidMultiplier, 0f, 1f);

        if (string.IsNullOrWhiteSpace(Ui.FontFile))
        {
            Ui.FontFile = "minecraft/font/NotoSans-Regular.ttf";
        }

        if (Herobrine.MinManifestIntervalSeconds < 5f)
        {
            Herobrine.MinManifestIntervalSeconds = 5f;
        }

        if (Herobrine.MaxManifestIntervalSeconds < Herobrine.MinManifestIntervalSeconds + 1f)
        {
            Herobrine.MaxManifestIntervalSeconds = Herobrine.MinManifestIntervalSeconds + 1f;
        }

        if (Herobrine.ManifestDurationSeconds < 0.5f)
        {
            Herobrine.ManifestDurationSeconds = 0.5f;
        }

        if (Herobrine.EventCooldownSeconds < 0f)
        {
            Herobrine.EventCooldownSeconds = 0f;
        }

        if (Herobrine.WorldEffectCooldownSeconds < 0f)
        {
            Herobrine.WorldEffectCooldownSeconds = 0f;
        }

        if (Herobrine.MinManifestDistance < 8f)
        {
            Herobrine.MinManifestDistance = 8f;
        }

        if (Herobrine.MaxManifestDistance < Herobrine.MinManifestDistance + 4f)
        {
            Herobrine.MaxManifestDistance = Herobrine.MinManifestDistance + 4f;
        }

        Herobrine.BehindPlayerChance = Math.Clamp(Herobrine.BehindPlayerChance, 0f, 1f);
        Herobrine.NightBias = Math.Clamp(Herobrine.NightBias, 0f, 4f);
        Herobrine.CaveBias = Math.Clamp(Herobrine.CaveBias, 0f, 4f);

        if (Herobrine.DirectLookDespawnSeconds < 0.1f)
        {
            Herobrine.DirectLookDespawnSeconds = 0.1f;
        }

        if (Herobrine.HiddenTimeoutSeconds < 0f)
        {
            Herobrine.HiddenTimeoutSeconds = 0f;
        }

        Herobrine.WorldEffectIntensity = Math.Clamp(Herobrine.WorldEffectIntensity, 0f, 1f);
        Herobrine.Mode = HerobrineModeCatalog.Normalize(Herobrine.Mode);

        if (Mob.MaxAlive < 0)
        {
            Mob.MaxAlive = 0;
        }

        if (Mob.MaxEventQueue < 1)
        {
            Mob.MaxEventQueue = 1;
        }
        else if (Mob.MaxEventQueue > 16384)
        {
            Mob.MaxEventQueue = 16384;
        }

        if (Mob.SpawnRadius < 16)
        {
            Mob.SpawnRadius = 16;
        }

        if (Mob.DespawnRadius < Mob.SpawnRadius + 16)
        {
            Mob.DespawnRadius = Mob.SpawnRadius + 16;
        }

        if (Mob.SpawnIntervalSeconds < 0.25f)
        {
            Mob.SpawnIntervalSeconds = 0.25f;
        }

        if (Mob.SpawnAttemptsPerTick < 1)
        {
            Mob.SpawnAttemptsPerTick = 1;
        }

        if (Mob.PlayerAttackDamage < 1)
        {
            Mob.PlayerAttackDamage = 1;
        }

        if (Mob.PlayerAttackCooldownSeconds < 0f)
        {
            Mob.PlayerAttackCooldownSeconds = 0f;
        }

        if (Mob.PlayerDamageCooldownSeconds < 0f)
        {
            Mob.PlayerDamageCooldownSeconds = 0f;
        }

        if (Mob.HostileDayMultiplier < 0f)
        {
            Mob.HostileDayMultiplier = 0f;
        }

        if (Mob.HostileNightMultiplier < 0f)
        {
            Mob.HostileNightMultiplier = 0f;
        }

        if (Mob.PassiveDayMultiplier < 0f)
        {
            Mob.PassiveDayMultiplier = 0f;
        }

        if (Mob.PassiveNightMultiplier < 0f)
        {
            Mob.PassiveNightMultiplier = 0f;
        }

        if (Mob.RainHostileMultiplier < 0f)
        {
            Mob.RainHostileMultiplier = 0f;
        }
        else if (Mob.RainHostileMultiplier > 4f)
        {
            Mob.RainHostileMultiplier = 4f;
        }

        if (Mob.RainPassiveMultiplier < 0f)
        {
            Mob.RainPassiveMultiplier = 0f;
        }
        else if (Mob.RainPassiveMultiplier > 4f)
        {
            Mob.RainPassiveMultiplier = 4f;
        }

        if (Mob.HostileSkyExposureMultiplier < 0f)
        {
            Mob.HostileSkyExposureMultiplier = 0f;
        }
        else if (Mob.HostileSkyExposureMultiplier > 4f)
        {
            Mob.HostileSkyExposureMultiplier = 4f;
        }

        if (Mob.HostileShelterMultiplier < 0f)
        {
            Mob.HostileShelterMultiplier = 0f;
        }
        else if (Mob.HostileShelterMultiplier > 4f)
        {
            Mob.HostileShelterMultiplier = 4f;
        }

        if (Mob.PassiveSkyExposureMultiplier < 0f)
        {
            Mob.PassiveSkyExposureMultiplier = 0f;
        }
        else if (Mob.PassiveSkyExposureMultiplier > 4f)
        {
            Mob.PassiveSkyExposureMultiplier = 4f;
        }

        if (Mob.PassiveShelterMultiplier < 0f)
        {
            Mob.PassiveShelterMultiplier = 0f;
        }
        else if (Mob.PassiveShelterMultiplier > 4f)
        {
            Mob.PassiveShelterMultiplier = 4f;
        }

        if (Mob.WaterSlowMultiplier < 0f)
        {
            Mob.WaterSlowMultiplier = 0f;
        }

        if (Mob.WaterBuoyancy < 0f)
        {
            Mob.WaterBuoyancy = 0f;
        }
        Mob.WaterCurrentMultiplier = Math.Clamp(Mob.WaterCurrentMultiplier, 0f, 5f);

        if (Mob.LavaDamagePerSecond < 0f)
        {
            Mob.LavaDamagePerSecond = 0f;
        }

        if (Mob.EdgeAvoidDistance < 0f)
        {
            Mob.EdgeAvoidDistance = 0f;
        }

        if (Mob.StepHeight < 0f)
        {
            Mob.StepHeight = 0f;
        }

        if (Mob.JumpVelocity < 0f)
        {
            Mob.JumpVelocity = 0f;
        }

        if (Mob.WanderChangeSeconds < 0.25f)
        {
            Mob.WanderChangeSeconds = 0.25f;
        }

        if (Mob.HostilePursuitSeconds < 0f)
        {
            Mob.HostilePursuitSeconds = 0f;
        }

        if (Mob.HostilePursuitSpeedMultiplier < 0f)
        {
            Mob.HostilePursuitSpeedMultiplier = 0f;
        }
        else if (Mob.HostilePursuitSpeedMultiplier > 4f)
        {
            Mob.HostilePursuitSpeedMultiplier = 4f;
        }

        Mob.ZombieWeight = Math.Max(0f, Mob.ZombieWeight);
        Mob.CreeperWeight = Math.Max(0f, Mob.CreeperWeight);
        Mob.CowWeight = Math.Max(0f, Mob.CowWeight);
        Mob.SheepWeight = Math.Max(0f, Mob.SheepWeight);
        Mob.ChickenWeight = Math.Max(0f, Mob.ChickenWeight);
        Mob.EliteSpawnChance = Math.Clamp(Mob.EliteSpawnChance, 0f, 1f);
        Mob.EliteVariantChance = Math.Clamp(Mob.EliteVariantChance, 0f, 1f);
        if (Mob.EliteHealthMultiplier < 1f)
        {
            Mob.EliteHealthMultiplier = 1f;
        }

        if (Mob.EliteDamageMultiplier < 1f)
        {
            Mob.EliteDamageMultiplier = 1f;
        }

        if (Mob.EliteSpeedMultiplier < 1f)
        {
            Mob.EliteSpeedMultiplier = 1f;
        }

        if (Mob.ElitePursuitMultiplier < 1f)
        {
            Mob.ElitePursuitMultiplier = 1f;
        }

        if (Mob.EliteDropMultiplier < 1f)
        {
            Mob.EliteDropMultiplier = 1f;
        }

        if (Mob.StaggerSeconds < 0.01f)
        {
            Mob.StaggerSeconds = 0.01f;
        }

        Mob.StaggerSpeedMultiplier = Math.Clamp(Mob.StaggerSpeedMultiplier, 0.05f, 1f);

        if (Tnt.FuseSeconds < 0.1f)
        {
            Tnt.FuseSeconds = 0.1f;
        }

        if (Tnt.ChainReactionFuseSeconds < 0.05f)
        {
            Tnt.ChainReactionFuseSeconds = 0.05f;
        }

        if (Tnt.ExplosionRadius < 1f)
        {
            Tnt.ExplosionRadius = 1f;
        }

        if (Tnt.ExplosionDamage < 1)
        {
            Tnt.ExplosionDamage = 1;
        }

        if (Tnt.KnockbackStrength < 0f)
        {
            Tnt.KnockbackStrength = 0f;
        }

        if (Tnt.ResistanceScale < 0.01f)
        {
            Tnt.ResistanceScale = 0.01f;
        }

        if (Tnt.MaxAffectedBlocks < 1)
        {
            Tnt.MaxAffectedBlocks = 1;
        }

        if (Tnt.MaxPrimedTnt < 1)
        {
            Tnt.MaxPrimedTnt = 1;
        }
        else if (Tnt.MaxPrimedTnt > 16384)
        {
            Tnt.MaxPrimedTnt = 16384;
        }

        if (Tnt.MaxEventQueue < 1)
        {
            Tnt.MaxEventQueue = 1;
        }
        else if (Tnt.MaxEventQueue > 16384)
        {
            Tnt.MaxEventQueue = 16384;
        }
    }

    private static GameConfig CreateDefaultConfig(bool strictBetaMode)
    {
        return new GameConfig
        {
            StrictBetaMode = strictBetaMode
        };
    }
}
