using System;
using System.IO;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.IO;

public static class WorldSave
{
    private static readonly byte[] PlayerMagic = { (byte)'M', (byte)'C', (byte)'P', (byte)'S' };
    private const int PlayerVersion = 2;

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

        string text = File.ReadAllText(path);
        return int.TryParse(text, out int seed) ? seed : fallback;
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
        writer.Write(data.SelectedIndex);
    }

    public static PlayerSaveData LoadPlayer(string worldPath, PlayerSaveData fallback)
    {
        string path = Path.Combine(worldPath, "player.dat");
        if (!File.Exists(path))
        {
            return fallback;
        }

        using var reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read));
        if (TryReadMagic(reader, out bool hasMagic) && hasMagic)
        {
            int version = reader.ReadInt32();
            if (version != PlayerVersion)
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
            int selected = reader.ReadInt32();
            if (selected < 0 || selected >= hotbarCount)
            {
                selected = 0;
            }

            return new PlayerSaveData(new Vector3(x, y, z), hotbar, selected);
        }

        // legacy format (position only)
        reader.BaseStream.Position = 0;
        float lx = reader.ReadSingle();
        float ly = reader.ReadSingle();
        float lz = reader.ReadSingle();
        var legacyHotbar = new[] { BlockId.Grass, BlockId.Dirt, BlockId.Stone, BlockId.Wood, BlockId.Planks, BlockId.Glass, BlockId.Sand, BlockId.Torch, BlockId.Water };
        return new PlayerSaveData(new Vector3(lx, ly, lz), legacyHotbar, 0);
    }

    private static bool TryReadMagic(BinaryReader reader, out bool hasMagic)
    {
        hasMagic = false;
        if (reader.BaseStream.Length < PlayerMagic.Length + sizeof(int))
        {
            return false;
        }

        byte[] magic = reader.ReadBytes(PlayerMagic.Length);
        if (magic.Length != PlayerMagic.Length)
        {
            return false;
        }

        hasMagic = magic[0] == PlayerMagic[0] &&
                   magic[1] == PlayerMagic[1] &&
                   magic[2] == PlayerMagic[2] &&
                   magic[3] == PlayerMagic[3];
        return true;
    }
}
