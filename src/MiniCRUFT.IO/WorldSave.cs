using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.IO;

public static class WorldSave
{
    private static readonly byte[] PlayerMagic = { (byte)'M', (byte)'C', (byte)'P', (byte)'S' };
    private static readonly byte[] MobMagic = { (byte)'M', (byte)'C', (byte)'M', (byte)'B' };
    private static readonly byte[] TntMagic = { (byte)'M', (byte)'C', (byte)'T', (byte)'N' };
    private static readonly byte[] HerobrineMagic = { (byte)'M', (byte)'C', (byte)'H', (byte)'B' };
    private static readonly byte[] DayNightMagic = { (byte)'M', (byte)'C', (byte)'D', (byte)'N' };
    private static readonly byte[] WeatherMagic = { (byte)'M', (byte)'C', (byte)'W', (byte)'E' };
    private static readonly byte[] HungerMagic = { (byte)'M', (byte)'C', (byte)'H', (byte)'G' };
    private const int LegacyBinarySeedBytes = sizeof(int);
    private const int PlayerVersion = 4;
    private const int MobVersion = 3;
    private const int TntVersion = 2;
    private const int HerobrineVersion = 1;
    private const int DayNightVersion = 1;
    private const int WeatherVersion = 1;
    private const int HungerVersion = 1;

    public static void SaveSeed(string worldPath, int seed)
    {
        Directory.CreateDirectory(worldPath);
        File.WriteAllText(Path.Combine(worldPath, "seed.dat"), seed.ToString());
    }

    public static int LoadSeed(string worldPath, int fallback)
    {
        string path = Path.Combine(worldPath, "seed.dat");
        if (!File.Exists(path))
        {
            return fallback;
        }

        try
        {
            string text = File.ReadAllText(path);
            if (int.TryParse(text, out int seed))
            {
                return seed;
            }

            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length == LegacyBinarySeedBytes)
            {
                using var reader = new BinaryReader(stream);
                return reader.ReadInt32();
            }
        }
        catch (IOException ex)
        {
            Log.Warn($"Failed to load seed file {path}: {ex.Message}");
            return fallback;
        }

