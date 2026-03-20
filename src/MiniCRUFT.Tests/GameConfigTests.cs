using System;
using System.IO;
using MiniCRUFT.Core;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class GameConfigTests
{
    [Fact]
    public void LoadOrCreate_CreatesFileWithDefaultValues_WhenMissing()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_config_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            string path = Path.Combine(root, "config.json");

            var config = GameConfig.LoadOrCreate(path);

            Assert.True(File.Exists(path));
            Assert.Equal(8, config.ChunkRadius);
            Assert.True(config.VSync);
            Assert.Equal(100, config.WindowX);
            Assert.Equal(100, config.WindowY);
            Assert.Equal(1280, config.WindowWidth);
            Assert.Equal(720, config.WindowHeight);
            Assert.True(config.StrictBetaMode);
            Assert.NotNull(config.SaveSettings);
            Assert.NotNull(config.WorldGen);
            Assert.NotNull(config.Mob);
            Assert.NotNull(config.Tnt);
            Assert.NotNull(config.FirstPerson);
            Assert.NotNull(config.CameraMotion);
            Assert.NotNull(config.Herobrine);
            Assert.NotNull(config.Tools);
            Assert.Equal(1, config.Render.Anisotropy);
            Assert.False(config.Render.UseMipmaps);
            Assert.Equal(0.2f, config.Render.Foliage.CutoutAlphaThreshold);
            Assert.Equal(0f, config.Render.Foliage.DitherStrength);
            Assert.Equal(WorldGenConfig.DefaultChunkGenerationWorkers, config.WorldGen.ChunkGenerationWorkers);
            Assert.Equal(RenderConfig.DefaultChunkMeshWorkers, config.Render.ChunkMeshWorkers);
            Assert.True(config.CameraMotion.Enabled);
            Assert.Equal(0.03f, config.CameraMotion.BobAmplitude);
            Assert.Equal(7f, config.CameraMotion.BobSpeed);
            Assert.Equal(0.55f, config.CameraMotion.BobLateralFactor);
            Assert.Equal(0.25f, config.CameraMotion.BobForwardFactor);
            Assert.Equal(14f, config.CameraMotion.InertiaStrength);
            Assert.Equal(0.2f, config.CameraMotion.AirborneMultiplier);
            Assert.Equal(0.35f, config.CameraMotion.LiquidMultiplier);
            Assert.Equal(1.1f, config.DayNight.SunIntensityCurve);
            Assert.Equal(8, config.DayNight.MoonPhaseCycleDays);
            Assert.Equal(1.35f, config.Mob.RainHostileMultiplier);
            Assert.Equal(0.7f, config.Mob.RainPassiveMultiplier);
            Assert.Equal(0.35f, config.Mob.HostileSkyExposureMultiplier);
            Assert.Equal(1.15f, config.Mob.HostileShelterMultiplier);
            Assert.Equal(1.2f, config.Mob.PassiveSkyExposureMultiplier);
            Assert.Equal(0.55f, config.Mob.PassiveShelterMultiplier);
            Assert.Equal(4f, config.Mob.HostilePursuitSeconds);
            Assert.Equal(1.1f, config.Mob.HostilePursuitSpeedMultiplier);
            Assert.Equal(0.9f, config.Mob.WaterCurrentMultiplier);
            Assert.NotNull(config.Fire);
            Assert.True(config.Fire.Enabled);
            Assert.Equal(128, config.Fire.MaxUpdatesPerFrame);
            Assert.Equal(1024, config.Fire.MaxEventQueue);
            Assert.Equal(32, config.Fire.MaxExplosionIgnitedBlocks);
            Assert.Equal(6.8f, config.Fire.MaxAgeSeconds);
            Assert.Equal(0.25f, config.Fire.SpreadIntervalSeconds);
            Assert.Equal(0.5f, config.Fire.SpreadChance);
            Assert.Equal(0.9f, config.Fire.BurnStartSeconds);
            Assert.Equal(0.3f, config.Fire.BurnChance);
            Assert.Equal(5f, config.Fire.ExplosionIgniteRadius);
            Assert.Equal(0.55f, config.Fire.ExplosionIgniteChance);
            Assert.Equal(1.4f, config.Fire.RainExtinguishMultiplier);
            Assert.Equal(0.035f, config.Mob.EliteSpawnChance);
            Assert.Equal(0.2f, config.Mob.EliteVariantChance);
            Assert.Equal(1.8f, config.Mob.EliteHealthMultiplier);
            Assert.Equal(1.35f, config.Mob.EliteDamageMultiplier);
            Assert.Equal(1.12f, config.Mob.EliteSpeedMultiplier);
            Assert.Equal(1.4f, config.Mob.ElitePursuitMultiplier);
            Assert.Equal(1.5f, config.Mob.EliteDropMultiplier);
            Assert.Equal(0.18f, config.Mob.StaggerSeconds);
            Assert.Equal(0.3f, config.Mob.StaggerSpeedMultiplier);
            Assert.Equal(8, config.Particles.MobHurtCount);
            Assert.Equal(14, config.Particles.MobDeathCount);
            Assert.Equal(0.35f, config.Particles.MobHurtLifetime);
            Assert.Equal(0.55f, config.Particles.MobDeathLifetime);
            Assert.Equal(0.11f, config.Particles.MobHurtSize);
            Assert.Equal(0.14f, config.Particles.MobDeathSize);
            Assert.Equal(1.6f, config.Particles.MobHurtSpeed);
            Assert.Equal(2.2f, config.Particles.MobDeathSpeed);
            Assert.Equal(0.18f, config.Particles.MobHurtSpread);
            Assert.Equal(0.25f, config.Particles.MobDeathSpread);
            Assert.Equal(0.22f, config.Particles.MobHurtUpwardBias);
            Assert.Equal(0.3f, config.Particles.MobDeathUpwardBias);
            Assert.Equal(0.08f, config.Particles.MobHurtMotionInfluence);
            Assert.Equal(0.12f, config.Particles.MobDeathMotionInfluence);
            Assert.Equal(5, config.Particles.MobAttackCount);
            Assert.Equal(0.24f, config.Particles.MobAttackLifetime);
            Assert.Equal(0.1f, config.Particles.MobAttackSize);
            Assert.Equal(1.45f, config.Particles.MobAttackSpeed);
            Assert.Equal(0.16f, config.Particles.MobAttackSpread);
            Assert.Equal(0.18f, config.Particles.MobAttackUpwardBias);
            Assert.Equal(0.07f, config.Particles.MobAttackMotionInfluence);
            Assert.Equal(1.35f, config.Particles.EliteMobParticleMultiplier);
            Assert.Equal(0.45f, config.Physics.WaterMoveMultiplier);
            Assert.Equal(0.2f, config.Physics.WaterGravityMultiplier);
            Assert.Equal(3.4f, config.Physics.WaterBuoyancy);
            Assert.Equal(4.25f, config.Physics.WaterJumpVelocity);
            Assert.Equal(2.5f, config.Physics.WaterMaxFallSpeed);
            Assert.Equal(0.9f, config.Physics.WaterCurrentMultiplier);
            Assert.Equal(1.2f, config.Audio.SwimStepDistance);
            Assert.Equal(0.35f, config.Audio.WeatherVolume);
            Assert.Equal(0.45f, config.Audio.FireVolume);
            Assert.Equal(12f, config.Audio.WeatherIntervalSeconds);
            Assert.Equal(0.015f, config.Weather.LightningChancePerSecond);
            Assert.Equal(0.45f, config.Weather.LightningMinRainIntensity);
            Assert.Equal(0.16f, config.Weather.LightningFlashFadeSeconds);
            Assert.Equal(0.9f, config.Weather.LightningFlashStrength);
            Assert.Equal(1.25f, config.Weather.LightningThunderDelaySeconds);
            Assert.Equal(18f, config.Weather.LightningCooldownSeconds);
            Assert.True(config.WorldGen.StrictBetaMode);
            Assert.Null(config.WorldGen.ForcedBiome);
            Assert.Equal(0f, config.WorldGen.StructureChance);
            Assert.Equal(0f, config.WorldGen.CampChance);
            Assert.Equal(0f, config.WorldGen.WatchtowerChance);
            Assert.Equal(0f, config.WorldGen.BuriedCacheChance);
            Assert.Equal(0f, config.WorldGen.CaveCacheChance);
            Assert.Equal(0f, config.WorldGen.RuinChance);
            Assert.Equal(0f, config.WorldGen.MineShaftChance);
            Assert.Equal("minecraft/font/consolas.ttf", config.Ui.FontFile);
            Assert.Equal(12f, config.Ui.HotbarCountFontSize);
            Assert.Equal(0.85f, config.FirstPerson.HandMotionScale);
            Assert.Equal(0.9f, config.FirstPerson.HandSwingScale);
            Assert.Equal(0.55f, config.FirstPerson.ItemMotionScale);
            Assert.Equal(1.08f, config.FirstPerson.ItemSwingScale);
            Assert.Equal(1.75f, config.FirstPerson.SwingCurvePower);
            Assert.Equal(52f, config.Atmosphere.SunSize);
            Assert.Equal(40f, config.Atmosphere.MoonSize);
            Assert.Equal(0.84f, config.Atmosphere.FogNightMultiplier);
            Assert.Equal(0.88f, config.Atmosphere.FogRainMultiplier);
            Assert.False(config.Herobrine.Enabled);
            Assert.Equal(HerobrineModeCatalog.Classic, config.Herobrine.Mode);
            Assert.Equal(90f, config.Herobrine.MinManifestIntervalSeconds);
            Assert.Equal(240f, config.Herobrine.MaxManifestIntervalSeconds);
            Assert.Equal(0.35f, config.Herobrine.WorldEffectIntensity);
            Assert.Equal(64, config.Tools.WoodMaxDurability);
            Assert.Equal(128, config.Tools.StoneMaxDurability);
            Assert.Equal(256, config.Tools.IronMaxDurability);
            Assert.Equal(512, config.Tools.DiamondMaxDurability);
            Assert.Equal(1, config.Tools.ToolWearPerAction);
            Assert.Equal(0.5f, config.Tools.BlockBreakTimeScale);
            Assert.Equal(0.08f, config.Tools.MinBreakSeconds);
            Assert.Equal(1.6f, config.Tools.WrongToolBreakPenalty);
            Assert.Equal(1f, config.Tools.WoodMiningSpeedMultiplier);
            Assert.Equal(1.35f, config.Tools.StoneMiningSpeedMultiplier);
            Assert.Equal(1.8f, config.Tools.IronMiningSpeedMultiplier);
            Assert.Equal(2.6f, config.Tools.DiamondMiningSpeedMultiplier);
            Assert.Equal(16, config.Tools.WoodRepairDurability);
            Assert.Equal(32, config.Tools.StoneRepairDurability);
            Assert.Equal(64, config.Tools.IronRepairDurability);
            Assert.Equal(128, config.Tools.DiamondRepairDurability);
            Assert.Equal(1, config.SaveSettings.LoadWorkers);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTripsRepresentativeValues()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_config_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            string path = Path.Combine(root, "config.json");
            var config = new GameConfig
            {
                ChunkRadius = 12,
                Fullscreen = true,
                WindowX = 320,
                WindowY = 180,
                WindowWidth = 1600,
                WindowHeight = 900,
                Language = "en",
                FieldOfView = 90f,
                MouseSensitivity = 0.18f,
                PlayerSpeed = 7.5f,
                Physics =
                {
                    PlayerMaxHealth = 24,
                    HurtCooldownSeconds = 1.0f,
                    WaterMoveMultiplier = 0.5f,
                    WaterGravityMultiplier = 0.25f,
                    WaterBuoyancy = 3.75f,
                    WaterJumpVelocity = 4.6f,
                    WaterMaxFallSpeed = 2.75f,
                    WaterCurrentMultiplier = 1.75f
                },
                Audio =
                {
                    SpatialInnerRadius = 3.5f,
                    SpatialOuterRadius = 28f,
                    SpatialPanStrength = 0.8f,
                    MasterVolume = 0.9f,
                    MobVolume = 0.8f,
                    MobStepVolume = 0.55f,
                    FuseVolume = 0.45f,
                    ExplosionVolume = 0.65f,
                    SwimStepDistance = 1.45f,
                    FireVolume = 0.4f,
                    WeatherVolume = 0.42f,
                    WeatherIntervalSeconds = 9.5f
                },
                Particles =
                {
                    MaxParticles = 384,
                    Gravity = -6f,
                    Drag = 0.88f,
                    MobHurtCount = 6,
                    MobDeathCount = 18,
                    MobHurtLifetime = 0.28f,
                    MobDeathLifetime = 0.62f,
                    MobHurtSize = 0.1f,
                    MobDeathSize = 0.16f,
                    MobHurtSpeed = 1.8f,
                    MobDeathSpeed = 2.4f,
                    MobHurtSpread = 0.2f,
                    MobDeathSpread = 0.3f,
                    MobHurtUpwardBias = 0.18f,
                    MobDeathUpwardBias = 0.34f,
                    MobHurtMotionInfluence = 0.1f,
                    MobDeathMotionInfluence = 0.14f,
                    MobAttackCount = 7,
                    MobAttackLifetime = 0.26f,
                    MobAttackSize = 0.12f,
                    MobAttackSpeed = 1.55f,
                    MobAttackSpread = 0.19f,
                    MobAttackUpwardBias = 0.2f,
                    MobAttackMotionInfluence = 0.09f,
                    EliteMobParticleMultiplier = 1.5f
                },
                Tnt =
                {
                    Enabled = true,
                    PrimeOnPlace = true,
                    FuseSeconds = 3.8f,
                    ChainReactionFuseSeconds = 0.4f,
                    ExplosionRadius = 4.75f,
                    ExplosionDamage = 18,
                    KnockbackStrength = 7.5f,
                    ResistanceScale = 0.4f,
                    MaxAffectedBlocks = 400,
                    MaxPrimedTnt = 2048,
                    MaxEventQueue = 256
                },
                Fire =
                {
                    Enabled = true,
                    MaxUpdatesPerFrame = 72,
                    MaxEventQueue = 256,
                    MaxExplosionIgnitedBlocks = 24,
                    MaxAgeSeconds = 6.5f,
                    SpreadIntervalSeconds = 0.25f,
                    SpreadChance = 0.4f,
                    BurnStartSeconds = 1.0f,
                    BurnChance = 0.3f,
                    ExplosionIgniteRadius = 6f,
                    ExplosionIgniteChance = 0.55f,
                    RainExtinguishMultiplier = 1.2f
                },
                Mob =
                {
                    Enabled = true,
                    MaxAlive = 18,
                    SpawnRadius = 128,
                    DespawnRadius = 192,
                    SpawnIntervalSeconds = 4.5f,
                    SpawnAttemptsPerTick = 12,
                    PlayerAttackDamage = 5,
                    PlayerAttackCooldownSeconds = 0.4f,
                    PlayerDamageCooldownSeconds = 1.0f,
                    HostileDayMultiplier = 0.3f,
                    HostileNightMultiplier = 1.2f,
                    PassiveDayMultiplier = 1.1f,
                    PassiveNightMultiplier = 0.4f,
                    RainHostileMultiplier = 1.5f,
                    RainPassiveMultiplier = 0.5f,
                    HostileSkyExposureMultiplier = 0.4f,
                    HostileShelterMultiplier = 1.25f,
                    PassiveSkyExposureMultiplier = 1.15f,
                    PassiveShelterMultiplier = 0.6f,
                    HostilePursuitSeconds = 5f,
                    HostilePursuitSpeedMultiplier = 1.2f,
                    Gravity = -18f,
                    WaterSlowMultiplier = 0.4f,
                    WaterBuoyancy = 6.5f,
                    WaterCurrentMultiplier = 1.5f,
                    LavaDamagePerSecond = 9f,
                    EdgeAvoidDistance = 1.5f,
                    StepHeight = 0.65f,
                    JumpVelocity = 6.1f,
                    WanderChangeSeconds = 4f,
                    ZombieWeight = 1.2f,
                    CreeperWeight = 0.7f,
                    CowWeight = 1.1f,
                    SheepWeight = 1.0f,
                    ChickenWeight = 0.8f,
                    EliteSpawnChance = 0.12f,
                    EliteVariantChance = 0.3f,
                    EliteHealthMultiplier = 2.1f,
                    EliteDamageMultiplier = 1.5f,
                    EliteSpeedMultiplier = 1.2f,
                    ElitePursuitMultiplier = 1.6f,
                    EliteDropMultiplier = 2f,
                    StaggerSeconds = 0.2f,
                    StaggerSpeedMultiplier = 0.4f,
                    MaxEventQueue = 512
                },
                SaveSettings =
                {
                    EnableAutoSave = false,
                    AutoSaveIntervalSeconds = 45f,
                    SaveWorkers = 2,
                    LoadWorkers = 3,
                    UnloadExtraRadius = 4,
                    MaxBlockChangesPerFrame = 3072
                },
                Spawn =
                {
                    Mode = "SpawnBiome",
                    SearchRadius = 4096,
                    MaxAttempts = 128,
                    Randomize = false
                },
                Ui =
                {
                    HudScale = 1.5f,
                    FontFile = "minecraft/font/TestFont.ttf",
                    AutoScale = false,
                    HotbarCountFontSize = 11f,
                    HotbarCountMarginX = 5f,
                    HotbarCountMarginY = 2f
                },
                Render =
                {
                    Anisotropy = 4,
                    UseMipmaps = true,
                    FogStart = 140f,
                    FogEnd = 480f,
                    ChunkMeshWorkers = 4
                },
                FirstPerson =
                {
                    Enabled = true,
                    ItemScale = 0.5f,
                    SwingDurationSeconds = 0.4f,
                    TransparentScale = 0.3f,
                    HandMotionScale = 0.8f,
                    HandSwingScale = 0.9f,
                    ItemMotionScale = 0.6f,
                    ItemSwingScale = 1.05f,
                    SwingCurvePower = 1.7f
                },
                CameraMotion =
                {
                    Enabled = true,
                    BobAmplitude = 0.05f,
                    BobSpeed = 8f,
                    BobLateralFactor = 0.6f,
                    BobForwardFactor = 0.3f,
                    InertiaStrength = 10f,
                    AirborneMultiplier = 0.15f,
                    LiquidMultiplier = 0.4f
                },
                Herobrine =
                {
                    Enabled = true,
                    Mode = "Nightmare",
                    MinManifestIntervalSeconds = 75f,
                    MaxManifestIntervalSeconds = 210f,
                    ManifestDurationSeconds = 7.5f,
                    EventCooldownSeconds = 16f,
                    WorldEffectCooldownSeconds = 38f,
                    MinManifestDistance = 20f,
                    MaxManifestDistance = 54f,
                    BehindPlayerChance = 0.6f,
                    NightBias = 1.2f,
                    CaveBias = 1.4f,
                    DirectLookDespawnSeconds = 1.1f,
                    HiddenTimeoutSeconds = 5.5f,
                    WorldEffectIntensity = 0.65f
                },
                Tools =
                {
                    WoodMaxDurability = 80,
                    StoneMaxDurability = 160,
                    IronMaxDurability = 320,
                    DiamondMaxDurability = 640,
                    ToolWearPerAction = 2,
                    BlockBreakTimeScale = 0.7f,
                    MinBreakSeconds = 0.12f,
                    WrongToolBreakPenalty = 1.9f,
                    WoodMiningSpeedMultiplier = 1.05f,
                    StoneMiningSpeedMultiplier = 1.45f,
                    IronMiningSpeedMultiplier = 1.95f,
                    DiamondMiningSpeedMultiplier = 2.75f,
                    WoodRepairDurability = 24,
                    StoneRepairDurability = 40,
                    IronRepairDurability = 72,
                    DiamondRepairDurability = 144
                },
                DayNight =
                {
                    SunIntensityCurve = 1.3f,
                    MoonPhaseCycleDays = 6
                },
                Weather =
                {
                    ToggleIntervalSeconds = 100f,
                    ToggleChance = 0.35f,
                    RainDarkenR = 0.62f,
                    RainDarkenG = 0.68f,
                    RainDarkenB = 0.75f,
                    RainFadeInSeconds = 2.5f,
                    RainFadeOutSeconds = 4.5f,
                    RainSpawnRate = 54f,
                    RainMaxParticles = 128,
                    RainParticleSpeed = 28f,
                    RainParticleLength = 18f,
                    RainParticleWidth = 1.25f,
                    RainStreakTint = new Color3(0.68f, 0.78f, 0.94f),
                    RainStreakAlpha = 0.22f,
                    LightningChancePerSecond = 0.07f,
                    LightningMinRainIntensity = 0.6f,
                    LightningFlashFadeSeconds = 0.25f,
                    LightningFlashStrength = 0.75f,
                    LightningThunderDelaySeconds = 1.75f,
                    LightningCooldownSeconds = 24f
                },
                Atmosphere =
                {
                    SunSize = 72f,
                    MoonSize = 50f,
                    FogDayColor = new Color3(0.48f, 0.58f, 0.74f),
                    FogNightColor = new Color3(0.04f, 0.05f, 0.08f),
                    FogRainColor = new Color3(0.34f, 0.38f, 0.44f),
                    FogNightMultiplier = 0.8f,
                    FogRainMultiplier = 0.9f
                },
                WorldGen =
                {
                    BaseHeight = 60,
                    SeaLevel = 64,
                    ForcedBiome = "Taiga",
                    RiverThreshold = 0.08f,
                    TreeChance = 0.12f,
                    RuinChance = 0.006f,
                    MineShaftChance = 0.004f,
                    ChunkGenerationWorkers = 3
                }
            };

            GameConfig.Save(path, config);
            var loaded = GameConfig.LoadOrCreate(path);

            Assert.Equal(12, loaded.ChunkRadius);
            Assert.True(loaded.Fullscreen);
            Assert.Equal(320, loaded.WindowX);
            Assert.Equal(180, loaded.WindowY);
            Assert.Equal(1600, loaded.WindowWidth);
            Assert.Equal(900, loaded.WindowHeight);
            Assert.Equal("en", loaded.Language);
            Assert.Equal(90f, loaded.FieldOfView);
            Assert.Equal(0.18f, loaded.MouseSensitivity);
            Assert.Equal(7.5f, loaded.PlayerSpeed);
            Assert.Equal(24, loaded.Physics.PlayerMaxHealth);
            Assert.Equal(1.0f, loaded.Physics.HurtCooldownSeconds);
            Assert.Equal(0.5f, loaded.Physics.WaterMoveMultiplier);
            Assert.Equal(0.25f, loaded.Physics.WaterGravityMultiplier);
            Assert.Equal(3.75f, loaded.Physics.WaterBuoyancy);
            Assert.Equal(4.6f, loaded.Physics.WaterJumpVelocity);
            Assert.Equal(2.75f, loaded.Physics.WaterMaxFallSpeed);
            Assert.Equal(1.75f, loaded.Physics.WaterCurrentMultiplier);
            Assert.Equal(0.9f, loaded.Audio.MasterVolume);
            Assert.Equal(0.8f, loaded.Audio.MobVolume);
            Assert.Equal(0.55f, loaded.Audio.MobStepVolume);
            Assert.Equal(0.45f, loaded.Audio.FuseVolume);
            Assert.Equal(0.65f, loaded.Audio.ExplosionVolume);
            Assert.Equal(3.5f, loaded.Audio.SpatialInnerRadius);
            Assert.Equal(28f, loaded.Audio.SpatialOuterRadius);
            Assert.Equal(0.8f, loaded.Audio.SpatialPanStrength);
            Assert.Equal(1.45f, loaded.Audio.SwimStepDistance);
            Assert.Equal(0.42f, loaded.Audio.WeatherVolume);
            Assert.Equal(9.5f, loaded.Audio.WeatherIntervalSeconds);
            Assert.Equal(384, loaded.Particles.MaxParticles);
            Assert.Equal(-6f, loaded.Particles.Gravity);
            Assert.Equal(0.88f, loaded.Particles.Drag);
            Assert.Equal(6, loaded.Particles.MobHurtCount);
            Assert.Equal(18, loaded.Particles.MobDeathCount);
            Assert.Equal(0.28f, loaded.Particles.MobHurtLifetime);
            Assert.Equal(0.62f, loaded.Particles.MobDeathLifetime);
            Assert.Equal(0.1f, loaded.Particles.MobHurtSize);
            Assert.Equal(0.16f, loaded.Particles.MobDeathSize);
            Assert.Equal(1.8f, loaded.Particles.MobHurtSpeed);
            Assert.Equal(2.4f, loaded.Particles.MobDeathSpeed);
            Assert.Equal(0.2f, loaded.Particles.MobHurtSpread);
            Assert.Equal(0.3f, loaded.Particles.MobDeathSpread);
            Assert.Equal(0.18f, loaded.Particles.MobHurtUpwardBias);
            Assert.Equal(0.34f, loaded.Particles.MobDeathUpwardBias);
            Assert.Equal(0.1f, loaded.Particles.MobHurtMotionInfluence);
            Assert.Equal(0.14f, loaded.Particles.MobDeathMotionInfluence);
            Assert.Equal(7, loaded.Particles.MobAttackCount);
            Assert.Equal(0.26f, loaded.Particles.MobAttackLifetime);
            Assert.Equal(0.12f, loaded.Particles.MobAttackSize);
            Assert.Equal(1.55f, loaded.Particles.MobAttackSpeed);
            Assert.Equal(0.19f, loaded.Particles.MobAttackSpread);
            Assert.Equal(0.2f, loaded.Particles.MobAttackUpwardBias);
            Assert.Equal(0.09f, loaded.Particles.MobAttackMotionInfluence);
            Assert.Equal(1.5f, loaded.Particles.EliteMobParticleMultiplier);
            Assert.True(loaded.Tnt.Enabled);
            Assert.True(loaded.Tnt.PrimeOnPlace);
            Assert.Equal(3.8f, loaded.Tnt.FuseSeconds);
            Assert.Equal(0.4f, loaded.Tnt.ChainReactionFuseSeconds);
            Assert.Equal(4.75f, loaded.Tnt.ExplosionRadius);
            Assert.Equal(18, loaded.Tnt.ExplosionDamage);
            Assert.Equal(7.5f, loaded.Tnt.KnockbackStrength);
            Assert.Equal(0.4f, loaded.Tnt.ResistanceScale);
            Assert.Equal(400, loaded.Tnt.MaxAffectedBlocks);
            Assert.Equal(2048, loaded.Tnt.MaxPrimedTnt);
            Assert.Equal(256, loaded.Tnt.MaxEventQueue);
            Assert.True(loaded.Fire.Enabled);
            Assert.Equal(72, loaded.Fire.MaxUpdatesPerFrame);
            Assert.Equal(256, loaded.Fire.MaxEventQueue);
            Assert.Equal(24, loaded.Fire.MaxExplosionIgnitedBlocks);
            Assert.Equal(6.5f, loaded.Fire.MaxAgeSeconds);
            Assert.Equal(0.25f, loaded.Fire.SpreadIntervalSeconds);
            Assert.Equal(0.4f, loaded.Fire.SpreadChance);
            Assert.Equal(1.0f, loaded.Fire.BurnStartSeconds);
            Assert.Equal(0.3f, loaded.Fire.BurnChance);
            Assert.Equal(6f, loaded.Fire.ExplosionIgniteRadius);
            Assert.Equal(0.55f, loaded.Fire.ExplosionIgniteChance);
            Assert.Equal(1.2f, loaded.Fire.RainExtinguishMultiplier);
            Assert.Equal(0.4f, loaded.Audio.FireVolume);
            Assert.True(loaded.Mob.Enabled);
            Assert.Equal(18, loaded.Mob.MaxAlive);
            Assert.Equal(512, loaded.Mob.MaxEventQueue);
            Assert.Equal(128, loaded.Mob.SpawnRadius);
            Assert.Equal(192, loaded.Mob.DespawnRadius);
            Assert.Equal(4.5f, loaded.Mob.SpawnIntervalSeconds);
            Assert.Equal(12, loaded.Mob.SpawnAttemptsPerTick);
            Assert.Equal(5, loaded.Mob.PlayerAttackDamage);
            Assert.Equal(0.4f, loaded.Mob.PlayerAttackCooldownSeconds);
            Assert.Equal(1.0f, loaded.Mob.PlayerDamageCooldownSeconds);
            Assert.Equal(0.3f, loaded.Mob.HostileDayMultiplier);
            Assert.Equal(1.2f, loaded.Mob.HostileNightMultiplier);
            Assert.Equal(1.1f, loaded.Mob.PassiveDayMultiplier);
            Assert.Equal(0.4f, loaded.Mob.PassiveNightMultiplier);
            Assert.Equal(1.5f, loaded.Mob.RainHostileMultiplier);
            Assert.Equal(0.5f, loaded.Mob.RainPassiveMultiplier);
            Assert.Equal(0.4f, loaded.Mob.HostileSkyExposureMultiplier);
            Assert.Equal(1.25f, loaded.Mob.HostileShelterMultiplier);
            Assert.Equal(1.15f, loaded.Mob.PassiveSkyExposureMultiplier);
            Assert.Equal(0.6f, loaded.Mob.PassiveShelterMultiplier);
            Assert.Equal(5f, loaded.Mob.HostilePursuitSeconds);
            Assert.Equal(1.2f, loaded.Mob.HostilePursuitSpeedMultiplier);
            Assert.Equal(-18f, loaded.Mob.Gravity);
            Assert.Equal(0.4f, loaded.Mob.WaterSlowMultiplier);
            Assert.Equal(6.5f, loaded.Mob.WaterBuoyancy);
            Assert.Equal(1.5f, loaded.Mob.WaterCurrentMultiplier);
            Assert.Equal(9f, loaded.Mob.LavaDamagePerSecond);
            Assert.Equal(1.5f, loaded.Mob.EdgeAvoidDistance);
            Assert.Equal(0.65f, loaded.Mob.StepHeight);
            Assert.Equal(6.1f, loaded.Mob.JumpVelocity);
            Assert.Equal(4f, loaded.Mob.WanderChangeSeconds);
            Assert.Equal(1.2f, loaded.Mob.ZombieWeight);
            Assert.Equal(0.7f, loaded.Mob.CreeperWeight);
            Assert.Equal(1.1f, loaded.Mob.CowWeight);
            Assert.Equal(1.0f, loaded.Mob.SheepWeight);
            Assert.Equal(0.8f, loaded.Mob.ChickenWeight);
            Assert.Equal(0.12f, loaded.Mob.EliteSpawnChance);
            Assert.Equal(0.3f, loaded.Mob.EliteVariantChance);
            Assert.Equal(2.1f, loaded.Mob.EliteHealthMultiplier);
            Assert.Equal(1.5f, loaded.Mob.EliteDamageMultiplier);
            Assert.Equal(1.2f, loaded.Mob.EliteSpeedMultiplier);
            Assert.Equal(1.6f, loaded.Mob.ElitePursuitMultiplier);
            Assert.Equal(2f, loaded.Mob.EliteDropMultiplier);
            Assert.Equal(0.2f, loaded.Mob.StaggerSeconds);
            Assert.Equal(0.4f, loaded.Mob.StaggerSpeedMultiplier);
            Assert.False(loaded.SaveSettings.EnableAutoSave);
            Assert.Equal(45f, loaded.SaveSettings.AutoSaveIntervalSeconds);
            Assert.Equal(2, loaded.SaveSettings.SaveWorkers);
            Assert.Equal(3, loaded.SaveSettings.LoadWorkers);
            Assert.Equal(4, loaded.SaveSettings.UnloadExtraRadius);
            Assert.Equal(3072, loaded.SaveSettings.MaxBlockChangesPerFrame);
            Assert.Equal("SpawnBiome", loaded.Spawn.Mode);
            Assert.Equal(4096, loaded.Spawn.SearchRadius);
            Assert.Equal(128, loaded.Spawn.MaxAttempts);
            Assert.False(loaded.Spawn.Randomize);
            Assert.Equal(1.5f, loaded.Ui.HudScale);
            Assert.Equal("minecraft/font/TestFont.ttf", loaded.Ui.FontFile);
            Assert.False(loaded.Ui.AutoScale);
            Assert.Equal(11f, loaded.Ui.HotbarCountFontSize);
            Assert.Equal(5f, loaded.Ui.HotbarCountMarginX);
            Assert.Equal(2f, loaded.Ui.HotbarCountMarginY);
            Assert.Equal(4, loaded.Render.Anisotropy);
            Assert.True(loaded.Render.UseMipmaps);
            Assert.Equal(140f, loaded.Render.FogStart);
            Assert.Equal(480f, loaded.Render.FogEnd);
            Assert.Equal(4, loaded.Render.ChunkMeshWorkers);
            Assert.True(loaded.FirstPerson.Enabled);
            Assert.Equal(0.5f, loaded.FirstPerson.ItemScale);
            Assert.Equal(0.4f, loaded.FirstPerson.SwingDurationSeconds);
            Assert.Equal(0.3f, loaded.FirstPerson.TransparentScale);
            Assert.Equal(0.8f, loaded.FirstPerson.HandMotionScale);
            Assert.Equal(0.9f, loaded.FirstPerson.HandSwingScale);
            Assert.Equal(0.6f, loaded.FirstPerson.ItemMotionScale);
            Assert.Equal(1.05f, loaded.FirstPerson.ItemSwingScale);
            Assert.Equal(1.7f, loaded.FirstPerson.SwingCurvePower);
            Assert.Equal(60, loaded.WorldGen.BaseHeight);
            Assert.Equal(64, loaded.WorldGen.SeaLevel);
            Assert.Equal("Taiga", loaded.WorldGen.ForcedBiome);
            Assert.Equal(0.08f, loaded.WorldGen.RiverThreshold);
            Assert.Equal(0.12f, loaded.WorldGen.TreeChance);
            Assert.Equal(0.006f, loaded.WorldGen.RuinChance);
            Assert.Equal(0.004f, loaded.WorldGen.MineShaftChance);
            Assert.Equal(3, loaded.WorldGen.ChunkGenerationWorkers);
            Assert.NotNull(loaded.CameraMotion);
            Assert.True(loaded.CameraMotion.Enabled);
            Assert.Equal(0.05f, loaded.CameraMotion.BobAmplitude);
            Assert.Equal(8f, loaded.CameraMotion.BobSpeed);
            Assert.Equal(0.6f, loaded.CameraMotion.BobLateralFactor);
            Assert.Equal(0.3f, loaded.CameraMotion.BobForwardFactor);
            Assert.Equal(10f, loaded.CameraMotion.InertiaStrength);
            Assert.Equal(0.15f, loaded.CameraMotion.AirborneMultiplier);
            Assert.Equal(0.4f, loaded.CameraMotion.LiquidMultiplier);
            Assert.NotNull(loaded.Herobrine);
            Assert.True(loaded.Herobrine.Enabled);
            Assert.Equal(75f, loaded.Herobrine.MinManifestIntervalSeconds);
            Assert.Equal(210f, loaded.Herobrine.MaxManifestIntervalSeconds);
            Assert.Equal("Nightmare", loaded.Herobrine.Mode);
            Assert.Equal(7.5f, loaded.Herobrine.ManifestDurationSeconds);
            Assert.Equal(16f, loaded.Herobrine.EventCooldownSeconds);
            Assert.Equal(38f, loaded.Herobrine.WorldEffectCooldownSeconds);
            Assert.Equal(20f, loaded.Herobrine.MinManifestDistance);
            Assert.Equal(54f, loaded.Herobrine.MaxManifestDistance);
            Assert.Equal(0.6f, loaded.Herobrine.BehindPlayerChance);
            Assert.Equal(1.2f, loaded.Herobrine.NightBias);
            Assert.Equal(1.4f, loaded.Herobrine.CaveBias);
            Assert.Equal(1.1f, loaded.Herobrine.DirectLookDespawnSeconds);
            Assert.Equal(5.5f, loaded.Herobrine.HiddenTimeoutSeconds);
            Assert.Equal(0.65f, loaded.Herobrine.WorldEffectIntensity);
            Assert.Equal(80, loaded.Tools.WoodMaxDurability);
            Assert.Equal(160, loaded.Tools.StoneMaxDurability);
            Assert.Equal(320, loaded.Tools.IronMaxDurability);
            Assert.Equal(640, loaded.Tools.DiamondMaxDurability);
            Assert.Equal(2, loaded.Tools.ToolWearPerAction);
            Assert.Equal(0.7f, loaded.Tools.BlockBreakTimeScale);
            Assert.Equal(0.12f, loaded.Tools.MinBreakSeconds);
            Assert.Equal(1.9f, loaded.Tools.WrongToolBreakPenalty);
            Assert.Equal(1.05f, loaded.Tools.WoodMiningSpeedMultiplier);
            Assert.Equal(1.45f, loaded.Tools.StoneMiningSpeedMultiplier);
            Assert.Equal(1.95f, loaded.Tools.IronMiningSpeedMultiplier);
            Assert.Equal(2.75f, loaded.Tools.DiamondMiningSpeedMultiplier);
            Assert.Equal(24, loaded.Tools.WoodRepairDurability);
            Assert.Equal(40, loaded.Tools.StoneRepairDurability);
            Assert.Equal(72, loaded.Tools.IronRepairDurability);
            Assert.Equal(144, loaded.Tools.DiamondRepairDurability);
            Assert.Equal(1.3f, loaded.DayNight.SunIntensityCurve);
            Assert.Equal(6, loaded.DayNight.MoonPhaseCycleDays);
            Assert.Equal(100f, loaded.Weather.ToggleIntervalSeconds);
            Assert.Equal(0.35f, loaded.Weather.ToggleChance);
            Assert.Equal(0.62f, loaded.Weather.RainDarkenR);
            Assert.Equal(0.68f, loaded.Weather.RainDarkenG);
            Assert.Equal(0.75f, loaded.Weather.RainDarkenB);
            Assert.Equal(2.5f, loaded.Weather.RainFadeInSeconds);
            Assert.Equal(4.5f, loaded.Weather.RainFadeOutSeconds);
            Assert.Equal(54f, loaded.Weather.RainSpawnRate);
            Assert.Equal(128, loaded.Weather.RainMaxParticles);
            Assert.Equal(28f, loaded.Weather.RainParticleSpeed);
            Assert.Equal(18f, loaded.Weather.RainParticleLength);
            Assert.Equal(1.25f, loaded.Weather.RainParticleWidth);
            Assert.Equal(0.68f, loaded.Weather.RainStreakTint.R);
            Assert.Equal(0.78f, loaded.Weather.RainStreakTint.G);
            Assert.Equal(0.94f, loaded.Weather.RainStreakTint.B);
            Assert.Equal(0.22f, loaded.Weather.RainStreakAlpha);
            Assert.Equal(0.07f, loaded.Weather.LightningChancePerSecond);
            Assert.Equal(0.6f, loaded.Weather.LightningMinRainIntensity);
            Assert.Equal(0.25f, loaded.Weather.LightningFlashFadeSeconds);
            Assert.Equal(0.75f, loaded.Weather.LightningFlashStrength);
            Assert.Equal(1.75f, loaded.Weather.LightningThunderDelaySeconds);
            Assert.Equal(24f, loaded.Weather.LightningCooldownSeconds);
            Assert.Equal(72f, loaded.Atmosphere.SunSize);
            Assert.Equal(50f, loaded.Atmosphere.MoonSize);
            Assert.Equal(0.48f, loaded.Atmosphere.FogDayColor.R);
            Assert.Equal(0.58f, loaded.Atmosphere.FogDayColor.G);
            Assert.Equal(0.74f, loaded.Atmosphere.FogDayColor.B);
            Assert.Equal(0.04f, loaded.Atmosphere.FogNightColor.R);
            Assert.Equal(0.05f, loaded.Atmosphere.FogNightColor.G);
            Assert.Equal(0.08f, loaded.Atmosphere.FogNightColor.B);
            Assert.Equal(0.34f, loaded.Atmosphere.FogRainColor.R);
            Assert.Equal(0.38f, loaded.Atmosphere.FogRainColor.G);
            Assert.Equal(0.44f, loaded.Atmosphere.FogRainColor.B);
            Assert.Equal(0.8f, loaded.Atmosphere.FogNightMultiplier);
            Assert.Equal(0.9f, loaded.Atmosphere.FogRainMultiplier);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Normalize_ClampsInvalidValues()
    {
        var config = new GameConfig
        {
            ChunkPreloadExtra = -2,
            WindowWidth = 0,
            WindowHeight = -1,
            Audio =
            {
                MaxActive = 1000,
                SpatialInnerRadius = -1f,
                SpatialOuterRadius = 0.1f,
                SpatialPanStrength = 2f,
                MasterVolume = -1f,
                MobVolume = -1f,
                MobStepVolume = 2f,
                FuseVolume = 2f,
                ExplosionVolume = 2f,
                SwimStepDistance = 0f,
                FireVolume = 2f,
                WeatherVolume = 2f,
                WeatherIntervalSeconds = 0f
            },
            Physics =
            {
                PlayerHeight = 1.2f,
                EyeHeight = 2.5f,
                SprintMultiplier = 0f,
                PlayerMaxHealth = 0,
                HurtCooldownSeconds = -1f,
                WaterMoveMultiplier = 0f,
                WaterGravityMultiplier = -1f,
                WaterBuoyancy = -1f,
                WaterJumpVelocity = -1f,
                WaterMaxFallSpeed = -1f
            },
            Spawn =
            {
                SearchRadius = 1,
                MaxAttempts = 0,
                MinHeightAboveSea = 0
            },
            SaveSettings =
            {
                MaxBlockChangesPerFrame = 0,
                LoadWorkers = 0
            },
            Mob =
            {
                MaxAlive = -4,
                MaxEventQueue = 0,
                SpawnRadius = 1,
                DespawnRadius = 8,
                SpawnIntervalSeconds = 0.01f,
                SpawnAttemptsPerTick = 0,
                PlayerAttackDamage = 0,
                PlayerAttackCooldownSeconds = -2f,
                PlayerDamageCooldownSeconds = -3f,
                HostileDayMultiplier = -1f,
                HostileNightMultiplier = -2f,
                PassiveDayMultiplier = -3f,
                PassiveNightMultiplier = -4f,
                Gravity = 0f,
                WaterSlowMultiplier = -1f,
                WaterBuoyancy = -1f,
                LavaDamagePerSecond = -1f,
                EdgeAvoidDistance = -1f,
                StepHeight = -1f,
                JumpVelocity = -1f,
                WanderChangeSeconds = 0.01f,
                ZombieWeight = -1f,
                CreeperWeight = -1f,
                CowWeight = -1f,
                SheepWeight = -1f,
                ChickenWeight = -1f,
                EliteSpawnChance = -1f,
                EliteVariantChance = -1f,
                EliteHealthMultiplier = 0f,
                EliteDamageMultiplier = 0f,
                EliteSpeedMultiplier = 0f,
                ElitePursuitMultiplier = 0f,
                EliteDropMultiplier = 0f,
                StaggerSeconds = 0f,
                StaggerSpeedMultiplier = 0f
            },
            Tnt =
            {
                FuseSeconds = 0f,
                ChainReactionFuseSeconds = 0f,
                ExplosionRadius = 0f,
                ExplosionDamage = 0,
                KnockbackStrength = -1f,
                ResistanceScale = 0f,
                MaxAffectedBlocks = 0,
                MaxPrimedTnt = 0,
                MaxEventQueue = 0
            },
            Fire =
            {
                MaxUpdatesPerFrame = 0,
                MaxEventQueue = 0,
                MaxExplosionIgnitedBlocks = 0,
                MaxAgeSeconds = 0f,
                SpreadIntervalSeconds = 0f,
                SpreadChance = -1f,
                BurnStartSeconds = -1f,
                BurnChance = -1f,
                ExplosionIgniteRadius = 0f,
                ExplosionIgniteChance = -1f,
                RainExtinguishMultiplier = -1f
            },
            Render =
            {
                FogStart = 100f,
                FogEnd = 100f,
                MaxMeshUploadsPerFrame = 0,
                Anisotropy = 0,
                ChunkMeshWorkers = 0
            },
            FirstPerson =
            {
                HandWidth = 0f,
                HandHeight = 0f,
                HandDepth = 0f,
                HandMotionScale = 0f,
                HandSwingScale = 0f,
                ItemScale = 0f,
                TransparentScale = 0f,
                ItemMotionScale = 0f,
                ItemSwingScale = 0f,
                SwingCurvePower = 0f,
                SwingDurationSeconds = 0f,
                CrossScale = 0f,
                TorchScale = 0f,
                CardThickness = 0f
            },
            CameraMotion =
            {
                BobAmplitude = -1f,
                BobSpeed = -2f,
                BobLateralFactor = -3f,
                BobForwardFactor = -4f,
                InertiaStrength = -5f,
                AirborneMultiplier = -0.5f,
                LiquidMultiplier = 2f
            },
            DayNight =
            {
                SunIntensityCurve = 0f,
                MoonPhaseCycleDays = 0
            },
            Weather =
            {
                RainFadeInSeconds = 0f,
                RainFadeOutSeconds = 0f,
                RainSpawnRate = -1f,
                RainMaxParticles = -1,
                RainParticleSpeed = -1f,
                RainParticleLength = 0f,
                RainParticleWidth = 0f,
                RainStreakAlpha = 2f,
                LightningChancePerSecond = -1f,
                LightningMinRainIntensity = 2f,
                LightningFlashFadeSeconds = 0f,
                LightningFlashStrength = 2f,
                LightningThunderDelaySeconds = 0f,
                LightningCooldownSeconds = 0f
            },
            Particles =
            {
                MaxParticles = 0,
                Gravity = 2f,
                Drag = 2f,
                MobHurtCount = 0,
                MobDeathCount = 0,
                MobHurtLifetime = 0f,
                MobDeathLifetime = 0f,
                MobHurtSize = 0f,
                MobDeathSize = 0f,
                MobHurtSpeed = 0f,
                MobDeathSpeed = 0f,
                MobHurtSpread = 0f,
                MobDeathSpread = 0f,
                MobHurtUpwardBias = -1f,
                MobDeathUpwardBias = 2f,
                MobHurtMotionInfluence = -1f,
                MobDeathMotionInfluence = 2f,
                MobAttackCount = 0,
                MobAttackLifetime = 0f,
                MobAttackSize = 0f,
                MobAttackSpeed = 0f,
                MobAttackSpread = 0f,
                MobAttackUpwardBias = -1f,
                MobAttackMotionInfluence = 2f,
                EliteMobParticleMultiplier = 0f
            },
            Atmosphere =
            {
                SunSize = 0f,
                MoonSize = 0f,
                FogNightMultiplier = 0f,
                FogRainMultiplier = 3f
            },
            Herobrine =
            {
                MinManifestIntervalSeconds = 0f,
                MaxManifestIntervalSeconds = 1f,
                ManifestDurationSeconds = 0f,
                EventCooldownSeconds = -2f,
                WorldEffectCooldownSeconds = -3f,
                MinManifestDistance = 1f,
                MaxManifestDistance = 2f,
                BehindPlayerChance = 2f,
                NightBias = -1f,
                CaveBias = 6f,
                DirectLookDespawnSeconds = 0f,
                HiddenTimeoutSeconds = -1f,
                WorldEffectIntensity = 2f
            },
            WorldGen =
            {
                RiverBankInfluenceMin = 0.9f,
                RiverWaterInfluenceMin = 0.1f,
                LargeTreeMinHeight = 1,
                LargeTreeMaxHeight = 1,
                RuinChance = -1f,
                MineShaftChance = 2f,
                ChunkGenerationWorkers = 0
            },
            Ui =
            {
                FontFile = " "
            }
        };

        config.Normalize();

        Assert.Equal(1, config.WindowWidth);
        Assert.Equal(1, config.WindowHeight);
        Assert.Equal(0, config.ChunkPreloadExtra);
        Assert.Equal(64, config.Spawn.SearchRadius);
        Assert.Equal(1, config.Spawn.MaxAttempts);
        Assert.Equal(1, config.Spawn.MinHeightAboveSea);
        Assert.Equal(64, config.Audio.MaxActive);
        Assert.Equal(0.5f, config.Audio.SpatialInnerRadius);
        Assert.Equal(1.5f, config.Audio.SpatialOuterRadius);
        Assert.Equal(1f, config.Audio.SpatialPanStrength);
        Assert.Equal(0f, config.Audio.MasterVolume);
        Assert.Equal(0.1f, config.Audio.SwimStepDistance);
        Assert.Equal(1.2f, config.Physics.EyeHeight);
        Assert.Equal(1f, config.Physics.SprintMultiplier);
        Assert.Equal(1, config.Physics.PlayerMaxHealth);
        Assert.Equal(0f, config.Physics.HurtCooldownSeconds);
        Assert.Equal(0.1f, config.Physics.WaterMoveMultiplier);
        Assert.Equal(0f, config.Physics.WaterGravityMultiplier);
        Assert.Equal(0f, config.Physics.WaterBuoyancy);
        Assert.Equal(0f, config.Physics.WaterJumpVelocity);
        Assert.Equal(0f, config.Physics.WaterMaxFallSpeed);
        Assert.Equal(0f, config.Audio.MobVolume);
        Assert.Equal(1f, config.Audio.MobStepVolume);
        Assert.Equal(1f, config.Audio.FuseVolume);
        Assert.Equal(1f, config.Audio.ExplosionVolume);
        Assert.Equal(1f, config.Audio.FireVolume);
        Assert.Equal(110f, config.Render.FogEnd);
        Assert.Equal(1, config.Render.MaxMeshUploadsPerFrame);
        Assert.Equal(RenderConfig.DefaultChunkMeshWorkers, config.Render.ChunkMeshWorkers);
        Assert.Equal(1, config.Render.Anisotropy);
        Assert.False(config.Render.UseMipmaps);
        Assert.Equal(1, config.SaveSettings.LoadWorkers);
        Assert.True(config.FirstPerson.HandWidth > 0f);
        Assert.True(config.FirstPerson.HandHeight > 0f);
        Assert.True(config.FirstPerson.HandDepth > 0f);
        Assert.True(config.FirstPerson.HandMotionScale > 0f);
        Assert.True(config.FirstPerson.HandSwingScale > 0f);
        Assert.True(config.FirstPerson.ItemScale > 0f);
        Assert.True(config.FirstPerson.TransparentScale > 0f);
        Assert.True(config.FirstPerson.ItemMotionScale > 0f);
        Assert.True(config.FirstPerson.ItemSwingScale > 0f);
        Assert.True(config.FirstPerson.SwingCurvePower > 0f);
        Assert.True(config.FirstPerson.SwingDurationSeconds > 0f);
        Assert.True(config.FirstPerson.CrossScale > 0f);
        Assert.True(config.FirstPerson.TorchScale > 0f);
        Assert.True(config.FirstPerson.CardThickness > 0f);
        Assert.Equal(0f, config.CameraMotion.BobAmplitude);
        Assert.Equal(0f, config.CameraMotion.BobSpeed);
        Assert.Equal(0f, config.CameraMotion.BobLateralFactor);
        Assert.Equal(0f, config.CameraMotion.BobForwardFactor);
        Assert.Equal(0f, config.CameraMotion.InertiaStrength);
        Assert.Equal(0f, config.CameraMotion.AirborneMultiplier);
        Assert.Equal(1f, config.CameraMotion.LiquidMultiplier);
        Assert.Equal(0.1f, config.DayNight.SunIntensityCurve);
        Assert.Equal(1, config.DayNight.MoonPhaseCycleDays);
        Assert.Equal(0.1f, config.Weather.RainFadeInSeconds);
        Assert.Equal(0.1f, config.Weather.RainFadeOutSeconds);
        Assert.Equal(0f, config.Weather.RainSpawnRate);
        Assert.Equal(0, config.Weather.RainMaxParticles);
        Assert.Equal(0f, config.Weather.RainParticleSpeed);
        Assert.Equal(0.1f, config.Weather.RainParticleLength);
        Assert.Equal(0.1f, config.Weather.RainParticleWidth);
        Assert.Equal(1f, config.Weather.RainStreakAlpha);
        Assert.Equal(0f, config.Weather.LightningChancePerSecond);
        Assert.Equal(1f, config.Weather.LightningMinRainIntensity);
        Assert.Equal(0.05f, config.Weather.LightningFlashFadeSeconds);
        Assert.Equal(1f, config.Weather.LightningFlashStrength);
        Assert.Equal(0.05f, config.Weather.LightningThunderDelaySeconds);
        Assert.Equal(1f, config.Weather.LightningCooldownSeconds);
        Assert.Equal(16, config.Particles.MaxParticles);
        Assert.Equal(0f, config.Particles.Gravity);
        Assert.Equal(1f, config.Particles.Drag);
        Assert.Equal(1, config.Particles.MobHurtCount);
        Assert.Equal(1, config.Particles.MobDeathCount);
        Assert.Equal(0.05f, config.Particles.MobHurtLifetime);
        Assert.Equal(0.05f, config.Particles.MobDeathLifetime);
        Assert.Equal(0.01f, config.Particles.MobHurtSize);
        Assert.Equal(0.01f, config.Particles.MobDeathSize);
        Assert.Equal(0.01f, config.Particles.MobHurtSpeed);
        Assert.Equal(0.01f, config.Particles.MobDeathSpeed);
        Assert.Equal(0.01f, config.Particles.MobHurtSpread);
        Assert.Equal(0.01f, config.Particles.MobDeathSpread);
        Assert.Equal(0f, config.Particles.MobHurtUpwardBias);
        Assert.Equal(1f, config.Particles.MobDeathUpwardBias);
        Assert.Equal(0f, config.Particles.MobHurtMotionInfluence);
        Assert.Equal(1f, config.Particles.MobDeathMotionInfluence);
        Assert.Equal(1, config.Particles.MobAttackCount);
        Assert.Equal(0.05f, config.Particles.MobAttackLifetime);
        Assert.Equal(0.01f, config.Particles.MobAttackSize);
        Assert.Equal(0.01f, config.Particles.MobAttackSpeed);
        Assert.Equal(0.01f, config.Particles.MobAttackSpread);
        Assert.Equal(0f, config.Particles.MobAttackUpwardBias);
        Assert.Equal(1f, config.Particles.MobAttackMotionInfluence);
        Assert.Equal(0.5f, config.Particles.EliteMobParticleMultiplier);
        Assert.Equal(1f, config.Audio.WeatherVolume);
        Assert.Equal(1f, config.Audio.WeatherIntervalSeconds);
        Assert.Equal(1f, config.Atmosphere.SunSize);
        Assert.Equal(1f, config.Atmosphere.MoonSize);
        Assert.Equal(0.25f, config.Atmosphere.FogNightMultiplier);
        Assert.Equal(2f, config.Atmosphere.FogRainMultiplier);
        Assert.Equal(5f, config.Herobrine.MinManifestIntervalSeconds);
        Assert.Equal(6f, config.Herobrine.MaxManifestIntervalSeconds);
        Assert.Equal(0.5f, config.Herobrine.ManifestDurationSeconds);
        Assert.Equal(0f, config.Herobrine.EventCooldownSeconds);
        Assert.Equal(0f, config.Herobrine.WorldEffectCooldownSeconds);
        Assert.Equal(8f, config.Herobrine.MinManifestDistance);
        Assert.Equal(12f, config.Herobrine.MaxManifestDistance);
        Assert.Equal(1f, config.Herobrine.BehindPlayerChance);
        Assert.Equal(0f, config.Herobrine.NightBias);
        Assert.Equal(4f, config.Herobrine.CaveBias);
        Assert.Equal(0.1f, config.Herobrine.DirectLookDespawnSeconds);
        Assert.Equal(0f, config.Herobrine.HiddenTimeoutSeconds);
        Assert.Equal(1f, config.Herobrine.WorldEffectIntensity);
        Assert.Equal(WorldGenConfig.DefaultChunkGenerationWorkers, config.WorldGen.ChunkGenerationWorkers);
        Assert.Equal(config.WorldGen.RiverBankInfluenceMin, config.WorldGen.RiverWaterInfluenceMin);
        Assert.Equal(0f, config.WorldGen.RuinChance);
        Assert.Equal(1f, config.WorldGen.MineShaftChance);
        Assert.Equal(4, config.WorldGen.LargeTreeMinHeight);
        Assert.Equal(5, config.WorldGen.LargeTreeMaxHeight);
        Assert.Equal("minecraft/font/NotoSans-Regular.ttf", config.Ui.FontFile);
        Assert.Equal(0, config.Mob.MaxAlive);
        Assert.Equal(16, config.Mob.SpawnRadius);
        Assert.Equal(32, config.Mob.DespawnRadius);
        Assert.Equal(0.25f, config.Mob.SpawnIntervalSeconds);
        Assert.Equal(1, config.Mob.SpawnAttemptsPerTick);
        Assert.Equal(1, config.Mob.PlayerAttackDamage);
        Assert.Equal(0f, config.Mob.PlayerAttackCooldownSeconds);
        Assert.Equal(0f, config.Mob.PlayerDamageCooldownSeconds);
        Assert.Equal(0f, config.Mob.HostileDayMultiplier);
        Assert.Equal(0f, config.Mob.HostileNightMultiplier);
        Assert.Equal(0f, config.Mob.PassiveDayMultiplier);
        Assert.Equal(0f, config.Mob.PassiveNightMultiplier);
        Assert.Equal(0f, config.Mob.Gravity);
        Assert.Equal(0f, config.Mob.WaterSlowMultiplier);
        Assert.Equal(0f, config.Mob.WaterBuoyancy);
        Assert.Equal(0f, config.Mob.LavaDamagePerSecond);
        Assert.Equal(1, config.Mob.MaxEventQueue);
        Assert.Equal(0f, config.Mob.EdgeAvoidDistance);
        Assert.Equal(0f, config.Mob.StepHeight);
        Assert.Equal(0f, config.Mob.JumpVelocity);
        Assert.Equal(0.25f, config.Mob.WanderChangeSeconds);
        Assert.Equal(0f, config.Mob.ZombieWeight);
        Assert.Equal(0f, config.Mob.CreeperWeight);
        Assert.Equal(0f, config.Mob.CowWeight);
        Assert.Equal(0f, config.Mob.SheepWeight);
        Assert.Equal(0f, config.Mob.ChickenWeight);
        Assert.Equal(0f, config.Mob.EliteSpawnChance);
        Assert.Equal(0f, config.Mob.EliteVariantChance);
        Assert.Equal(1f, config.Mob.EliteHealthMultiplier);
        Assert.Equal(1f, config.Mob.EliteDamageMultiplier);
        Assert.Equal(1f, config.Mob.EliteSpeedMultiplier);
        Assert.Equal(1f, config.Mob.ElitePursuitMultiplier);
        Assert.Equal(1f, config.Mob.EliteDropMultiplier);
        Assert.Equal(0.01f, config.Mob.StaggerSeconds);
        Assert.Equal(0.05f, config.Mob.StaggerSpeedMultiplier);
        Assert.Equal(0.1f, config.Tnt.FuseSeconds);
        Assert.Equal(0.05f, config.Tnt.ChainReactionFuseSeconds);
        Assert.Equal(1f, config.Tnt.ExplosionRadius);
        Assert.Equal(1, config.Tnt.ExplosionDamage);
        Assert.Equal(0f, config.Tnt.KnockbackStrength);
        Assert.Equal(0.01f, config.Tnt.ResistanceScale);
        Assert.Equal(1, config.Tnt.MaxAffectedBlocks);
        Assert.Equal(1, config.Tnt.MaxPrimedTnt);
        Assert.Equal(1, config.Tnt.MaxEventQueue);
        Assert.Equal(1, config.Fire.MaxUpdatesPerFrame);
        Assert.Equal(1, config.Fire.MaxEventQueue);
        Assert.Equal(1, config.Fire.MaxExplosionIgnitedBlocks);
        Assert.Equal(0.5f, config.Fire.MaxAgeSeconds);
        Assert.Equal(0.05f, config.Fire.SpreadIntervalSeconds);
        Assert.Equal(0f, config.Fire.SpreadChance);
        Assert.Equal(0f, config.Fire.BurnStartSeconds);
        Assert.Equal(0f, config.Fire.BurnChance);
        Assert.Equal(1f, config.Fire.ExplosionIgniteRadius);
        Assert.Equal(0f, config.Fire.ExplosionIgniteChance);
        Assert.Equal(0f, config.Fire.RainExtinguishMultiplier);
        Assert.Equal(1, config.SaveSettings.MaxBlockChangesPerFrame);
    }

    [Fact]
    public void LoadOrCreate_ReadsPartialLegacyJson_AndKeepsDefaultsForMissingSections()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_config_legacy_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            string path = Path.Combine(root, "config.json");
            File.WriteAllText(path, """
            {
              "ChunkRadius": 10,
              "Language": "en",
              "Save": {
                "EnableAutoSave": false
              },
              "WorldGen": {
                "BaseHeight": 58,
                "ForcedBiome": "Any"
              }
            }
            """);

            var loaded = GameConfig.LoadOrCreate(path);

            Assert.Equal(10, loaded.ChunkRadius);
            Assert.Equal("en", loaded.Language);
            Assert.False(loaded.SaveSettings.EnableAutoSave);
            Assert.Equal(30f, loaded.SaveSettings.AutoSaveIntervalSeconds);
            Assert.Equal(100, loaded.WindowX);
            Assert.Equal(100, loaded.WindowY);
            Assert.Equal(58, loaded.WorldGen.BaseHeight);
            Assert.Equal("Any", loaded.WorldGen.ForcedBiome);
            Assert.NotNull(loaded.Physics);
            Assert.NotNull(loaded.Render);
            Assert.NotNull(loaded.Ui);
            Assert.NotNull(loaded.Audio);
            Assert.NotNull(loaded.Spawn);
            Assert.NotNull(loaded.DayNight);
            Assert.NotNull(loaded.Weather);
            Assert.NotNull(loaded.Mob);
            Assert.NotNull(loaded.Tnt);
            Assert.NotNull(loaded.Fire);
            Assert.NotNull(loaded.CameraMotion);
            Assert.NotNull(loaded.Herobrine);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void LoadOrCreate_FallsBackToDefaults_WhenJsonIsInvalid()
    {
        string root = Path.Combine(Path.GetTempPath(), $"minicruft_config_invalid_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            string path = Path.Combine(root, "config.json");
            File.WriteAllText(path, "{ invalid json");

            var loaded = GameConfig.LoadOrCreate(path);

            Assert.Equal(8, loaded.ChunkRadius);
            Assert.True(loaded.VSync);
            Assert.NotNull(loaded.SaveSettings);
            Assert.NotNull(loaded.WorldGen);
            Assert.NotNull(loaded.Tnt);
            Assert.NotNull(loaded.Fire);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Normalize_ClampsUpperBoundsForParticlesAndFire()
    {
        var config = new GameConfig
        {
            Particles =
            {
                MaxParticles = 999999
            },
            Fire =
            {
                MaxUpdatesPerFrame = 999999,
                MaxEventQueue = 999999,
                MaxExplosionIgnitedBlocks = 999999
            }
        };

        config.Normalize();

        Assert.Equal(4096, config.Particles.MaxParticles);
        Assert.Equal(256, config.Fire.MaxUpdatesPerFrame);
        Assert.Equal(4096, config.Fire.MaxEventQueue);
        Assert.Equal(256, config.Fire.MaxExplosionIgnitedBlocks);
    }

    [Fact]
    public void Normalize_ClampsCameraSettingsToSupportedRange()
    {
        var config = new GameConfig
        {
            FieldOfView = 5f,
            MouseSensitivity = 0f
        };

        config.Normalize();

        Assert.Equal(GameConfig.MinFieldOfView, config.FieldOfView);
        Assert.Equal(GameConfig.MinMouseSensitivity, config.MouseSensitivity);

        config.FieldOfView = 200f;
        config.MouseSensitivity = 2f;
        config.Normalize();

        Assert.Equal(GameConfig.MaxFieldOfView, config.FieldOfView);
        Assert.Equal(GameConfig.MaxMouseSensitivity, config.MouseSensitivity);
    }
}
