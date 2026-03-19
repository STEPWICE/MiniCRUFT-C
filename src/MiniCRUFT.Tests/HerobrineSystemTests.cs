using System;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.Game;
using MiniCRUFT.IO;
using MiniCRUFT.Renderer;
using MiniCRUFT.World;
using Xunit;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Tests;

public sealed class HerobrineSystemTests
{
    [Fact]
    public void Herobrine_Manifests_WhenCooldownExpires()
    {
        WorldType world = CreateFlatWorld();
        var editor = new WorldEditor(world, new WorldChangeQueue());
        var player = new Player(new Vector3(32.5f, 65f, 32.5f), new PhysicsConfig());
        var camera = CreateCamera(player.Position, yawDegrees: 180f);
        var mobSystem = new MobSystem(1234, CreateMobConfig());
        var herobrine = new HerobrineSystem(1234, CreateHerobrineConfig());

        herobrine.Load(new HerobrineSaveData(1234, Vector3.Zero, player.Position, 0f, 0f, 0f, 0f, 0f, 0, false));
        herobrine.Update(0.1f, world, editor, mobSystem, player, camera, sunIntensity: 0.2f);

        Assert.True(mobSystem.HasMobOfType(MobType.Herobrine));
    }

    [Fact]
    public void Herobrine_Despawns_WhenPlayerLooksAtHimLongEnough()
    {
        WorldType world = CreateFlatWorld();
        var editor = new WorldEditor(world, new WorldChangeQueue());
        var player = new Player(new Vector3(32.5f, 65f, 32.5f), new PhysicsConfig());
        var mobSystem = new MobSystem(4321, CreateMobConfig());
        var herobrineConfig = CreateHerobrineConfig();
        var herobrine = new HerobrineSystem(4321, herobrineConfig);
        var camera = CreateCamera(player.Position, yawDegrees: 180f);

        herobrine.Load(new HerobrineSaveData(4321, Vector3.Zero, player.Position, 0f, 0f, 0f, 0f, 0f, 0, false));
        herobrine.Update(0.1f, world, editor, mobSystem, player, camera, sunIntensity: 0.1f);

        Assert.True(mobSystem.TryGetMobSnapshot(MobType.Herobrine, out MobRenderInstance snapshot));

        camera.Position = player.Position + new Vector3(0f, 1.62f, 0f);
        PointCameraAt(camera, snapshot.Position + new Vector3(0f, snapshot.Height * 0.7f, 0f));

        for (int i = 0; i < 12; i++)
        {
            herobrine.Update(0.1f, world, editor, mobSystem, player, camera, sunIntensity: 0.1f);
        }

        Assert.False(mobSystem.HasMobOfType(MobType.Herobrine));
    }

    [Fact]
    public void Herobrine_Disable_RemovesActiveManifestationImmediately()
    {
        WorldType world = CreateFlatWorld();
        var editor = new WorldEditor(world, new WorldChangeQueue());
        var player = new Player(new Vector3(32.5f, 65f, 32.5f), new PhysicsConfig());
        var config = CreateHerobrineConfig();
        var herobrine = new HerobrineSystem(9876, config);
        var mobSystem = new MobSystem(9876, CreateMobConfig());
        var camera = CreateCamera(player.Position, yawDegrees: 180f);

        herobrine.Load(new HerobrineSaveData(9876, Vector3.Zero, player.Position, 0f, 0f, 0f, 0f, 0f, 0, false));
        herobrine.Update(0.1f, world, editor, mobSystem, player, camera, sunIntensity: 0.2f);
        Assert.True(mobSystem.HasMobOfType(MobType.Herobrine));

        config.Enabled = false;
        herobrine.Update(0.1f, world, editor, mobSystem, player, camera, sunIntensity: 0.2f);

        Assert.False(mobSystem.HasMobOfType(MobType.Herobrine));
    }

    [Fact]
    public void Herobrine_DebugStatus_ReportsSelectedMode()
    {
        var config = CreateHerobrineConfig();
        config.Mode = HerobrineModeCatalog.Haunt;

        var herobrine = new HerobrineSystem(2468, config);

        Assert.Contains("mode=Haunt", herobrine.BuildDebugStatus());
    }

    [Fact]
    public void Herobrine_Rejects_SaveData_FromDifferentWorldSeed()
    {
        var config = CreateHerobrineConfig();
        var herobrine = new HerobrineSystem(2468, config);

        herobrine.Load(new HerobrineSaveData(
            seed: 9999,
            lastManifestPosition: new Vector3(1f, 2f, 3f),
            lastObservedPlayerPosition: new Vector3(4f, 5f, 6f),
            hauntPressure: 0.9f,
            manifestCooldown: 0f,
            eventCooldown: 0f,
            worldEffectCooldown: 0f,
            activeTimer: 3f,
            encounterCount: 2,
            isManifested: true));

        Assert.False(herobrine.IsManifested);
        Assert.Equal(0f, herobrine.HauntPressure);
        Assert.Contains("mode=Classic", herobrine.BuildDebugStatus());
    }

    private static Camera CreateCamera(Vector3 playerPosition, float yawDegrees)
    {
        return new Camera
        {
            Position = playerPosition + new Vector3(0f, 1.62f, 0f),
            Yaw = yawDegrees,
            Pitch = 0f
        };
    }

    private static void PointCameraAt(Camera camera, Vector3 target)
    {
        Vector3 delta = target - camera.Position;
        float horizontal = MathF.Sqrt(delta.X * delta.X + delta.Z * delta.Z);
        camera.Yaw = MathF.Atan2(delta.Z, delta.X) * (180f / MathF.PI);
        camera.Pitch = MathF.Atan2(delta.Y, horizontal) * (180f / MathF.PI);
    }

    private static WorldType CreateFlatWorld()
    {
        var settings = new WorldGenSettings();
        var world = new WorldType(1337, settings);

        for (int chunkZ = -4; chunkZ <= 4; chunkZ++)
        {
            for (int chunkX = -4; chunkX <= 4; chunkX++)
            {
                var chunk = new Chunk(chunkX, chunkZ);
                for (int x = 0; x < Chunk.SizeX; x++)
                {
                    for (int z = 0; z < Chunk.SizeZ; z++)
                    {
                        chunk.SetBlock(x, 64, z, BlockId.Stone);
                    }
                }

                world.SetChunk(chunk);
            }
        }

        return world;
    }

    private static MobConfig CreateMobConfig()
    {
        return new MobConfig
        {
            SpawnAttemptsPerTick = 1,
            SpawnIntervalSeconds = 999f,
            DespawnRadius = 256,
            MaxAlive = 16
        };
    }

    private static HerobrineConfig CreateHerobrineConfig()
    {
        return new HerobrineConfig
        {
            Enabled = true,
            MinManifestIntervalSeconds = 5f,
            MaxManifestIntervalSeconds = 6f,
            ManifestDurationSeconds = 12f,
            EventCooldownSeconds = 60f,
            WorldEffectCooldownSeconds = 60f,
            MinManifestDistance = 16f,
            MaxManifestDistance = 22f,
            BehindPlayerChance = 1f,
            DirectLookDespawnSeconds = 0.35f,
            HiddenTimeoutSeconds = 6f,
            WorldEffectIntensity = 0f
        };
    }
}
