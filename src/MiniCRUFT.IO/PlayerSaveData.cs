using System;
using System.Numerics;
using MiniCRUFT.World;

namespace MiniCRUFT.IO;

public readonly struct PlayerSaveData
{
    public Vector3 Position { get; }
    public BlockId[] Hotbar { get; }
    public int[] Counts { get; }
    public int[]? ToolDurability { get; }
    public int SelectedIndex { get; }

    public PlayerSaveData(Vector3 position, BlockId[] hotbar, int selectedIndex)
        : this(position, hotbar, BlockStackDefaults.CreateDefaultCounts(hotbar), null, selectedIndex)
    {
    }

    public PlayerSaveData(Vector3 position, BlockId[] hotbar, int[] counts, int selectedIndex)
        : this(position, hotbar, counts, null, selectedIndex)
    {
    }

    public PlayerSaveData(Vector3 position, BlockId[] hotbar, int[] counts, int[]? toolDurability, int selectedIndex)
    {
        hotbar ??= Array.Empty<BlockId>();
        if (counts == null || counts.Length != hotbar.Length)
        {
            counts = BlockStackDefaults.CreateDefaultCounts(hotbar);
        }

        if (toolDurability != null && toolDurability.Length != hotbar.Length)
        {
            var normalizedDurability = new int[hotbar.Length];
            Array.Fill(normalizedDurability, -1);
            Array.Copy(toolDurability, normalizedDurability, Math.Min(toolDurability.Length, normalizedDurability.Length));
            toolDurability = normalizedDurability;
        }

        Position = position;
        Hotbar = hotbar;
        Counts = counts;
        ToolDurability = toolDurability;
        SelectedIndex = hotbar.Length == 0 ? 0 : Math.Clamp(selectedIndex, 0, hotbar.Length - 1);
    }
}
