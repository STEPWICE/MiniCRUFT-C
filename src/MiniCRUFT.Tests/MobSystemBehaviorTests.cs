using System.Collections.Generic;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.Game;
using MiniCRUFT.IO;
using MiniCRUFT.World;
using Xunit;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Tests;

public sealed class MobSystemBehaviorTests
{
    [Fact]
    public void PassiveMob_FleesPlayer_WhenWithinAlertRadius()
    {
        WorldType world = CreateFlatWorld();
        var editor = new WorldEditor(world, new WorldChangeQueue());
        var player = new Player(new Vector3(8.5f, 65f, 8.5f), new PhysicsConfig());
        var system = new MobSystem(1234, CreateMobConfig());
        system.Load(new[]
        {
            new MobSaveData(
                MobType.Cow,
                new Vector3(6.5f, 65f, 8.5f),
                Vector3.Zero,
                new Vector3(6.5f, 65f, 8.5f),
                0f,
                0f,
                10,
                0f,
                0f,
                0f,
                0f,
                0f,
                false,
                true,
                0f,
                0f)
        });

        float startX = system.BuildSaveData()[0].Position.X;

        for (int i = 0; i < 20; i++)
        {
            system.Update(0.1f, world, editor, player, 0.8f);
        }

        Vector3 endPosition = system.BuildSaveData()[0].Position;
        Assert.True(endPosition.X < startX - 0.5f);
        Assert.True(Vector3.DistanceSquared(endPosition, player.Position) > Vector3.DistanceSquared(new Vector3(startX, 65f, 8.5f), player.Position));
    }

    [Fact]
    public void HostileMob_SteersAroundWall_WhenChasingPlayer()
    {
        WorldType world = CreateFlatWorld(withWall: true);
        var editor = new WorldEditor(world, new WorldChangeQueue());
        var player = new Player(new Vector3(12.5f, 65f, 8.5f), new PhysicsConfig());
        var system = new MobSystem(5678, CreateMobConfig());
        system.Load(new[]
        {
            new MobSaveData(
                MobType.Zombie,
                new Vector3(6.5f, 65f, 8.5f),
                Vector3.Zero,
                new Vector3(6.5f, 65f, 8.5f),
                0f,
                0f,
                20,
                0f,
                0f,
                0f,
                0f,
                0f,
                false,
                true,
                0f,
                0f)
        });

        float startZ = system.BuildSaveData()[0].Position.Z;

        for (int i = 0; i < 15; i++)
        {
            system.Update(0.1f, world, editor, player, 0f);
        }

        Vector3 endPosition = system.BuildSaveData()[0].Position;
        Assert.True(endPosition.X > 6.5f, $"Expected X to increase past the start position, but got {endPosition}.");
        Assert.True(MathF.Abs(endPosition.Z - startZ) > 0.05f, $"Expected Z to change while steering, but startZ={startZ}, endZ={endPosition.Z}.");
    }

    [Fact]
    public void PassiveMob_DoesNotHopOnFlatGround()
    {
        WorldType world = CreateFlatWorld();
        var editor = new WorldEditor(world, new WorldChangeQueue());
        var player = new Player(new Vector3(8.5f, 65f, 8.5f), new PhysicsConfig());
        var system = new MobSystem(2468, CreateMobConfig());
        system.Load(new[]
        {
            new MobSaveData(
                MobType.Cow,
                new Vector3(6.5f, 65f, 8.5f),
                Vector3.Zero,
                new Vector3(6.5f, 65f, 8.5f),
                0f,
                0f,
                10,
                0f,
                0f,
                0f,
                0f,
                0f,
                false,
                true,
                0f,
                0f)
        });

        float startY = system.BuildSaveData()[0].Position.Y;

        for (int i = 0; i < 30; i++)
        {
            system.Update(0.1f, world, editor, player, 0.8f);
        }

        Vector3 endPosition = system.BuildSaveData()[0].Position;
        Assert.InRange(endPosition.Y, startY - 0.05f, startY + 0.05f);
    }

