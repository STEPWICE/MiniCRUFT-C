using System;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.Renderer;

public enum SelectionKind
{
    None,
    Block,
    Mob
}

public readonly struct SelectionState
{
    public SelectionKind Kind { get; }
    public BlockCoord Block { get; }
    public MobRenderInstance Mob { get; }
    public float Distance { get; }
    public float Progress { get; }

    public bool HasSelection => Kind != SelectionKind.None;
    public bool IsBlock => Kind == SelectionKind.Block;
    public bool IsMob => Kind == SelectionKind.Mob;

    private SelectionState(SelectionKind kind, BlockCoord block, MobRenderInstance mob, float distance, float progress)
    {
        Kind = kind;
        Block = block;
        Mob = mob;
        Distance = distance;
        Progress = Math.Clamp(progress, 0f, 1f);
    }

    public static SelectionState None => default;

    public static SelectionState ForBlock(BlockCoord block, float distance)
    {
        return new SelectionState(SelectionKind.Block, block, default, distance, 0f);
    }

    public static SelectionState ForBlock(BlockCoord block, float distance, float progress)
    {
        return new SelectionState(SelectionKind.Block, block, default, distance, progress);
    }

    public static SelectionState ForMob(MobRenderInstance mob, float distance)
    {
        return new SelectionState(SelectionKind.Mob, default, mob, distance, 0f);
    }
}
