using System;
using MiniCRUFT.Core;

namespace MiniCRUFT.World;

public readonly struct LootDrop
{
    public BlockId Item { get; }
    public int Count { get; }

    public LootDrop(BlockId item, int count)
    {
        Item = item;
        Count = Math.Max(0, count);
    }
}