    [Fact]
    public void PassiveMob_FleesNearbyHostile_WhenPlayerIsFarAway()
    {
        WorldType world = CreateFlatWorld();
        var editor = new WorldEditor(world, new WorldChangeQueue());
        var player = new Player(new Vector3(30.5f, 65f, 8.5f), new PhysicsConfig());
        var system = new MobSystem(9753, CreateMobConfig());
        system.Load(new[]
        {
            new MobSaveData(
                MobType.Cow,
                new Vector3(6.5f, 65f, 8.5f),
                Vector3.Zero,
                new Vector3(6.5f, 65f, 8.5f),
                0f,
                0f,
                10,
                0f,
                0f,
                0f,
                0f,
                0f,
                false,
                true,
                0f,
                0f),
            new MobSaveData(
                MobType.Zombie,
                new Vector3(8.5f, 65f, 8.5f),
                Vector3.Zero,
                new Vector3(8.5f, 65f, 8.5f),
                0f,
                0f,
                20,
                0f,
                0f,
                0f,
                0f,
                0f,
                false,
                true,
                0f,
                0f)
        });

        Assert.True(system.TryGetMobSnapshot(MobType.Cow, out var start));

        for (int i = 0; i < 18; i++)
        {
            system.Update(0.1f, world, editor, player, 0f);
        }

        Assert.True(system.TryGetMobSnapshot(MobType.Cow, out var end));
        Assert.True(end.Position.X < start.Position.X - 0.15f, $"Expected cow to flee left, but start={start.Position}, end={end.Position}.");
        Assert.True(Vector3.DistanceSquared(end.Position, new Vector3(8.5f, 65f, 8.5f)) > Vector3.DistanceSquared(start.Position, new Vector3(8.5f, 65f, 8.5f)));
    }

    [Fact]
    public void Creeper_FusesAndExplodes_WhenPlayerIsNearby()
    {
        WorldType world = CreateFlatWorld();
        world.SetBlock(9, 65, 8, BlockId.Stone);

        var editor = new WorldEditor(world, new WorldChangeQueue());
        var player = new Player(new Vector3(10.5f, 65f, 8.5f), new PhysicsConfig());
        var system = new MobSystem(24680, CreateMobConfig());
        system.Load(new[]
        {
            new MobSaveData(
                MobType.Creeper,
                new Vector3(8.5f, 65f, 8.5f),
                Vector3.Zero,
                new Vector3(8.5f, 65f, 8.5f),
                0f,
                0f,
                20,
                0f,
                0f,
                0f,
                0f,
                0f,
                false,
                true,
                0f,
                0f)
        });

        int explosionEvents = 0;
        int deathEvents = 0;
        for (int i = 0; i < 30; i++)
        {
            system.Update(0.1f, world, editor, player, 0f);
            while (system.TryDequeueEvent(out var ev))
            {
                if (ev.Type != MobType.Creeper)
                {
                    continue;
                }

                if (ev.Kind == MobEventKind.Explosion)
                {
                    explosionEvents++;
                }
                else if (ev.Kind == MobEventKind.Death)
                {
                    deathEvents++;
                }
            }

            if (explosionEvents > 0)
            {
                break;
            }
        }

        Assert.True(explosionEvents > 0, "Expected the creeper to explode once the fuse completed.");
        Assert.True(deathEvents > 0, "Expected the creeper to die after exploding.");
        Assert.True(player.Health < player.MaxHealth, "Expected the creeper explosion to damage the player.");
        Assert.Equal(BlockId.Air, world.GetBlock(9, 65, 8));
    }

    [Fact]
    public void Creeper_DoesNotFuse_WhenLineOfSightIsBlocked()
    {
        WorldType world = CreateFlatWorld();
        for (int z = 0; z < Chunk.SizeZ; z++)
        {
            world.SetBlock(9, 65, z, BlockId.Stone);
            world.SetBlock(9, 66, z, BlockId.Stone);
        }

        var editor = new WorldEditor(world, new WorldChangeQueue());
        var player = new Player(new Vector3(10.5f, 65f, 8.5f), new PhysicsConfig());
        var system = new MobSystem(24681, CreateMobConfig());
        system.Load(new[]
        {
            new MobSaveData(
                MobType.Creeper,
                new Vector3(8.5f, 65f, 8.5f),
                Vector3.Zero,
                new Vector3(8.5f, 65f, 8.5f),
                0f,
                0f,
                20,
                0f,
                0f,
                0f,
                0f,
                0f,
                false,
                true,
                0f,
                0f)
        });

        int explosionEvents = 0;
        for (int i = 0; i < 24; i++)
        {
            system.Update(0.1f, world, editor, player, 0f);
            while (system.TryDequeueEvent(out var ev))
            {
                if (ev.Type == MobType.Creeper && ev.Kind == MobEventKind.Explosion)
                {
                    explosionEvents++;
                }
            }
        }

        Assert.Equal(0, explosionEvents);
        Assert.True(system.HasMobOfType(MobType.Creeper));
        Assert.False(system.BuildSaveData()[0].SpecialActive);
    }