        Log.Warn($"Seed file {path} is not valid text or legacy binary data, using fallback.");
        return fallback;
    }

    public static void SaveDayNight(string worldPath, DayNightSaveData data)
    {
        Directory.CreateDirectory(worldPath);
        string path = Path.Combine(worldPath, "daynight.dat");

        using var writer = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write));
        writer.Write(DayNightMagic);
        writer.Write(DayNightVersion);
        writer.Write(data.TimeOfDay);
        writer.Write(data.DayCount);
    }

    public static DayNightSaveData LoadDayNight(string worldPath, DayNightSaveData fallback)
    {
        string path = Path.Combine(worldPath, "daynight.dat");
        if (!File.Exists(path))
        {
            return fallback;
        }

        try
        {
            using var reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));
            if (!TryReadMagic(reader, DayNightMagic, out bool hasMagic) || !hasMagic)
            {
                Log.Warn($"Day/night save {path} is not a supported format, using fallback.");
                return fallback;
            }

            int version = reader.ReadInt32();
            if (version != DayNightVersion)
            {
                Log.Warn($"Day/night save {path} has unsupported version {version}, using fallback.");
                return fallback;
            }

            float timeOfDay = Math.Clamp(reader.ReadSingle(), 0f, 1f);
            long dayCount = Math.Max(0L, reader.ReadInt64());
            return new DayNightSaveData(timeOfDay, dayCount);
        }
        catch (EndOfStreamException)
        {
            Log.Warn($"Day/night save {path} ended unexpectedly, using fallback.");
            return fallback;
        }
        catch (IOException ex)
        {
            Log.Warn($"Failed to load day/night save {path}: {ex.Message}");
            return fallback;
        }
    }

    public static void SaveWeather(string worldPath, WeatherSaveData data)
    {
        Directory.CreateDirectory(worldPath);
        string path = Path.Combine(worldPath, "weather.dat");

        using var writer = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write));
        writer.Write(WeatherMagic);
        writer.Write(WeatherVersion);
        writer.Write(data.TargetRaining);
        writer.Write(data.RainIntensity);
        writer.Write(data.Timer);
    }

    public static WeatherSaveData LoadWeather(string worldPath, WeatherSaveData fallback)
    {
        string path = Path.Combine(worldPath, "weather.dat");
        if (!File.Exists(path))
        {
            return fallback;
        }

        try
        {
            using var reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));
            if (!TryReadMagic(reader, WeatherMagic, out bool hasMagic) || !hasMagic)
            {
                Log.Warn($"Weather save {path} is not a supported format, using fallback.");
                return fallback;
            }

            int version = reader.ReadInt32();
            if (version != WeatherVersion)
            {
                Log.Warn($"Weather save {path} has unsupported version {version}, using fallback.");
                return fallback;
            }

            bool targetRaining = reader.ReadBoolean();
            float rainIntensity = Math.Clamp(reader.ReadSingle(), 0f, 1f);
            float timer = Math.Max(0f, reader.ReadSingle());
            return new WeatherSaveData(targetRaining, rainIntensity, timer);
        }
        catch (EndOfStreamException)
        {
            Log.Warn($"Weather save {path} ended unexpectedly, using fallback.");
            return fallback;
        }
        catch (IOException ex)
        {
            Log.Warn($"Failed to load weather save {path}: {ex.Message}");
            return fallback;
        }
    }

    public static void SaveHunger(string worldPath, HungerSaveData data)
    {
        Directory.CreateDirectory(worldPath);
        string path = Path.Combine(worldPath, "hunger.dat");

        using var writer = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write));
        writer.Write(HungerMagic);
        writer.Write(HungerVersion);
        writer.Write(data.Hunger);
        writer.Write(data.StarvationTimer);
    }

    public static HungerSaveData LoadHunger(string worldPath, HungerSaveData fallback)
    {
        string path = Path.Combine(worldPath, "hunger.dat");
        if (!File.Exists(path))
        {
            return fallback;
        }

        try
        {
            using var reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));
            if (!TryReadMagic(reader, HungerMagic, out bool hasMagic) || !hasMagic)
            {
                Log.Warn($"Hunger save {path} is not a supported format, using fallback.");
                return fallback;
            }

            int version = reader.ReadInt32();
            if (version != HungerVersion)
            {
                Log.Warn($"Hunger save {path} has unsupported version {version}, using fallback.");
                return fallback;
            }

            float hunger = Math.Max(0f, reader.ReadSingle());
            float starvationTimer = Math.Max(0f, reader.ReadSingle());
            return new HungerSaveData(hunger, starvationTimer);
        }
        catch (EndOfStreamException)
        {
            Log.Warn($"Hunger save {path} ended unexpectedly, using fallback.");
            return fallback;
        }
        catch (IOException ex)
        {
            Log.Warn($"Failed to load hunger save {path}: {ex.Message}");
            return fallback;
        }
    }

    public static void SavePlayer(string worldPath, PlayerSaveData data)
    {
        Directory.CreateDirectory(worldPath);
        string path = Path.Combine(worldPath, "player.dat");
        using var writer = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write));
        writer.Write(PlayerMagic);
        writer.Write(PlayerVersion);
        writer.Write(data.Position.X);
        writer.Write(data.Position.Y);
        writer.Write(data.Position.Z);

        writer.Write(data.Hotbar.Length);
        for (int i = 0; i < data.Hotbar.Length; i++)
        {
            writer.Write((byte)data.Hotbar[i]);
        }

        for (int i = 0; i < data.Hotbar.Length; i++)
        {
            int count = data.Counts != null && i < data.Counts.Length
                ? data.Counts[i]
                : BlockStackDefaults.GetDefaultCount(data.Hotbar[i]);
            writer.Write(Math.Max(0, count));
        }

        writer.Write(data.Hotbar.Length);
        for (int i = 0; i < data.Hotbar.Length; i++)
        {
            int durability = data.ToolDurability != null && i < data.ToolDurability.Length
                ? data.ToolDurability[i]
                : -1;
            writer.Write(durability);
        }
        writer.Write(data.SelectedIndex);
    }

    public static PlayerSaveData LoadPlayer(string worldPath, PlayerSaveData fallback)
    {
        string path = Path.Combine(worldPath, "player.dat");
        if (!File.Exists(path))
        {
            return fallback;
        }

        try
        {
            using var reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read));
            if (TryReadMagic(reader, PlayerMagic, out bool hasMagic) && hasMagic)
            {
                int version = reader.ReadInt32();
                if (version < 2 || version > PlayerVersion)
                {
                    Log.Warn($"Player save has unsupported version {version}, using fallback.");
                    return fallback;
                }

                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                float z = reader.ReadSingle();
                int hotbarCount = reader.ReadInt32();
                if (hotbarCount <= 0 || hotbarCount > 64)
                {
                    Log.Warn($"Player save hotbar size {hotbarCount} invalid, using fallback.");
                    return fallback;
                }

                var hotbar = new BlockId[hotbarCount];
                for (int i = 0; i < hotbarCount; i++)
                {
                    hotbar[i] = (BlockId)reader.ReadByte();
                }

                if (version >= 3)
                {
                    var counts = new int[hotbarCount];
                    for (int i = 0; i < hotbarCount; i++)
                    {
                        counts[i] = Math.Max(0, reader.ReadInt32());
                    }

                    if (version >= 4)
                    {
                        int durabilityCount = reader.ReadInt32();
                        if (durabilityCount <= 0 || durabilityCount > 64)
                        {
                            Log.Warn($"Player save durability array size {durabilityCount} invalid, using fallback.");
                            return fallback;
                        }

                        var durability = new int[durabilityCount];
                        for (int i = 0; i < durabilityCount; i++)
                        {
                            durability[i] = reader.ReadInt32();
                        }

                        int selectedIndex = reader.ReadInt32();
                        if (selectedIndex < 0 || selectedIndex >= hotbarCount)
                        {
                            selectedIndex = 0;
                        }

                        return new PlayerSaveData(new Vector3(x, y, z), hotbar, counts, durability, selectedIndex);
                    }

                    int selected = reader.ReadInt32();
                    if (selected < 0 || selected >= hotbarCount)
                    {
                        selected = 0;
                    }

                    return new PlayerSaveData(new Vector3(x, y, z), hotbar, counts, null, selected);
                }

                if (version == 2)
                {
                    int selected = reader.ReadInt32();
                    if (selected < 0 || selected >= hotbarCount)
                    {
                        selected = 0;
                    }

                    return new PlayerSaveData(new Vector3(x, y, z), hotbar, BlockStackDefaults.CreateDefaultCounts(hotbar), null, selected);
                }

                Log.Warn($"Player save has unsupported version {version}, using fallback.");
                return fallback;
            }

            // legacy format (position only)
            if (reader.BaseStream.Length < sizeof(float) * 3)
            {
                Log.Warn("Player save is too short for legacy format, using fallback.");
                return fallback;
            }

            reader.BaseStream.Position = 0;
            float lx = reader.ReadSingle();
            float ly = reader.ReadSingle();
            float lz = reader.ReadSingle();
            var legacyHotbar = new[] { BlockId.Grass, BlockId.Dirt, BlockId.Stone, BlockId.Wood, BlockId.Planks, BlockId.Glass, BlockId.Sand, BlockId.Torch, BlockId.Tnt };
            return new PlayerSaveData(new Vector3(lx, ly, lz), legacyHotbar, 0);
        }
        catch (EndOfStreamException)
        {
            Log.Warn("Player save ended unexpectedly, using fallback.");
            return fallback;
        }
        catch (IOException ex)
        {
            Log.Warn($"Failed to load player save: {ex.Message}");
            return fallback;
        }
    }

    public static void SaveMobs(string worldPath, IEnumerable<MobSaveData> mobs)
    {
        Directory.CreateDirectory(worldPath);
        string path = Path.Combine(worldPath, "mobs.dat");
        var mobList = mobs as List<MobSaveData> ?? new List<MobSaveData>(mobs);

        using var writer = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write));
        writer.Write(MobMagic);
        writer.Write(MobVersion);
        writer.Write(mobList.Count);

        for (int i = 0; i < mobList.Count; i++)
        {
            var mob = mobList[i];
            writer.Write((byte)mob.Type);
            writer.Write(mob.Position.X);
            writer.Write(mob.Position.Y);
            writer.Write(mob.Position.Z);
            writer.Write(mob.Velocity.X);
            writer.Write(mob.Velocity.Y);
            writer.Write(mob.Velocity.Z);
            writer.Write(mob.HomePosition.X);
            writer.Write(mob.HomePosition.Y);
            writer.Write(mob.HomePosition.Z);
            writer.Write(mob.Yaw);
            writer.Write(mob.WanderAngle);
            writer.Write(mob.Health);
            writer.Write(mob.AttackCooldown);
            writer.Write(mob.WanderTimer);
            writer.Write(mob.IdleTimer);
            writer.Write(mob.HurtTimer);
            writer.Write(mob.SpecialTimer);
            writer.Write(mob.SpecialActive);
            writer.Write(mob.OnGround);
            writer.Write(mob.StepDistance);
            writer.Write(mob.Age);
            writer.Write(mob.Elite);
            writer.Write((int)mob.EliteVariant);
        }
    }

    public static List<MobSaveData> LoadMobs(string worldPath)
    {
        string path = Path.Combine(worldPath, "mobs.dat");
        if (!File.Exists(path))
        {
            return new List<MobSaveData>();
        }

        try
        {
            using var reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));
            if (!TryReadMagic(reader, MobMagic, out bool hasMagic) || !hasMagic)
            {
                Log.Warn($"Mob save {path} is not a supported format, ignoring it.");
                return new List<MobSaveData>();
            }

            int version = reader.ReadInt32();
            if (version > MobVersion)
            {
                Log.Warn($"Mob save {path} has unsupported future version {version}, ignoring it.");
                return new List<MobSaveData>();
            }

            return version switch
            {
                1 => LoadMobsV1(path, reader),
                2 => LoadMobsV2(path, reader),
                3 => LoadMobsV3(path, reader),
                _ => new List<MobSaveData>()
            };
        }
        catch (EndOfStreamException)
        {
            Log.Warn($"Mob save {path} ended unexpectedly, ignoring it.");
            return new List<MobSaveData>();
        }
        catch (IOException ex)
        {
            Log.Warn($"Failed to load mob save {path}: {ex.Message}");
            return new List<MobSaveData>();
        }
    }

    public static void SaveTnt(string worldPath, IEnumerable<TntSaveData> tnts)
    {
        Directory.CreateDirectory(worldPath);
        string path = Path.Combine(worldPath, "tnt.dat");
        var tntList = tnts as List<TntSaveData> ?? new List<TntSaveData>(tnts);

        using var writer = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write));
        writer.Write(TntMagic);
        writer.Write(TntVersion);
        writer.Write(tntList.Count);

        for (int i = 0; i < tntList.Count; i++)
        {
            var tnt = tntList[i];
            writer.Write(tnt.Position.X);
            writer.Write(tnt.Position.Y);
            writer.Write(tnt.Position.Z);
            writer.Write(tnt.FuseRemaining);
            writer.Write(tnt.FuseDuration);
        }
    }

    public static List<TntSaveData> LoadTnt(string worldPath, int maxCount = 4096)
    {
        string path = Path.Combine(worldPath, "tnt.dat");
        if (!File.Exists(path))
        {
            return new List<TntSaveData>();
        }

        try
        {
            using var reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));
            if (!TryReadMagic(reader, TntMagic, out bool hasMagic) || !hasMagic)
            {
                Log.Warn($"Tnt save {path} is not a supported format, ignoring it.");
                return new List<TntSaveData>();
            }

            int version = reader.ReadInt32();
            if (version > TntVersion)
            {
                Log.Warn($"Tnt save {path} has unsupported future version {version}, ignoring it.");
                return new List<TntSaveData>();
            }

            return version switch
            {
                1 => LoadTntV1(path, reader, maxCount),
                2 => LoadTntV2(path, reader, maxCount),
                _ => new List<TntSaveData>()
            };
        }
        catch (EndOfStreamException)
        {
            Log.Warn($"Tnt save {path} ended unexpectedly, ignoring it.");
            return new List<TntSaveData>();
        }
        catch (IOException ex)
        {
            Log.Warn($"Failed to load tnt save {path}: {ex.Message}");
            return new List<TntSaveData>();
        }
    }

    public static void SaveHerobrine(string worldPath, HerobrineSaveData data)
    {
        Directory.CreateDirectory(worldPath);
        string path = Path.Combine(worldPath, "herobrine.dat");

        using var writer = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write));
        writer.Write(HerobrineMagic);
        writer.Write(HerobrineVersion);
        writer.Write(data.Seed);
        writer.Write(data.LastManifestPosition.X);
        writer.Write(data.LastManifestPosition.Y);
        writer.Write(data.LastManifestPosition.Z);
        writer.Write(data.LastObservedPlayerPosition.X);
        writer.Write(data.LastObservedPlayerPosition.Y);
        writer.Write(data.LastObservedPlayerPosition.Z);
        writer.Write(data.HauntPressure);
        writer.Write(data.ManifestCooldown);
        writer.Write(data.EventCooldown);
        writer.Write(data.WorldEffectCooldown);
        writer.Write(data.ActiveTimer);
        writer.Write(data.EncounterCount);
        writer.Write(data.IsManifested);
    }

    public static HerobrineSaveData LoadHerobrine(string worldPath, HerobrineSaveData fallback)
    {
        string path = Path.Combine(worldPath, "herobrine.dat");
        if (!File.Exists(path))
        {
            return fallback;
        }

        try
        {
            using var reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));
            if (!TryReadMagic(reader, HerobrineMagic, out bool hasMagic) || !hasMagic)
            {
                Log.Warn($"Herobrine save {path} is not a supported format, using fallback.");
                return fallback;
            }

            int version = reader.ReadInt32();
            if (version > HerobrineVersion)
            {
                Log.Warn($"Herobrine save {path} has unsupported future version {version}, using fallback.");
                return fallback;
            }

            return version switch
            {
                1 => LoadHerobrineV1(path, reader, fallback),
                _ => fallback
            };
        }
        catch (EndOfStreamException)
        {
            Log.Warn($"Herobrine save {path} ended unexpectedly, using fallback.");
            return fallback;
        }
        catch (IOException ex)
        {
            Log.Warn($"Failed to load Herobrine save {path}: {ex.Message}");
            return fallback;
        }
    }

    private static List<MobSaveData> LoadMobsV1(string path, BinaryReader reader)
    {
        int count = reader.ReadInt32();
        if (count < 0 || count > 4096)
        {
            Log.Warn($"Mob save {path} has invalid mob count {count}, ignoring it.");
            return new List<MobSaveData>();
        }

        var mobs = new List<MobSaveData>(count);
        for (int i = 0; i < count; i++)
        {
            var type = (MobType)reader.ReadByte();
            float px = reader.ReadSingle();
            float py = reader.ReadSingle();
            float pz = reader.ReadSingle();
            float vx = reader.ReadSingle();
            float vy = reader.ReadSingle();
            float vz = reader.ReadSingle();
            float hx = reader.ReadSingle();
            float hy = reader.ReadSingle();
            float hz = reader.ReadSingle();
            float yaw = reader.ReadSingle();
            float wanderAngle = reader.ReadSingle();
            int health = reader.ReadInt32();
            float attackCooldown = reader.ReadSingle();
            float wanderTimer = reader.ReadSingle();
            float idleTimer = reader.ReadSingle();
            float hurtTimer = reader.ReadSingle();
            float specialTimer = reader.ReadSingle();
            bool specialActive = reader.ReadBoolean();
            bool onGround = reader.ReadBoolean();
            float stepDistance = reader.ReadSingle();
            float age = reader.ReadSingle();

            mobs.Add(new MobSaveData(
                type,
                new Vector3(px, py, pz),
                new Vector3(vx, vy, vz),
                new Vector3(hx, hy, hz),
                yaw,
                wanderAngle,
                health,
                attackCooldown,
                wanderTimer,
                idleTimer,
                hurtTimer,
                specialTimer,
                specialActive,
                onGround,
                stepDistance,
                age,
                elite: false));
        }

        return mobs;
    }

    private static List<MobSaveData> LoadMobsV2(string path, BinaryReader reader)
    {
        int count = reader.ReadInt32();
        if (count < 0 || count > 4096)
        {
            Log.Warn($"Mob save {path} has invalid mob count {count}, ignoring it.");
            return new List<MobSaveData>();
        }

        var mobs = new List<MobSaveData>(count);
        for (int i = 0; i < count; i++)
        {
            var type = (MobType)reader.ReadByte();
            float px = reader.ReadSingle();
            float py = reader.ReadSingle();
            float pz = reader.ReadSingle();
            float vx = reader.ReadSingle();
            float vy = reader.ReadSingle();
            float vz = reader.ReadSingle();
            float hx = reader.ReadSingle();
            float hy = reader.ReadSingle();
            float hz = reader.ReadSingle();
            float yaw = reader.ReadSingle();
            float wanderAngle = reader.ReadSingle();
            int health = reader.ReadInt32();
            float attackCooldown = reader.ReadSingle();
            float wanderTimer = reader.ReadSingle();
            float idleTimer = reader.ReadSingle();
            float hurtTimer = reader.ReadSingle();
            float specialTimer = reader.ReadSingle();
            bool specialActive = reader.ReadBoolean();
            bool onGround = reader.ReadBoolean();
            float stepDistance = reader.ReadSingle();
            float age = reader.ReadSingle();
            bool elite = reader.ReadBoolean();

            mobs.Add(new MobSaveData(
                type,
                new Vector3(px, py, pz),
                new Vector3(vx, vy, vz),
                new Vector3(hx, hy, hz),
                yaw,
                wanderAngle,
                health,
                attackCooldown,
                wanderTimer,
                idleTimer,
                hurtTimer,
                specialTimer,
                specialActive,
                onGround,
                stepDistance,
                age,
                elite));
        }

        return mobs;
    }

    private static List<MobSaveData> LoadMobsV3(string path, BinaryReader reader)
    {
        int count = reader.ReadInt32();
        if (count < 0 || count > 4096)
        {
            Log.Warn($"Mob save {path} has invalid mob count {count}, ignoring it.");
            return new List<MobSaveData>();
        }

        var mobs = new List<MobSaveData>(count);
        for (int i = 0; i < count; i++)
        {
            MobType type = (MobType)reader.ReadByte();
            float px = reader.ReadSingle();
            float py = reader.ReadSingle();
            float pz = reader.ReadSingle();
            float vx = reader.ReadSingle();
            float vy = reader.ReadSingle();
            float vz = reader.ReadSingle();
            float hx = reader.ReadSingle();
            float hy = reader.ReadSingle();
            float hz = reader.ReadSingle();
            float yaw = reader.ReadSingle();
            float wander = reader.ReadSingle();
            int health = reader.ReadInt32();
            float attackCooldown = reader.ReadSingle();
            float wanderTimer = reader.ReadSingle();
            float idleTimer = reader.ReadSingle();
            float hurtTimer = reader.ReadSingle();
            float specialTimer = reader.ReadSingle();
            bool specialActive = reader.ReadBoolean();
            bool onGround = reader.ReadBoolean();
            float stepDistance = reader.ReadSingle();
            float age = reader.ReadSingle();
            bool elite = reader.ReadBoolean();
            int eliteVariantValue = reader.ReadInt32();
            EliteMobVariant eliteVariant = eliteVariantValue switch
            {
                0 => EliteMobVariant.None,
                1 => EliteMobVariant.Brute,
                2 => EliteMobVariant.Hunter,
                3 => EliteMobVariant.Warden,
                _ => EliteMobVariant.None
            };

            mobs.Add(new MobSaveData(
                type,
                new Vector3(px, py, pz),
                new Vector3(vx, vy, vz),
                new Vector3(hx, hy, hz),
                yaw,
                wander,
                health,
                attackCooldown,
                wanderTimer,
                idleTimer,
                hurtTimer,
                specialTimer,
                specialActive,
                onGround,
                stepDistance,
                age,
                elite,
                eliteVariant));
        }

        return mobs;
    }

    private static List<TntSaveData> LoadTntV1(string path, BinaryReader reader, int maxCount)
    {
        int count = reader.ReadInt32();
        int limit = Math.Clamp(maxCount, 1, 65536);
        if (count < 0)
        {
            Log.Warn($"Tnt save {path} has invalid count {count}, ignoring it.");
            return new List<TntSaveData>();
        }

        if (count > limit)
        {
            Log.Warn($"Tnt save {path} has {count} entries; truncating to {limit}.");
            count = limit;
        }

        var tnts = new List<TntSaveData>(count);
        for (int i = 0; i < count; i++)
        {
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            int z = reader.ReadInt32();
            float fuse = reader.ReadSingle();
            float duration = Math.Max(0f, fuse);
            tnts.Add(new TntSaveData(new BlockCoord(x, y, z), Math.Max(0f, fuse), duration));
        }

        return tnts;
    }

    private static List<TntSaveData> LoadTntV2(string path, BinaryReader reader, int maxCount)
    {
        int count = reader.ReadInt32();
        int limit = Math.Clamp(maxCount, 1, 65536);
        if (count < 0)
        {
            Log.Warn($"Tnt save {path} has invalid count {count}, ignoring it.");
            return new List<TntSaveData>();
        }

        if (count > limit)
        {
            Log.Warn($"Tnt save {path} has {count} entries; truncating to {limit}.");
            count = limit;
        }

        var tnts = new List<TntSaveData>(count);
        for (int i = 0; i < count; i++)
        {
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            int z = reader.ReadInt32();
            float fuse = reader.ReadSingle();
            float duration = reader.ReadSingle();
            tnts.Add(new TntSaveData(new BlockCoord(x, y, z), Math.Max(0f, fuse), Math.Max(0f, duration)));
        }

        return tnts;
    }

    private static HerobrineSaveData LoadHerobrineV1(string path, BinaryReader reader, HerobrineSaveData fallback)
    {
        try
        {
            int seed = reader.ReadInt32();
            float manifestX = reader.ReadSingle();
            float manifestY = reader.ReadSingle();
            float manifestZ = reader.ReadSingle();
            float playerX = reader.ReadSingle();
            float playerY = reader.ReadSingle();
            float playerZ = reader.ReadSingle();
            float hauntPressure = Math.Max(0f, reader.ReadSingle());
            float manifestCooldown = Math.Max(0f, reader.ReadSingle());
            float eventCooldown = Math.Max(0f, reader.ReadSingle());
            float worldEffectCooldown = Math.Max(0f, reader.ReadSingle());
            float activeTimer = Math.Max(0f, reader.ReadSingle());
            int encounterCount = Math.Max(0, reader.ReadInt32());
            bool isManifested = reader.ReadBoolean();

            return new HerobrineSaveData(
                seed,
                new Vector3(manifestX, manifestY, manifestZ),
                new Vector3(playerX, playerY, playerZ),
                hauntPressure,
                manifestCooldown,
                eventCooldown,
                worldEffectCooldown,
                activeTimer,
                encounterCount,
                isManifested);
        }
        catch (EndOfStreamException)
        {
            Log.Warn($"Herobrine save {path} ended unexpectedly while reading version 1 data, using fallback.");
            return fallback;
        }
    }

    private static bool TryReadMagic(BinaryReader reader, byte[] expectedMagic, out bool hasMagic)
    {
        hasMagic = false;
        if (reader.BaseStream.Length < expectedMagic.Length + sizeof(int))
        {
            return false;
        }

        byte[] magic = reader.ReadBytes(expectedMagic.Length);
        if (magic.Length != expectedMagic.Length)
        {
            return false;
        }

        hasMagic = magic[0] == expectedMagic[0] &&
                   magic[1] == expectedMagic[1] &&
                   magic[2] == expectedMagic[2] &&
                   magic[3] == expectedMagic[3];
        return true;
    }
}
