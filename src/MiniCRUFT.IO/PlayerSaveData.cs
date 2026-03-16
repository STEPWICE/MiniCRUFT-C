using System.Numerics;
using MiniCRUFT.World;

namespace MiniCRUFT.IO;

public readonly struct PlayerSaveData
{
    public Vector3 Position { get; }
    public BlockId[] Hotbar { get; }
    public int SelectedIndex { get; }

    public PlayerSaveData(Vector3 position, BlockId[] hotbar, int selectedIndex)
    {
        Position = position;
        Hotbar = hotbar;
        SelectedIndex = selectedIndex;
    }
}