    [Fact]
    public void Creeper_CancelsFuse_WhenPlayerMovesBeyondSevenBlocks()
    {
        WorldType world = CreateFlatWorld();
        var editor = new WorldEditor(world, new WorldChangeQueue());
        var player = new Player(new Vector3(10.5f, 65f, 8.5f), new PhysicsConfig());
        var system = new MobSystem(24682, CreateMobConfig());
        system.Load(new[]
        {
            new MobSaveData(
                MobType.Creeper,
                new Vector3(8.5f, 65f, 8.5f),
                Vector3.Zero,
                new Vector3(8.5f, 65f, 8.5f),
                0f,
                0f,
                20,
                0f,
                0f,
                0f,
                0f,
                0f,
                false,
                true,
                0f,
                0f)
        });

        bool fuseStarted = false;
        for (int i = 0; i < 10; i++)
        {
            system.Update(0.1f, world, editor, player, 0f);
            if (system.BuildSaveData()[0].SpecialActive)
            {
                fuseStarted = true;
                break;
            }
        }

        Assert.True(fuseStarted, "Expected the creeper to start its fuse while the player was nearby.");

        player.Position = new Vector3(22.5f, 65f, 8.5f);

        int explosionEvents = 0;
        for (int i = 0; i < 30; i++)
        {
            system.Update(0.1f, world, editor, player, 0f);
            while (system.TryDequeueEvent(out var ev))
            {
                if (ev.Type == MobType.Creeper && ev.Kind == MobEventKind.Explosion)
                {
                    explosionEvents++;
                }
            }
        }

        Assert.Equal(0, explosionEvents);
        Assert.True(system.HasMobOfType(MobType.Creeper));
        Assert.False(system.BuildSaveData()[0].SpecialActive);
    }

    [Fact]
    public void CreeperExplosion_PrimesNearbyTnt()
    {
        WorldType world = CreateFlatWorld();
        world.SetBlock(9, 65, 8, BlockId.Tnt);

        var tntConfig = new TntConfig
        {
            Enabled = true,
            PrimeOnPlace = true,
            FuseSeconds = 4f,
            ChainReactionFuseSeconds = 0.35f,
            ExplosionRadius = 4.5f,
            ExplosionDamage = 20,
            KnockbackStrength = 8f,
            ResistanceScale = 0.35f,
            MaxAffectedBlocks = 512
        };

        var tntSystem = new TntSystem(tntConfig);
        var editor = new WorldEditor(world, new WorldChangeQueue());
        var player = new Player(new Vector3(10.5f, 65f, 8.5f), new PhysicsConfig());
        var system = new MobSystem(13579, CreateMobConfig());
        system.Load(new[]
        {
            new MobSaveData(
                MobType.Creeper,
                new Vector3(8.5f, 65f, 8.5f),
                Vector3.Zero,
                new Vector3(8.5f, 65f, 8.5f),
                0f,
                0f,
                20,
                0f,
                0f,
                0f,
                0f,
                0f,
                false,
                true,
                0f,
                0f)
        });

        for (int i = 0; i < 30; i++)
        {
            system.Update(
                0.1f,
                world,
                editor,
                player,
                0f,
                0f,
                primeTnt: block => tntSystem.Prime(world, block, tntConfig.ChainReactionFuseSeconds, emitEvent: false));
        }

        var renderInstances = new List<TntRenderInstance>();
        tntSystem.FillRenderInstances(renderInstances);

        Assert.Single(renderInstances);
        Assert.Equal(BlockId.Tnt, world.GetBlock(9, 65, 8));
    }

    [Fact]
    public void Load_PreservesHerobrineEvenWhenMaxAliveIsReached()
    {
        var system = new MobSystem(1357, new MobConfig
        {
            SpawnAttemptsPerTick = 0,
            SpawnIntervalSeconds = 999f,
            DespawnRadius = 256,
            MaxAlive = 1
        });

        system.Load(new[]
        {
            new MobSaveData(
                MobType.Cow,
                new Vector3(6.5f, 65f, 8.5f),
                Vector3.Zero,
                new Vector3(6.5f, 65f, 8.5f),
                0f,
                0f,
                10,
                0f,
                0f,
                0f,
                0f,
                0f,
                false,
                true,
                0f,
                0f),
            new MobSaveData(
                MobType.Herobrine,
                new Vector3(9.5f, 65f, 8.5f),
                Vector3.Zero,
                new Vector3(9.5f, 65f, 8.5f),
                0f,
                0f,
                20,
                0f,
                0f,
                0f,
                0f,
                0f,
                false,
                true,
                0f,
                0f)
        });

        Assert.True(system.HasMobOfType(MobType.Cow));
        Assert.True(system.HasMobOfType(MobType.Herobrine));
        Assert.Equal(2, system.BuildSaveData().Count);
    }

