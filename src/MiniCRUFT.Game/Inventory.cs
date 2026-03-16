using System;
using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public sealed class Inventory
{
    public BlockId[] Hotbar { get; } = new BlockId[9];
    public int SelectedIndex { get; private set; }

    public Inventory()
    {
        Reset();
    }

    public BlockId GetSelectedBlock() => Hotbar[SelectedIndex];

    public void Scroll(int delta)
    {
        if (delta == 0)
        {
            return;
        }

        int direction = delta > 0 ? -1 : 1;
        SelectedIndex = (SelectedIndex + direction + Hotbar.Length) % Hotbar.Length;
    }

    public void Reset()
    {
        Hotbar[0] = BlockId.Grass;
        Hotbar[1] = BlockId.Dirt;
        Hotbar[2] = BlockId.Stone;
        Hotbar[3] = BlockId.Wood;
        Hotbar[4] = BlockId.Planks;
        Hotbar[5] = BlockId.Glass;
        Hotbar[6] = BlockId.Sand;
        Hotbar[7] = BlockId.Torch;
        Hotbar[8] = BlockId.Water;
        SelectedIndex = 0;
    }

    public void ApplySave(BlockId[] hotbar, int selectedIndex)
    {
        int count = Math.Min(hotbar.Length, Hotbar.Length);
        for (int i = 0; i < count; i++)
        {
            Hotbar[i] = hotbar[i];
        }
        for (int i = count; i < Hotbar.Length; i++)
        {
            Hotbar[i] = BlockId.Air;
        }

        if (selectedIndex < 0 || selectedIndex >= Hotbar.Length)
        {
            selectedIndex = 0;
        }
        SelectedIndex = selectedIndex;
    }
}