    [Fact]
    public void Load_PreservesEliteState_AndBoostsMobHealth()
    {
        var system = new MobSystem(2468, CreateMobConfig());

        system.Load(new[]
        {
            new MobSaveData(
                MobType.Zombie,
                new Vector3(6.5f, 65f, 8.5f),
                Vector3.Zero,
                new Vector3(6.5f, 65f, 8.5f),
                0f,
                0f,
                99,
                0f,
                0f,
                0f,
                0f,
                0f,
                false,
                true,
                0f,
                0f,
                elite: true)
        });

        Assert.True(system.TryGetMobSnapshot(MobType.Zombie, out var snapshot));
        Assert.True(snapshot.Elite);
        Assert.Equal(36, snapshot.MaxHealth);
        Assert.Equal(36, snapshot.Health);
        Assert.True(system.BuildSaveData()[0].Elite);
    }

    [Fact]
    public void Load_PreservesEliteVariant_AndBoostsMobHealth()
    {
        var system = new MobSystem(2468, CreateMobConfig());

        system.Load(new[]
        {
            new MobSaveData(
                MobType.Zombie,
                new Vector3(6.5f, 65f, 8.5f),
                Vector3.Zero,
                new Vector3(6.5f, 65f, 8.5f),
                0f,
                0f,
                99,
                0f,
                0f,
                0f,
                0f,
                0f,
                false,
                true,
                0f,
                0f,
                elite: true,
                eliteVariant: EliteMobVariant.Warden)
        });

        Assert.True(system.TryGetMobSnapshot(MobType.Zombie, out var snapshot));
        Assert.True(snapshot.Elite);
        Assert.Equal(EliteMobVariant.Warden, snapshot.EliteVariant);
        Assert.Equal(61, snapshot.MaxHealth);
        Assert.Equal(61, snapshot.Health);
        Assert.Equal(EliteMobVariant.Warden, system.BuildSaveData()[0].EliteVariant);
    }

    [Fact]
    public void DamageResponse_StaggersMob_AndUpdatesSnapshot()
    {
        var system = new MobSystem(8642, CreateMobConfig());

        system.Load(new[]
        {
            new MobSaveData(
                MobType.Zombie,
                new Vector3(6.5f, 65f, 8.5f),
                Vector3.Zero,
                new Vector3(6.5f, 65f, 8.5f),
                0f,
                0f,
                10,
                0f,
                0f,
                0f,
                0f,
                0f,
                false,
                true,
                0f,
                0f)
        });

        var hit = new MobHitResult(true, 0, MobType.Zombie, new Vector3(6.5f, 65f, 8.5f), 1f);

        Assert.True(system.TryDamageMob(hit, 1, new Vector3(10.5f, 65f, 8.5f)));
        Assert.True(system.TryGetMobSnapshot(MobType.Zombie, out var snapshot));
        Assert.True(snapshot.HurtFlash > 0f);
        Assert.True(snapshot.StaggerProgress > 0.9f);
    }

    private static WorldType CreateFlatWorld(bool withWall = false)
    {
        var world = new WorldType(1337, new WorldGenSettings());
        var chunk = new Chunk(0, 0);

        for (int x = 0; x < Chunk.SizeX; x++)
        {
            for (int z = 0; z < Chunk.SizeZ; z++)
            {
                chunk.SetBlock(x, 64, z, BlockId.Stone);
            }
        }

        if (withWall)
        {
            for (int z = 0; z < Chunk.SizeZ; z++)
            {
                for (int y = 65; y <= 67; y++)
                {
                    chunk.SetBlock(9, y, z, BlockId.Stone);
                }
            }
        }

        world.SetChunk(chunk);
        return world;
    }

    private static MobConfig CreateMobConfig()
    {
        return new MobConfig
        {
            SpawnAttemptsPerTick = 0,
            SpawnIntervalSeconds = 999f,
            DespawnRadius = 256,
            MaxAlive = 8
        };
    }
}
